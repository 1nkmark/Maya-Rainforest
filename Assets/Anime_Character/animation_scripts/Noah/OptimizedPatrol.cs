using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class OptimizedPatrol : MonoBehaviour
{
    [Header("巡逻点设置")]
    public Transform[] waypoints;
    private int currentWaypoint = 0;

    [Header("巡逻控制")]
    public bool canPatrol = false;   // 是否允许巡逻

    [Header("角色组件")]
    public Animator animator;
    public NavMeshAgent agent;

    [Header("动画参数")]
    public float greetingTime = 2.5f; // 打招呼动画时长
    public float walkThreshold = 0.05f; // 动画切换行走的速度阈值

    void Start()
    {
        // 获取组件
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        // 确保 Root Motion 关闭
        animator.applyRootMotion = false;

        // Agent 初始化参数（可根据你的角色修改）
        agent.speed = 1.5f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = true;
        agent.updateRotation = true;

        // 开始打招呼流程
        StartCoroutine(GreetingThenPatrol());
    }

    void Update()
    {
        // 没有巡逻或 waypoint 空，直接返回
        if (!canPatrol || waypoints.Length == 0) return;

        // 动画同步：根据 NavMeshAgent 的速度播放行走动画
        float speed = agent.velocity.magnitude;
        animator.SetBool("isWalking", speed > walkThreshold);

        // 判断是否到达当前 waypoint
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // 到达最后一个 waypoint，停止巡逻
            if (currentWaypoint >= waypoints.Length - 1)
            {
                canPatrol = false;
                animator.SetBool("isWalking", false);
                return;
            }

            // 前往下一个 waypoint
            currentWaypoint++;
            agent.SetDestination(waypoints[currentWaypoint].position);
        }
    }

    /// <summary>
    /// 协程：先打招呼，再开始巡逻
    /// </summary>
    IEnumerator GreetingThenPatrol()
    {
        // 播放打招呼动画
        animator.SetBool("isGreeting", true);
        yield return new WaitForSeconds(greetingTime);
        animator.SetBool("isGreeting", false);

        // 等待少许时间后开始巡逻
        yield return new WaitForSeconds(0.5f);

        if (waypoints.Length == 0) yield break;

        // 开启巡逻
        canPatrol = true;
        currentWaypoint = 0;
        agent.SetDestination(waypoints[currentWaypoint].position);
        animator.SetBool("isWalking", true);
    }

    /// <summary>
    /// 外部调用：手动触发巡逻（可选）
    /// </summary>
    public void StartPatrol()
    {
        if (waypoints.Length == 0) return;
        canPatrol = true;
        currentWaypoint = 0;
        agent.SetDestination(waypoints[currentWaypoint].position);
        animator.SetBool("isWalking", true);
    }
}