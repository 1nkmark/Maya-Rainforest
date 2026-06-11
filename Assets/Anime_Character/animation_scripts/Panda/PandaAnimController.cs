using UnityEngine;
using System.Collections;

public class PandaAnimController : MonoBehaviour
{
    private Animator animator;
    private DialogueSystem dialogueSystem;
    private NavMeshAgentAI patrol;

    private Coroutine pandaCruiseCoroutine;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        patrol = GetComponent<NavMeshAgentAI>();

        if (animator == null)
            Debug.LogError("❌ 胖达找不到 Animator 组件！");

        if (patrol == null)
            Debug.LogError("❌ 胖达找不到 NavMeshAgentAI 组件！");

        GameObject dialogRoot = GameObject.Find("DialogueManager");

        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();

            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd += OnLineEnd;
                Debug.Log("胖达动画事件注册完成（Awake）");
            }
            else
            {
                Debug.LogError("❌ DialogueManager 上没有 DialogueSystem 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ 场景中找不到 DialogueManager！");
        }
    }

    void OnEnable()
    {
        GameEvents.OnPandaCruiseStarted += OnPandaCruiseStarted;
    }

    void OnDisable()
    {
        GameEvents.OnPandaCruiseStarted -= OnPandaCruiseStarted;
    }

    void ResetAllBools()
    {
        if (animator == null) return;

        animator.SetBool("isGreeting", false);
        animator.SetBool("isHappy", false);
        animator.SetBool("isPointing", false);
        animator.SetBool("isLooking", false);

        // ✅ 巡航中不强行关闭走路动画，避免和 NavMeshAgentAI 争抢 isWalking 控制权
        if (patrol == null || !patrol.canPatrol)
            animator.SetBool("isWalking", false);
    }

    void OnLineStart(string speaker, string content)
    {
        Debug.Log($"【收到台词】speaker={speaker} content={content}");

        if (speaker != "胖达") return;

        Debug.Log($"【胖达台词】{content}");

        ResetAllBools();

        if (content.Contains("新面孔"))
        {
            Debug.Log("胖达：触发 greeting 动画");
            animator.SetBool("isGreeting", true);
        }
        else if (content.Contains("别丧气"))
        {
            Debug.Log("胖达：触发 happy 动画");
            animator.SetBool("isHappy", true);
        }
        else if (content.Contains("红外成像仪"))
        {
            Debug.Log("胖达：触发 pointing 动画");
            animator.SetBool("isPointing", true);
        }
        else if (content.Contains("锅底"))
        {
            Debug.Log("胖达：触发 looking 动画");
            animator.SetBool("isLooking", true);
        }
        else if (content.Contains("把它握在手里"))
        {
            Debug.Log("胖达：触发 pointing 动画");
            animator.SetBool("isPointing", true);
        }
        else
        {
            Debug.Log("胖达：没有匹配到任何关键词，保持 Idle");
        }
    }

    void OnLineEnd(string speaker)
    {
        if (speaker == "胖达")
            ResetAllBools();
    }

    void OnPandaCruiseStarted()
    {
        Debug.Log("胖达收到巡航广播事件");

        if (pandaCruiseCoroutine != null)
            StopCoroutine(pandaCruiseCoroutine);

        pandaCruiseCoroutine = StartCoroutine(StartCruiseDelayed());
    }

    IEnumerator StartCruiseDelayed()
    {
        Debug.Log("胖达先保持 Idle 2 秒");

        ResetAllBools();

        yield return new WaitForSeconds(2f);

        if (patrol == null)
        {
            Debug.LogError("❌ 胖达没有 NavMeshAgentAI，无法开始巡航！");
            yield break;
        }

        Debug.Log("胖达开始巡航");
        patrol.BeginPatrol();
        pandaCruiseCoroutine = null;
    }

    void OnDestroy()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnLineStartWithContent -= OnLineStart;
            dialogueSystem.OnLineEnd -= OnLineEnd;
        }

        GameEvents.OnPandaCruiseStarted -= OnPandaCruiseStarted;
    }
}