using UnityEngine;

/// <summary>
/// 第二幕：神庙破译 —— 诺亚博士动画控制器
/// 根据 DialogueSystemAct2 广播的台词关键词，驱动诺亚博士的 Animator Bool 参数。
///
/// 对应 Play Animator（诺亚）中使用的 Bool 参数：
///   isPointing      → 指向石碑 / 讲解规则
///   isNodding       → 肯定玩家操作正确
///   isAcknowledging → 轻松应答 / 确认补充说明
///   isDisappointed  → 纠错 / 遗憾（预留）
///
/// 依赖：场景中存在名为 "DialogueManager" 的 GameObject，挂载 DialogueSystemAct2 组件。
/// </summary>
public class NoahAnimController2 : MonoBehaviour
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
            Debug.LogError("❌ [NoahAnimController2] 找不到 Animator 组件！");

        // 注册 DialogueSystemAct2 事件
        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystemAct2>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd += OnLineEnd;
                Debug.Log("✅ [NoahAnimController2] DialogueSystemAct2 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [NoahAnimController2] DialogueManager 上没有 DialogueSystemAct2 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [NoahAnimController2] 场景中找不到 DialogueManager！");
        }
    }

    // ─────────────────────────────────────────────
    //  对话事件回调
    // ─────────────────────────────────────────────

    void OnLineStart(string speaker, string content)
    {
        if (speaker != "诺亚") return;

        Debug.Log($"[NoahAnimController2] ▶ 收到台词 | 说话人：{speaker} | 内容：{content}");

        ResetAllBools();

        // ══════════════════════════════════════════════════════════════════
        //  阶段一：教学引导
        // ══════════════════════════════════════════════════════════════════

        // "这是一块玛雅数字石碑。它不是让我们按按钮，而是要我们亲手把数字画出来。"
        if (content.Contains("玛雅数字石碑"))
        {
            TriggerAnim("isIntroducing", "指向（介绍石碑）");
        }
        // "没错。玛雅数字很简单：一个点代表 1，一条横线代表 5，一个圆圈代表 0。"
        else if (content.Contains("一个点代表") || content.Contains("横线代表 5"))
        {
            TriggerAnim("isIntroNumber", "指向（讲解点线圆规则）");
            StartCoroutine(SwitchAnimAfterSeconds("isIntroNumber", "isIntroducing", 2.2f, "指向（介绍石碑）",6.5f));
        }
        // "看清楚石碑的区域。下面这块大的绘画板，用来画基础数字……"
        else if (content.Contains("看清楚石碑"))
        {
            TriggerAnim("isIntroducing", "指向（指向石碑区域）");
            StartCoroutine(SwitchAnimAfterSeconds("isIntroducing", "isIntroUpper", 9f, "指向（指向上方区域）",4f));
        }
        // "石碑上刻着三组残缺的数字：9、13 和 20。它们不是随机出现的……"
        else if (content.Contains("三组残缺"))
        {
            TriggerAnim("isIntroducing", "指向（介绍三组数字）");
        }
        // "9 象征地底世界，13 象征天上世界，而 20 代表玛雅计数中一次完整的循环。"
        else if (content.Contains("象征地底世界") || content.Contains("象征天上世界"))
        {
            TriggerAnim("isLowerupper", "指向（讲解9/13/20文化含义）");
            StartCoroutine(PlayAnimationSegmentAfterStateComplete(
                "isLowerupper", 5.5f, "isIntroNumber", 2f, 7f, "isIntroNumber 2-9秒段"));
        }
        // "先从基础数字开始。按照玛雅规则，我们先输入数字 9。"
        else if (content.Contains("先从基础数字"))
        {
            TriggerAnim("isIntroducing", "指向（引导开始输入）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段二：输入数字 9
        // ══════════════════════════════════════════════════════════════════

        // "数字 9 的写法是：一条横线，加四个点。"
        else if (content.Contains("数字 9 的写法") || content.Contains("数字9的写法"))
        {
            TriggerAnim("isIntroducing", "指向（讲解数字9写法）");
        }
        // "注意，这一步只需要用下面的大区域。不要画到上方的进位区。"
        else if (content.Contains("只需要用下面") || content.Contains("不要画到上方的进位区"))
        {
            TriggerAnim("isIntroducing", "指向（提示只用下方区域）");
        }
        // "很好。地面的数字已经被石碑识别。接下来输入 13。"
        else if (content.Contains("地面的数字已经被石碑识别"))
        {
            TriggerAnim("isNodding", "点头（肯定：输入9正确）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段三：输入数字 13
        // ══════════════════════════════════════════════════════════════════

        // "数字 13 的写法是：两条横线，加三个点。"
        else if (content.Contains("数字 13 的写法") || content.Contains("数字13的写法"))
        {
            TriggerAnim("isIntroducing", "指向（讲解数字13写法）");
        }
        // "对。仍然只在下面的大区域绘制。横线和点都属于基础数字，不需要进位。"
        else if (content.Contains("仍然只在下面") || content.Contains("不需要进位"))
        {
            TriggerAnim("isAcknowledging", "应答（确认：仍在下方区域绘制）");
        }
        // "现在要进入关键部分了。数字 20 不能简单地画四条横线。"
        else if (content.Contains("进入关键部分") || content.Contains("不能简单地画四条横线"))
        {
            TriggerAnim("isIntroducing", "指向（引出进位难点）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段四：输入数字 20（进位点 + 圈）
        // ══════════════════════════════════════════════════════════════════

        // "玛雅人使用二十进制。也就是说，数到 20 时，下面这一位要归零，上面这一位要进 1。"
        else if (content.Contains("二十进制") || content.Contains("下面这一位要归零"))
        {
            TriggerAnim("isIntroducing", "指向（讲解二十进制规则）");
        }
        // "没错。上方小区域画一个点，代表向高位进 1；下方大区域画一个圆圈，代表这一位是 0。"
        else if (content.Contains("上方小区域") || content.Contains("向高位进 1"))
        {
            TriggerAnim("isNodding", "点头（确认：上点下圈规则）");
        }
        // 玩家输入正确后：
        // "正确。上方一位，代表一个完整的二十；下方归零，表示这一轮计数完成。"
        else if (content.Contains("代表一个完整的二十") || content.Contains("这一轮计数完成"))
        {
            TriggerAnim("isNodding", "点头（肯定：输入20正确）");
        }
        // "这就是玛雅数字的进位规则。石碑已经认可我们的输入了。"
        else if (content.Contains("进位规则") || content.Contains("石碑已经认可"))
        {
            TriggerAnim("isAcknowledging", "应答（总结进位规则）");
        }

        // ── 未命中任何关键词 ──────────────────────────────────────────────
        else
        {
            Debug.Log($"[NoahAnimController2] ⚪ 无匹配关键词，保持 Idle | 台词：{content}");
        }
    }

    void OnLineEnd(string speaker)
    {
        if (speaker != "诺亚") return;

        Debug.Log($"[NoahAnimController2] ⏹ 台词结束，重置所有动画 Bool → Idle | 说话人：{speaker}");
        ResetAllBools();
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    /// <summary>SetBool 置 true，并打印触发日志。</summary>
    void TriggerAnim(string boolName, string description)
    {
        animator.SetBool(boolName, true);
        Debug.Log($"[NoahAnimController2] 🎬 触发动画 | Bool：{boolName} | 描述：{description}");
    }

    /// <summary>在指定秒数后把 Animator Bool 复位为 false。</summary>
    System.Collections.IEnumerator ResetBoolAfterSeconds(string boolName, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (animator != null)
        {
            animator.SetBool(boolName, false);
            Debug.Log($"[NoahAnimController2] ⏱️ {boolName} 已在 {seconds} 秒后重置为 false");
        }
    }

    System.Collections.IEnumerator SwitchAnimAfterSeconds(string fromBool, string toBool, float seconds, string toDescription, float keepToBoolSeconds = 0f)
    {
        yield return new WaitForSeconds(seconds);
        if (animator != null)
        {
            animator.SetBool(fromBool, false);
            animator.SetBool(toBool, true);
            Debug.Log($"[NoahAnimController2] 🎬 动画切换 | {fromBool} -> {toBool} | 描述：{toDescription}");
        }

        if (keepToBoolSeconds > 0f)
        {
            yield return new WaitForSeconds(keepToBoolSeconds);
            if (animator != null)
            {
                animator.SetBool(toBool, false);
                Debug.Log($"[NoahAnimController2] ⏱️ {toBool} 已在 {keepToBoolSeconds} 秒后重置为 false");
            }
        }
    }

    AnimationClip FindAnimationClip(string query)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.Equals(query, System.StringComparison.OrdinalIgnoreCase))
                return clip;

            string normalizedQuery = query.ToLowerInvariant().Replace("is", "");
            if (clip.name.ToLowerInvariant().Contains(normalizedQuery))
                return clip;
        }

        return null;
    }

    float GetAnimationClipLength(string query)
    {
        var clip = FindAnimationClip(query);
        return clip != null ? clip.length : 0f;
    }

    System.Collections.IEnumerator PlayAnimationSegmentAfterStateComplete(string fromBool, float fromDuration, string toStateName, float startSeconds, float durationSeconds, string description)
    {
        float waitSeconds = fromDuration;
        if (waitSeconds <= 0f)
        {
            waitSeconds = GetAnimationClipLength(fromBool);
            if (waitSeconds <= 0f)
            {
                Debug.LogWarning($"[NoahAnimController2] 无法找到动画剪辑 '{fromBool}' 的时长，使用 0.1 秒回退等待。");
                waitSeconds = 0.1f;
            }
        }

        yield return new WaitForSeconds(waitSeconds);

        if (animator == null)
            yield break;

        animator.SetBool(fromBool, false);

        var clip = FindAnimationClip(toStateName);
        if (clip != null)
        {
            animator.Play(clip.name, 0, Mathf.Clamp01(startSeconds / clip.length));
            Debug.Log($"[NoahAnimController2] 🎬 从 {startSeconds}s 开始播放 {clip.name}，时长 {durationSeconds}s");
        }
        else
        {
            animator.SetBool(toStateName, true);
            Debug.LogWarning($"[NoahAnimController2] 未找到动画剪辑 '{toStateName}'，改为 SetBool 触发播放。");
        }

        yield return new WaitForSeconds(durationSeconds);

        if (animator != null)
        {
            animator.SetBool(toStateName, false);
            Debug.Log($"[NoahAnimController2] ⏱️ {toStateName} 片段播放完成后已复位");
        }
    }

    /// <summary>将诺亚博士 Animator 中所有自定义 Bool 重置为 false。</summary>
    void ResetAllBools()
    {
        if (animator == null) return;

        animator.SetBool("isWalking", false);
        animator.SetBool("isGreeting", false);
        animator.SetBool("isDisappointed", false);
        animator.SetBool("isIntroducing", false);
        animator.SetBool("isNodding", false);
        animator.SetBool("isAcknowledging", false);
        animator.SetBool("isIntroNumber", false);
        animator.SetBool("isIntroUpper", false);
        animator.SetBool("isLowerupper", false);

        Debug.Log("[NoahAnimController2] 🔄 ResetAllBools 完成，所有 Bool → false");
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