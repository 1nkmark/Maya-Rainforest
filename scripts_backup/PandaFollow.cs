using UnityEngine;
using UnityEngine.AI;

public class PandaFollow : MonoBehaviour
{
    public Transform player;
    public float followDistance = 2f;
    public float stopDistance = 1.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isFollowing = false;
    private DialogueSystem dialogueSystem;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 一开始停着不动
        agent.isStopped = true;
        animator.SetBool("isWalking", false);

        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();
            dialogueSystem.OnDialogueEnd += StartFollowing;
        }
    }

    void StartFollowing()
    {
        Debug.Log("对话结束，胖达开始跟随！");
        isFollowing = true;
        agent.isStopped = false;
    }

    void Update()
    {
        if (!isFollowing || player == null) return;

        Vector3 agentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.position.x, 0, player.position.z);
        float dist = Vector3.Distance(agentPos, playerPos);

        // 加这行，运行时看控制台
        Debug.Log($"胖达距离玩家：{dist}，isFollowing：{isFollowing}，agent速度：{agent.velocity.magnitude}");

        if (dist > followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetBool("isWalking", true);
        }
        else
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);
        }
    }
    void OnDestroy()
    {
        if (dialogueSystem != null)
            dialogueSystem.OnDialogueEnd -= StartFollowing;
    }
}