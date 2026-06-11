using UnityEngine;
using System.Collections;

/// <summary>
/// 胖胖鸟动画控制器（剧情引入：落地雨林）
///
/// 台词时序与动画映射：
///   胖胖1 "新面孔"    → 立即 isHovering+isSpeaking；台词结束 → 落地回原位
///   胖胖2 "行动担当"  → 落地说话；台词结束 → Idle
///   胖胖3 "文化共鸣仪"→ isHovering+isSpeaking；台词结束 → 持续悬停看向玩家
///   胖胖4 "火把"      → isHovering+isSpeaking；台词结束 → isHovering巡航 → 到达悬停
///
/// Animator Bool 命名说明：
///   isHovering  —— 悬停飞翔（说话期间、持续悬停、到达Target后）
///   isFlying 已全部替换为 isHovering，不再使用
/// </summary>
public class BirdAnimController : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Inspector
    // ------------------------------------------------------------------ //

    [Header("巡航目标（神像前的空物体）")]
    public Transform cruiseTarget;

    [Header("巡航移动速度")]
    public float cruiseSpeed = 3f;

    [Header("飞行悬停高度（相对静息位置的 Y 偏移，单位：米）")]
    public float flyHeightOffset = 1.5f;

    [Header("起飞/落地垂直移动速度")]
    public float flyRiseSpeed = 2f;

    [Header("看向玩家旋转速度（Slerp 系数）")]
    public float lookAtSpeed = 3f;

    // ------------------------------------------------------------------ //
    //  私有字段
    // ------------------------------------------------------------------ //

    private Animator                animator;
    private DialogueSystem          dialogueSystem;
    private DialogueSystemAct2      dialogueSystemAct2;
    private DialogueSystemMayaGlyph dialogueSystemMayaGlyph;

    private Vector3 idlePosition;
    private Camera  mainCamera;

    /// <summary>当前是否处于悬停状态（isHovering 锁定中）</summary>
    private bool isInHoveringState = false;

    /// <summary>正在巡航飞向 Target，Update 的悬停/朝向逻辑停用</summary>
    private bool isCruising = false;

    /// <summary>已到达 cruiseTarget，Update 悬停目标切换为 cruiseTarget</summary>
    private bool isAtTarget = false;

    private enum BirdLine { None, Pangpang1, Pangpang2, Pangpang3, Pangpang4 }
    private BirdLine currentLine = BirdLine.None;

    private Coroutine cruiseCoroutine;
    private Coroutine returnCoroutine;

    // ------------------------------------------------------------------ //
    //  生命周期
    // ------------------------------------------------------------------ //

    void Start()
    {
        idlePosition = transform.position;
        mainCamera   = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("⚠️ 胖胖鸟：找不到 Main Camera");
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) Debug.LogError("❌ 胖胖鸟找不到 Animator 组件！");

        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd             += OnLineEnd;
                Debug.Log("✅ 注册 DialogueSystem");
            }

            dialogueSystemAct2 = dialogRoot.GetComponent<DialogueSystemAct2>();
            if (dialogueSystemAct2 != null)
            {
                dialogueSystemAct2.OnLineStartWithContent += OnLineStart;
                dialogueSystemAct2.OnLineEnd             += OnLineEnd;
                Debug.Log("✅ 注册 DialogueSystemAct2");
            }

            dialogueSystemMayaGlyph = dialogRoot.GetComponent<DialogueSystemMayaGlyph>();
            if (dialogueSystemMayaGlyph != null)
            {
                dialogueSystemMayaGlyph.OnLineStartWithContent += OnLineStart;
                dialogueSystemMayaGlyph.OnLineEnd             += OnLineEnd;
                Debug.Log("✅ 注册 DialogueSystemMayaGlyph");
            }
        }
        else
        {
            Debug.LogError("❌ 找不到 DialogueManager！");
        }
    }

    void OnEnable()  { GameEvents.OnPandaCruiseStarted += OnBirdCruiseStarted; }
    void OnDisable() { GameEvents.OnPandaCruiseStarted -= OnBirdCruiseStarted; }

    void OnDestroy()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnLineStartWithContent -= OnLineStart;
            dialogueSystem.OnLineEnd             -= OnLineEnd;
        }
        if (dialogueSystemAct2 != null)
        {
            dialogueSystemAct2.OnLineStartWithContent -= OnLineStart;
            dialogueSystemAct2.OnLineEnd             -= OnLineEnd;
        }
        if (dialogueSystemMayaGlyph != null)
        {
            dialogueSystemMayaGlyph.OnLineStartWithContent -= OnLineStart;
            dialogueSystemMayaGlyph.OnLineEnd             -= OnLineEnd;
        }
        GameEvents.OnPandaCruiseStarted -= OnBirdCruiseStarted;
    }

    // ------------------------------------------------------------------ //
    //  Update：悬停位置控制 + 朝向玩家
    //  条件：isInHoveringState=true 且 isCruising=false
    // ------------------------------------------------------------------ //

    void Update()
    {
        if (!isInHoveringState || isCruising) return;

        // ── 1. 悬停目标位置 ───────────────────────────────────────────
        Vector3 hoverPos = isAtTarget && cruiseTarget != null
            ? cruiseTarget.position
            : idlePosition + Vector3.up * flyHeightOffset;

        transform.position = Vector3.MoveTowards(
            transform.position, hoverPos, flyRiseSpeed * Time.deltaTime);

        // ── 2. 水平朝向 Main Camera（玩家）───────────────────────────
        if (mainCamera != null)
        {
            Vector3 dir = mainCamera.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation  = Quaternion.Slerp(
                    transform.rotation, targetRot, lookAtSpeed * Time.deltaTime);
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  动画辅助
    // ------------------------------------------------------------------ //

    void ResetAllBools()
    {
        if (animator == null) return;
        animator.SetBool("isHopping",        false);
        animator.SetBool("isLanding",        false);
        animator.SetBool("isSpeaking",       false);
        animator.SetBool("isWalkingForward", false);
        animator.SetBool("isWalkingRight",   false);
        animator.SetBool("isWalkingLeft",    false);
        animator.SetBool("isWalkingBack",    false);
        // isHovering 由 isInHoveringState 管理
        animator.SetBool("isHovering",       isInHoveringState);
        // isFlying 只在 CruiseToTarget 里用，此处强制关闭
        animator.SetBool("isFlying",         false);
    }

    // ------------------------------------------------------------------ //
    //  对话系统回调
    // ------------------------------------------------------------------ //

    void OnLineStart(string speaker, string content)
    {
        content = content ?? string.Empty;
        if (!IsBirdSpeaker(speaker)) return;
        Debug.Log($"【胖胖台词开始】{content}");

        // ── 胖胖1：立即 isHovering=true，不经过 ResetAllBools ─────────
        // 目的：第一时间送到状态机，消除任何过渡等待
        if (content.Contains("新面孔") || content.Contains("增援"))
        {
            currentLine        = BirdLine.Pangpang1;
            isInHoveringState  = true;
            animator.SetBool("isHovering", true);   // ← 立即设置，无延迟
            //animator.SetBool("isSpeaking", true);
            Debug.Log("胖胖鸟：胖胖1 → 立即悬停+说话");
            return;
        }

        ResetAllBools();

        // ── 胖胖2：落地说话 ──────────────────────────────────────────
        if (content.Contains("行动担当") || content.Contains("地球梦之队"))
        {
            currentLine = BirdLine.Pangpang2;
            animator.SetBool("isSpeaking", true);
            Debug.Log("胖胖鸟：胖胖2 → 落地说话");
        }

        // ── 胖胖3：悬停说话，结束后持续悬停 ─────────────────────────
        else if (content.Contains("文化共鸣仪") || content.Contains("拿起"))
        {
            currentLine       = BirdLine.Pangpang3;
            isInHoveringState = true;
            animator.SetBool("isHovering", true);
            //animator.SetBool("isSpeaking", true);
            Debug.Log("胖胖鸟：胖胖3 → 悬停说话（结束后持续悬停）");
        }

        // ── 胖胖4：悬停说话，结束后 HoverLeft → isFlying 巡航 ────────
        else if (content.Contains("火把") || content.Contains("跟着"))
        {
            currentLine       = BirdLine.Pangpang4;
            isInHoveringState = true;
            animator.SetBool("isHovering", true);
            animator.SetBool("isSpeaking", true);
            Debug.Log("胖胖鸟：胖胖4 → 悬停说话（结束后巡航）");
        }

        else
        {
            Debug.Log("胖胖鸟：OnLineStart 无关键词匹配，保持当前状态");
        }
    }

    void OnLineEnd(string speaker)
    {
        if (!IsBirdSpeaker(speaker)) return;
        animator.SetBool("isSpeaking", false);

        switch (currentLine)
        {
            // ── 胖胖1 结束：停止悬停 → Landing → 平滑回静息位置 ──────
            case BirdLine.Pangpang1:
                isInHoveringState = false;
                animator.SetBool("isHovering", false);
                animator.SetBool("isLanding",  true);
                if (returnCoroutine != null) StopCoroutine(returnCoroutine);
                returnCoroutine = StartCoroutine(ReturnToIdlePosition());
                Debug.Log("胖胖鸟：胖胖1 结束 → 落地");
                break;

            // ── 胖胖2 结束：回 Idle ──────────────────────────────────
            case BirdLine.Pangpang2:
                ResetAllBools();
                Debug.Log("胖胖鸟：胖胖2 结束 → Idle");
                break;

            // ── 胖胖3 结束：保持悬停，Update 继续看向玩家 ────────────
            case BirdLine.Pangpang3:
                Debug.Log("胖胖鸟：胖胖3 结束 → 继续悬停");
                break;

            // ── 胖胖4 结束：isHovering 巡航飞向 Target ───────────────
            case BirdLine.Pangpang4:
                Debug.Log("胖胖鸟：胖胖4 结束 → 启动巡航");
                if (cruiseCoroutine != null) StopCoroutine(cruiseCoroutine);
                cruiseCoroutine = StartCoroutine(CruiseToTarget());
                break;

            default:
                if (!isInHoveringState) ResetAllBools();
                break;
        }

        currentLine = BirdLine.None;
    }

    bool IsBirdSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker)) return false;
        return speaker.Trim().Replace(" ", "").Contains("胖胖");
    }

    // ------------------------------------------------------------------ //
    //  协程：胖胖1 落地后平滑回到静息位置
    // ------------------------------------------------------------------ //

    IEnumerator ReturnToIdlePosition()
    {
        while (Vector3.Distance(transform.position, idlePosition) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, idlePosition, flyRiseSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = idlePosition;
        returnCoroutine    = null;
        Debug.Log("胖胖鸟：已回到静息位置");
    }

    // ------------------------------------------------------------------ //
    //  巡航协程：isHovering 飞向 Target → 原地悬停
    // ------------------------------------------------------------------ //

    void OnBirdCruiseStarted()
    {
        Debug.Log("胖胖鸟：收到外部巡航广播");
        if (cruiseCoroutine != null) StopCoroutine(cruiseCoroutine);
        cruiseCoroutine = StartCoroutine(CruiseToTarget());
    }

    IEnumerator CruiseToTarget()
    {
        if (cruiseTarget == null)
        {
            Debug.LogWarning("⚠️ 胖胖鸟：cruiseTarget 未赋值，跳过巡航");
            yield break;
        }

        // ── 飞向 Target（停止 Update 控制位置和朝向）─────────────────
        animator.SetBool("isHovering", true);
        isCruising = true;
        Debug.Log("胖胖鸟：isHovering，飞向 Target");

        while (Vector3.Distance(transform.position, cruiseTarget.position) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                cruiseTarget.position,
                cruiseSpeed * Time.deltaTime);

            Vector3 dir = (cruiseTarget.position - transform.position).normalized;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            yield return null;
        }

        // ── 到达 Target，原地 isHovering 悬停 ────────────────────────
        transform.position = cruiseTarget.position;
        isAtTarget      = true;   // Update 悬停目标切换为 cruiseTarget.position
        isCruising      = false;  // 重新开启 Update（悬停+看向玩家）
        cruiseCoroutine = null;
        Debug.Log("胖胖鸟：已到达 Target，isHovering 原地悬停");
    }
}