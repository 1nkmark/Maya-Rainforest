using UnityEngine;
using UnityEngine.AI;

public class WaypointPatrol : MonoBehaviour
{
    // ✅ 改成private，不在Inspector显示，完全由代码控制
    [HideInInspector] public NavMeshAgent agent;
    public Transform[] waypoints;
    public bool canPatrol = false;

    private Animator animator;
    private int currentWaypoint = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = 1.5f; // 调整速度
    }

    void Update()
    {
        if (!canPatrol || waypoints.Length == 0) return;

        // ⭐ 用真实速度驱动动画
        float speed = agent.velocity.magnitude;
        animator.SetBool("isWalking", speed > 0.05f);
        animator.SetBool("isGreeting", false);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypoint].position);
        }
    }
}