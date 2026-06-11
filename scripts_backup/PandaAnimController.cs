using UnityEngine;

public class PandaAnimController : MonoBehaviour
{
    Animator animator;
    DialogueSystem dialogueSystem;

    bool hasPlayed = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();
            // ★ 直接注册即可，DialogueSystem 的自定义事件访问器内部已保证去重
            dialogueSystem.OnLineStart += OnLineStart;
        }
    }

    void OnLineStart(string speaker)
    {
        if (!hasPlayed && speaker == "胖达")
        {
            hasPlayed = true;
            animator.SetBool("isGreeting", true);
            Debug.Log("胖达：第一次说话，触发打招呼动画");
        }
    }

    void OnDestroy()
    {
        if (dialogueSystem != null)
            dialogueSystem.OnLineStart -= OnLineStart;
    }
}
