using UnityEngine;

/// <summary>
/// 第二幕：神庙破译 —— 胖达动画控制器
/// 根据 DialogueSystemAct2 广播的台词关键词，驱动胖达的 Animator Bool 参数。
///
/// 对应 Panda Animator 中使用的 Bool 参数：
///   isHappy     → 开心 / 恍然大悟
///   isPointing  → 指向 / 讲解操作
///   isCheering  → 欢呼 / 成功庆祝
///
/// 依赖：场景中存在名为 "DialogueManager" 的 GameObject，挂载 DialogueSystemAct2 组件。
/// </summary>
public class PandaAnimController2 : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  私有引用
    // ─────────────────────────────────────────────
    private Animator animator;
    private DialogueSystemAct2 dialogueSystem;

    // ─────────────────────────────────────────────
    //  Unity 生命周期
    // ─────────────────────────────────────────────
    void Awake()
    {
        // 获取 Animator（优先本体，找不到则在子物体中查找）
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("❌ [PandaAnimController2] 找不到 Animator 组件！");

        // 注册 DialogueSystemAct2 事件
        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystemAct2>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd += OnLineEnd;
                Debug.Log("✅ [PandaAnimController2] DialogueSystemAct2 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [PandaAnimController2] DialogueManager 上没有 DialogueSystemAct2 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [PandaAnimController2] 场景中找不到 DialogueManager！");
        }
    }

    // ─────────────────────────────────────────────
    //  对话事件回调
    // ─────────────────────────────────────────────

    void OnLineStart(string speaker, string content)
    {
        if (speaker != "胖达") return;

        Debug.Log($"[PandaAnimController2] ▶ 收到台词 | 说话人：{speaker} | 内容：{content}");

        ResetAllBools();

        // ── 阶段一：教学引导 ──────────────────────────────────────────────

        // "画数字？这个我会！不过……玛雅数字长得可不像我们平时写的 1、2、3 吧？"
        if (content.Contains("画数字"))
        {
            TriggerAnim("isHappy", "开心（画数字？）");
        }
        // "操作也简单！扣住右手手柄的食指扳机，就能在石碑上画……"
        else if (content.Contains("操作也简单"))
        {
            TriggerAnim("isPointing", "指向（操作也简单）");
        }

        // ── 阶段二：输入数字 9 ───────────────────────────────────────────

        // "也就是 5 加 4！先画一横，再在上面补四个点。"
        else if (content.Contains("也就是") && content.Contains("加 4"))
        {
            TriggerAnim("isHappy", "开心（也就是5加4）");
        }
        // "成了！一横四点，九个数刚刚好！"
        else if (content.Contains("成了"))
        {
            TriggerAnim("isCheering", "欢呼（成了！）");
        }

        // ── 阶段三：输入数字 13 ──────────────────────────────────────────

        // "我懂了！一条横线是 5，两条就是 10，再加 3 个点，就是 13！"
        else if (content.Contains("我懂了"))
        {
            TriggerAnim("isHappy", "开心（我懂了）");
        }
        // "这次也对了！玛雅数字还挺直观的嘛。"
        else if (content.Contains("这次也对了"))
        {
            TriggerAnim("isCheering", "欢呼（这次也对了）");
        }

        // ── 阶段四：输入数字 20（进位点 + 圈）────────────────────────────

        // "所以 20 不是在下面画四条横线，而是——上面画一个点，下面画一个圈？"
        else if (content.Contains("四条横线") || content.Contains("上面画一个点"))
        {
            TriggerAnim("isHappy", "开心（恍然大悟：20的写法）");
        }
        // "简单说：上面点一下，下面画圈。这个就是 20！"
        else if (content.Contains("上面点一下") || content.Contains("下面画圈"))
        {
            TriggerAnim("isHappy", "开心（总结：上点下圈就是20）");
        }
        // "也就是说，我们不是写了一个 20，而是让石碑看懂了'满二十进一'！"
        else if (content.Contains("满二十进一") || content.Contains("让石碑看懂"))
        {
            TriggerAnim("isCheering", "欢呼（理解进位：满二十进一）");
        }
        // "看！玛雅文明已经向我们敞开了大门，我们进去一探究竟吧！"
        else if (content.Contains("向我们敞开") || content.Contains("一探究竟"))
        {
            TriggerAnim("isVictory", "胜利（玛雅文明敞开大门）");
        }

        // ── 未命中任何关键词 ──────────────────────────────────────────────
        else
        {
            Debug.Log($"[PandaAnimController2] ⚪ 无匹配关键词，保持 Idle | 台词：{content}");
        }
    }

    void OnLineEnd(string speaker)
    {
        if (speaker != "胖达") return;

        Debug.Log($"[PandaAnimController2] ⏹ 台词结束，重置所有动画 Bool → Idle | 说话人：{speaker}");
        ResetAllBools();
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    /// <summary>SetBool 置 true，并打印触发日志。</summary>
    void TriggerAnim(string boolName, string description)
    {
        animator.SetBool(boolName, true);
        Debug.Log($"[PandaAnimController2] 🎬 触发动画 | Bool：{boolName} | 描述：{description}");
    }

    /// <summary>将胖达 Animator 中所有自定义 Bool 重置为 false。</summary>
    void ResetAllBools()
    {
        if (animator == null) return;

        animator.SetBool("isGreeting", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isHappy", false);
        animator.SetBool("isPointing", false);
        animator.SetBool("isLooking", false);
        animator.SetBool("isAgreeing", false);
        animator.SetBool("isVictory", false);
        animator.SetBool("isWaving1", false);
        animator.SetBool("isCheering", false);

        Debug.Log("[PandaAnimController2] 🔄 ResetAllBools 完成，所有 Bool → false");
    }

    // ─────────────────────────────────────────────
    //  清理
    // ─────────────────────────────────────────────
    void OnDestroy()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnLineStartWithContent -= OnLineStart;
            dialogueSystem.OnLineEnd -= OnLineEnd;
        }
    }
}