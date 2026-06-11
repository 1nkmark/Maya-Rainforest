using UnityEngine;

/// <summary>
/// 玛雅文字关卡 —— 诺亚博士视线控制器
/// 根据 DialogueSystemMayaGlyph 广播的台词关键词，在三个注视目标之间切换：
///   · 玩家（Camera）   → 诺亚直接对玩家讲解 / 肯定玩家操作
///   · 胖达（Panda）    → 诺亚回应胖达的提问 / 附和
///   · 默认点（defaultpoint） → 无对话 / Idle 状态
///
/// 旋转只影响 Y 轴，角色始终保持站立姿态不倾斜。
/// 依赖：场景中存在名为 "DialogueManager" 的 GameObject，挂载 DialogueSystemMayaGlyph 组件。
/// </summary>
public class NoahLookAt : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 配置
    // ─────────────────────────────────────────────
    [Header("注视目标")]
    [Tooltip("胖达的 Transform（直接拖入）")]
    public Transform pandaTransform;

    [Tooltip("默认朝向的空物体（场景中名为 defaultpoint 的空物体）")]
    public Transform defaultPoint;

    [Header("旋转参数")]
    [Tooltip("转向目标的插值速度，值越大转得越快")]
    public float rotateSpeed = 5f;

    // ─────────────────────────────────────────────
    //  私有状态
    // ─────────────────────────────────────────────
    private enum LookTarget { Default, Player, Panda }
    private LookTarget currentTarget = LookTarget.Default;

    private Transform cameraTransform;
    private DialogueSystemMayaGlyph dialogueSystem;

    // ─────────────────────────────────────────────
    //  Unity 生命周期
    // ─────────────────────────────────────────────
    void Start()
    {
        cameraTransform = Camera.main.transform;

        // 自动查找胖达（如果 Inspector 未手动拖入）
        if (pandaTransform == null)
        {
            GameObject pandaObj = GameObject.Find("Panda");
            if (pandaObj != null)
                pandaTransform = pandaObj.transform;
            else
                Debug.LogWarning("⚠️ [NoahLookAt] 找不到名为 'Panda' 的物体，请在 Inspector 中手动拖入 pandaTransform。");
        }

        // 自动查找 defaultpoint（如果 Inspector 未手动拖入）
        if (defaultPoint == null)
        {
            GameObject dp = GameObject.Find("defaultpoint");
            if (dp != null)
                defaultPoint = dp.transform;
            else
                Debug.LogWarning("⚠️ [NoahLookAt] 找不到名为 'defaultpoint' 的物体，请在 Inspector 中手动拖入 defaultPoint。");
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
                Debug.Log("✅ [NoahLookAt] DialogueSystemMayaGlyph 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [NoahLookAt] DialogueManager 上没有 DialogueSystemMayaGlyph 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [NoahLookAt] 场景中找不到 DialogueManager！");
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
        if (speaker == "诺亚")
        {
            // ── 看向玩家：诺亚直接向玩家讲解文化知识 / 肯定玩家操作 ──────

            // "探险家，注意看。我们要寻找的第一块碎片……"
            // "做得好。生存的根基已定……"
            // "帕伦克的尊严归位了……"
            // "探险家，注意观察这些文字的细节……"
            if (content.Contains("探险家") ||
                content.Contains("做得好") ||
                content.Contains("尊严归位") ||
                content.Contains("注意观察") ||
                content.Contains("注意看"))
            {
                SetTarget(LookTarget.Player, "看向玩家（直接对探险家说话）");
            }

            // ── 看向胖达：诺亚回应胖达的提问 / 纠正胖达 ────────────────

            // "那是 Waxak，年轻的玉米神。"  ← 接胖达"那堆字符里有个侧脸特别怪的"
            // "注意看细节！这个'盾牌'的边缘……"  ← 接胖达指出盾牌
            // "在玛雅语中，PAKAL 就是'盾牌'的意思……"  ← 回应胖达感叹
            // "他的名号是 K'inich Kan Bahlam……"  ← 回应胖达"感觉严肃"
            else if (content.Contains("Waxak") ||
                     content.Contains("年轻的玉米神") ||
                     content.Contains("注意看细节") ||
                     content.Contains("PAKAL") ||
                     content.Contains("K'inich Kan Bahlam") ||
                     content.Contains("跨越千年"))
            {
                SetTarget(LookTarget.Panda, "看向胖达（回应胖达的提问/感叹）");
            }

            // ── 看向玩家（默认）：其余诺亚讲解台词面向玩家 ─────────────
            else
            {
                SetTarget(LookTarget.Player, "看向玩家（诺亚其他讲解台词）");
            }
        }
        else if (speaker == "胖达")
        {
            // 胖达说话时，诺亚侧身朝向胖达（表示在听 / 随时准备回应）
            SetTarget(LookTarget.Panda, "看向胖达（在听胖达说话）");
        }
        else
        {
            // 其他说话者（国王幽灵等），诺亚朝向默认点
            SetTarget(LookTarget.Default, "看向默认点（其他角色在说话）");
        }
    }

    void OnLineEnd(string speaker)
    {
        // 任何人的台词结束后，诺亚回到默认朝向
        SetTarget(LookTarget.Default, $"台词结束，回到默认点 | 说话人：{speaker}");
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    /// <summary>设置当前注视目标并打印日志。</summary>
    void SetTarget(LookTarget target, string description)
    {
        currentTarget = target;
        Debug.Log($"[NoahLookAt] 👀 视线目标 → {target} | {description}");
    }

    /// <summary>根据当前枚举值返回对应的 Transform。</summary>
    Transform ResolveCurrentTarget()
    {
        switch (currentTarget)
        {
            case LookTarget.Player:
                return cameraTransform;

            case LookTarget.Panda:
                return pandaTransform;

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