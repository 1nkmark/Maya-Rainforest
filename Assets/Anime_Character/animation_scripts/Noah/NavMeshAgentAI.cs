using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentAI : MonoBehaviour
{
    [HideInInspector] public NavMeshAgent agent;

    [Header("巡航路径点")]
    public Transform[] waypoints;

    [Header("巡航控制")]
    public bool canPatrol = false;

    [Header("移动参数")]
    public float moveSpeed = 1.0f;
    public float arriveDistance = 0.5f;

    [Header("动画参数")]
    public float walkAnimThreshold = 0.05f;

    private Animator animator;
    private int currentWaypoint = 0;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        canPatrol = false;
        if (agent != null)
            agent.isStopped = true;
    }

    void Start()
    {
        if (agent == null) { Debug.LogError("❌ 找不到 NavMeshAgent！"); enabled = false; return; }
        if (animator == null) { Debug.LogError("❌ 找不到 Animator！"); enabled = false; return; }

        agent.speed = moveSpeed;
        agent.updateRotation = true;
        agent.angularSpeed = 360f;   // ✅ 修复：原120f转身太慢导致拐角平移，改为360f瞬间对齐方向
        agent.autoBraking = true;
        animator.applyRootMotion = false;

        Debug.Log($"NavMeshAgent 设置: updateRotation={agent.updateRotation}, angularSpeed={agent.angularSpeed}, autoBraking={agent.autoBraking}, applyRootMotion={animator.applyRootMotion}");

        StopPatrol();
    }

    void Update()
    {
        if (!canPatrol) return;

        // ✅ 修复：删除原来每帧的 Debug.Log（严重拖慢帧率，是胖达一顿一顿的主因）

        if (agent == null || !agent.isOnNavMesh) return;

        if (waypoints == null || waypoints.Length == 0)
        {
            animator.SetBool("isWalking", false);
            return;
        }

        UpdateWalkAnimation();
        UpdateWaypointMovement();
    }

    private void UpdateWalkAnimation()
    {
        if (animator == null || agent == null) return;

        bool shouldWalk = agent.remainingDistance > arriveDistance;
        animator.SetBool("isWalking", shouldWalk);

        // ✅ 修复：删除原来每帧的 Debug.Log（每帧输出isWalking/speed等，严重拖慢帧率）
    }

    private void UpdateWaypointMovement()
    {
        if (agent.pathPending) return;
        if (!agent.hasPath) return;
        if (agent.remainingDistance > arriveDistance) return;

        if (currentWaypoint >= waypoints.Length - 1)
        {
            Debug.Log("✅ 巡航结束");
            StopPatrol();
            return;
        }

        currentWaypoint++;
        agent.SetDestination(waypoints[currentWaypoint].position);

        // ✅ 修复：切换路径点时立即保持 isWalking=true，
        //         防止 remainingDistance 短暂归零导致动画闪一帧 Idle
        if (animator != null)
            animator.SetBool("isWalking", true);

        Debug.Log($"➡️ 前往：{waypoints[currentWaypoint].name}");
    }

    public void BeginPatrol()
    {
        if (agent == null) { Debug.LogError("❌ agent 为空！"); return; }
        if (waypoints == null || waypoints.Length == 0) { Debug.LogError("❌ waypoints 为空！"); return; }

        currentWaypoint = 0;
        canPatrol = true;
        agent.speed = moveSpeed;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.SetDestination(waypoints[currentWaypoint].position);
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] BeginPatrol 时不在 NavMesh 上！Base Offset 可能还不对");
        }

        if (animator != null)
        {
            animator.SetBool("isGreeting", false);
            animator.SetBool("isWalking", true);
        }

        Debug.Log($"🚶 [{gameObject.name}] 开始巡航，目标：{waypoints[currentWaypoint].name}");
    }

    public void StopPatrol()
    {
        canPatrol = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        if (animator != null)
            animator.SetBool("isWalking", false);
    }
}