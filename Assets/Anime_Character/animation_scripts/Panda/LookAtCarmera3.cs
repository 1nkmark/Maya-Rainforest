using UnityEngine;

/// <summary>
/// 玛雅文字关卡 —— 胖达视线控制器
/// 根据 DialogueSystemMayaGlyph 广播的台词关键词，在四个注视目标之间切换：
///   · 玩家（Camera）        → 胖达直接向玩家说话 / 操作指引
///   · 诺亚（Noah）          → 胖达附和/回应诺亚
///   · 上方（upper）         → 胖达抬头看向漂浮的文字/转盘（说"表情包"那句）
///   · 默认点（defaultpoint） → 无对话 / Idle 状态
///
/// 旋转只影响 Y 轴，角色始终保持站立姿态不倾斜。
/// 依赖：场景中存在名为 "DialogueManager" 的 GameObject，挂载 DialogueSystemMayaGlyph 组件。
/// </summary>
public class LookAtCamera3 : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 配置
    // ─────────────────────────────────────────────
    [Header("注视目标")]
    [Tooltip("诺亚博士的 Transform（直接拖入）")]
    public Transform noahTransform;

    [Tooltip("默认朝向的空物体（在场景中新建一个空物体命名为 defaultpoint 后拖入）")]
    public Transform defaultPoint;

    [Tooltip("胖达说'表情包'时看向的上方空物体（场景中命名为 upper）")]
    public Transform upperTransform;

    [Header("旋转参数")]
    [Tooltip("转向目标的插值速度，值越大转得越快")]
    public float rotateSpeed = 5f;

    // ─────────────────────────────────────────────
    //  私有状态
    // ─────────────────────────────────────────────
    private enum LookTarget { Default, Player, Noah, Upper }
    private LookTarget currentTarget = LookTarget.Default;

    private Transform cameraTransform;
    private DialogueSystemMayaGlyph dialogueSystem;

    // ─────────────────────────────────────────────
    //  Unity 生命周期
    // ─────────────────────────────────────────────
    void Start()
    {
        cameraTransform = Camera.main.transform;

        // 自动查找诺亚（如果 Inspector 未手动拖入）
        if (noahTransform == null)
        {
            GameObject noahObj = GameObject.Find("Noah");
            if (noahObj != null)
                noahTransform = noahObj.transform;
            else
                Debug.LogWarning("⚠️ [LookAtCamera3] 找不到名为 'Noah' 的物体，请在 Inspector 中手动拖入 noahTransform。");
        }

        // 自动查找 defaultpoint（如果 Inspector 未手动拖入）
        if (defaultPoint == null)
        {
            GameObject dp = GameObject.Find("defaultpoint");
            if (dp != null)
                defaultPoint = dp.transform;
            else
                Debug.LogWarning("⚠️ [LookAtCamera3] 找不到名为 'defaultpoint' 的物体，请在 Inspector 中手动拖入 defaultPoint。");
        }

        // 自动查找 upper（如果 Inspector 未手动拖入）
        if (upperTransform == null)
        {
            GameObject up = GameObject.Find("upper");
            if (up != null)
                upperTransform = up.transform;
            else
                Debug.LogWarning("⚠️ [LookAtCamera3] 找不到名为 'upper' 的物体，请在 Inspector 中手动拖入 upperTransform。");
        }

        // 注册对话事件
        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystemMayaGlyph>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd += OnLineEnd;
                Debug.Log("✅ [LookAtCamera3] DialogueSystemMayaGlyph 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [LookAtCamera3] DialogueManager 上没有 DialogueSystemMayaGlyph 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [LookAtCamera3] 场景中找不到 DialogueManager！");
        }
    }

    void Update()
    {
        Transform target = ResolveCurrentTarget();
        if (target == null) return;

        RotateToward(target.position);
    }

    // ─────────────────────────────────────────────
    //  对话事件回调
    // ─────────────────────────────────────────────

    void OnLineStart(string speaker, string content)
    {
        if (speaker == "胖达")
        {
            // ── 看向 upper：胖达抬头看漂浮的符号/转盘 ──────────────────────

            // "诺亚，这绕着我们转圈的'表情包'是什么呀！"
            // "诺亚，这些刻着奇怪符号的轮盘是干什么用的？"
            if (content.Contains("表情包") || content.Contains("奇怪符号的轮盘"))
            {
                SetTarget(LookTarget.Upper, "看向upper（抬头看漂浮符号/转盘）");
            }

            // ── 看向玩家：胖达直接对玩家发出操作指引 ──────────────────────

            // "这次的规矩变了。看准目标后，用你的左手射线……"
            // "先用你的左手射线指着这位玉米神的侧脸……"
            // "然后左手摇杆往前推，把他稳稳地送进……"
            // "快，再次按住你的左手中指，抓住那个……"
            // "快，按住左手中指抓住最后这个'斑点豹头'……"
            else if (content.Contains("左手射线") ||
                content.Contains("左手中指") ||
                content.Contains("左手摇杆") ||
                content.Contains("伙计") ||
                content.Contains("快，按住") ||
                content.Contains("快，再次"))
            {
                SetTarget(LookTarget.Player, "看向玩家（操作引导）");
            }

            // ── 看向诺亚：胖达附和或回应诺亚的讲解 ──────────────────────

            // "诺亚，这绕着我们转圈的'表情包'是什么呀！"
            // "所以他们对玉米简直崇拜到了骨子里，对吧？"
            // "喔！'盾牌'听起来就很厚实！"
            // "我也看到了！中间还有个像眼睛一样的图案。"
            // "嘿，这不就是序幕里追得我们满地找牙的那位嘛！"
            else if (content.Contains("诺亚") ||
                     content.Contains("对吧") ||
                     content.Contains("崇拜到了骨子里") ||
                     content.Contains("我也看到了") ||
                     content.Contains("盾牌") && content.Contains("厚实") ||
                     content.Contains("满地找牙"))
            {
                SetTarget(LookTarget.Noah, "看向诺亚（附和/回应）");
            }

            // ── 看向玩家（默认）：其余胖达台词朝向玩家 ───────────────────
            else
            {
                SetTarget(LookTarget.Player, "看向玩家（胖达其他台词）");
            }
        }
        else if (speaker == "诺亚")
        {
            // 诺亚说话时，胖达侧身朝向诺亚（表示在听）
            SetTarget(LookTarget.Noah, "看向诺亚（在听诺亚说话）");
        }
        else
        {
            // 其他说话者（国王幽灵等），胖达朝向默认点
            SetTarget(LookTarget.Default, "看向默认点（其他角色在说话）");
        }
    }

    void OnLineEnd(string speaker)
    {
        // 任何人的台词结束后，胖达回到默认朝向
        SetTarget(LookTarget.Default, $"台词结束，回到默认点 | 说话人：{speaker}");
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    /// <summary>设置当前注视目标并打印日志。</summary>
    void SetTarget(LookTarget target, string description)
    {
        currentTarget = target;
        Debug.Log($"[LookAtCamera3] 👀 视线目标 → {target} | {description}");
    }

    /// <summary>根据当前枚举值返回对应的 Transform。</summary>
    Transform ResolveCurrentTarget()
    {
        switch (currentTarget)
        {
            case LookTarget.Player:
                return cameraTransform;

            case LookTarget.Noah:
                return noahTransform;

            case LookTarget.Upper:
                return upperTransform;

            case LookTarget.Default:
            default:
                return defaultPoint;
        }
    }

    /// <summary>只绕 Y 轴平滑转向目标位置，保持角色站立姿态。</summary>
    void RotateToward(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotateSpeed
        );
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