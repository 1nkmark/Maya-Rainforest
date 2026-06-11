using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimController : MonoBehaviour
{
    Animator animator;
    DialogueSystem dialogueSystem;
    WaypointPatrol patrol;
    bool hasPlayed = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        patrol = GetComponent<WaypointPatrol>();

        if (patrol == null)
        {
            Debug.LogError("❌ 找不到 WaypointPatrol 组件！");
            return;
        }

        patrol.canPatrol = false;
        animator.SetBool("isWalking", false);
        animator.SetBool("isGreeting", false);

        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();
            dialogueSystem.OnLineStart += OnLineStart;
            dialogueSystem.OnDialogueEnd += OnDialogueEnd;
        }
    }

    void OnLineStart(string speaker)
    {
        if (!hasPlayed && speaker == "诺亚博士")
        {
            hasPlayed = true;
            Debug.Log("诺亚：首次说话，触发打招呼");
            StartCoroutine(PlayAnimSequence());
        }
    }

    System.Collections.IEnumerator PlayAnimSequence()
    {
        // ── 阶段1：打招呼 ────────────────────────
        Debug.Log("阶段1：打招呼");
        animator.SetBool("isGreeting", true);

        yield return new WaitForSeconds(2.5f);
        animator.SetBool("isWalking", true);
        yield return new WaitForSeconds(2.5f);
        // 打招呼结束回到idle，等对话全部结束再巡航
        Debug.Log("打招呼结束，等待所有对话结束");
        animator.SetBool("isGreeting", false);
        animator.SetBool("isWalking", false);
    }

    // 普通方法不能yield，改成启动协程
    void OnDialogueEnd()
    {
        Debug.Log("所有对话结束，5秒后开始巡航");
        StartCoroutine(DelayedPatrol());
    }

    System.Collections.IEnumerator DelayedPatrol()
    {
        yield return new WaitForSeconds(5f);

        Debug.Log("诺亚开始巡航！");
        //animator.SetBool("isGreeting", false);
        animator.SetBool("isWalking", true);
        patrol.canPatrol = true;

        if (patrol.waypoints.Length > 0)
            patrol.agent.SetDestination(patrol.waypoints[0].position);
    }

    void OnDestroy()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnLineStart -= OnLineStart;
            dialogueSystem.OnDialogueEnd -= OnDialogueEnd;
        }
    }
}