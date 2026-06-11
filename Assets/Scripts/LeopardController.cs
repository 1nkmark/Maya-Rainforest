using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class LeopardController : MonoBehaviour
{
    [Header("设置")]
    public Transform[] patrolPoints;
    public Transform player;
    public float safeDistance = 8f;

    private NavMeshAgent agent;
    private Animator anim;
    private int currentPointIndex = 0;
    
    // 状态控制
    private bool isAvoiding = false;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // 优化移动参数，增加优雅感
        agent.angularSpeed = 60f;
        agent.acceleration = 2f;
        agent.autoBraking = false;

        MoveToNextPatrolPoint();
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // 1. 避让逻辑：只有在玩家靠近且未处于避让状态时触发
        if (dist < safeDistance && !isAvoiding)
        {
            isAvoiding = true;
            PerformAvoidance();
        }
        // 2. 恢复逻辑：玩家远离后重回巡逻
        else if (dist > safeDistance + 3f && isAvoiding)
        {
            isAvoiding = false;
            MoveToNextPatrolPoint();
        }
        // 3. 正常巡逻逻辑
        else if (!isAvoiding && !agent.pathPending && agent.remainingDistance < 1f)
        {
            MoveToNextPatrolPoint();
        }

        // 4. 脱困机制：如果卡住超过3秒，强制寻找下一个目标
        CheckIfStuck();

        // 5. 动画同步
        anim.SetBool("IsWalking", true);
    }

    void PerformAvoidance()
    {
        Vector3 dirToPlayer = (transform.position - player.position).normalized;
        Vector3 sideDir = Vector3.Cross(dirToPlayer, Vector3.up);
        Vector3 avoidPos = transform.position + (sideDir * 6f) + (dirToPlayer * 2f);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(avoidPos, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("豹子：优雅绕行中...");
        }
    }

    void MoveToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        // 确保目标点在导航网格上
        NavMeshHit hit;
        if (NavMesh.SamplePosition(patrolPoints[currentPointIndex].position, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
    }

    void CheckIfStuck()
    {
        if (agent.velocity.sqrMagnitude < 0.1f && !agent.pathPending)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 3f)
            {
                Debug.LogWarning("豹子卡住，强制切换至下一个巡逻点");
                MoveToNextPatrolPoint();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }
}