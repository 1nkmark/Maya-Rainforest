using UnityEngine;
using System.Collections;

/// <summary>
/// 胖胖鸟动画控制器 —— 第三幕（历法）
///
/// 全程 isHovering=true。
///
/// 朝向优先级（Update）：巡航中 > 看Wheel > 看Noah > 看玩家（默认）
///
/// 触发时序：
///   国王 "时间的齿轮" 台词结束     → 飞向 birdPoint2
///   胖胖 "轮盘/奇怪符号"           → 看Wheel
///   诺亚 "宇宙呼吸/历法盘"         → 看Noah
///   诺亚 "神历/太阳历"             → 看Wheel
///   胖胖 "时间是个圆圈/2012"       → 看Noah
///   胖胖 "左边内圈/神历的日数"     → 看Wheel
///   胖胖 "找四个点/交给你了"       → 看玩家
///   胖胖 "咔哒/左边内圈搞定"       → 看Wheel
///   胖胖 "帮你转轮盘"              → 看Wheel
/// </summary>
public class BirdAnimController4 : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Inspector
    // ------------------------------------------------------------------ //

    [Header("── 必填：场景引用 ──")]
    [SerializeField] public Transform birdPoint2;           // 国王台词后胖胖飞到的位置
    [SerializeField] public Transform wheelTarget;          // 轮盘空物体
    [SerializeField] public Transform noahTransform;        // 诺亚角色 Transform
    [SerializeField] public Transform target2;              // "满格"台词后飞到的位置
    [SerializeField] public Transform featheredSerpentTarget; // 羽蛇神空物体（到达target2后看向）

    [Header("巡航移动速度")]
    public float cruiseSpeed = 3f;

    [Header("身体转向速度（Slerp 系数）")]
    public float turnSpeed = 5f;

    // ------------------------------------------------------------------ //
    //  私有字段
    // ------------------------------------------------------------------ //

    private Animator                animator;
    private DialogueSystem          dialogueSystem;
    private DialogueSystemAct2      dialogueSystemAct2;
    private DialogueSystemMayaGlyph dialogueSystemMayaGlyph;

    private Camera mainCamera;
    private bool   isCruising = false;
    private Coroutine cruiseCoroutine;

    private enum LookState { Player, Wheel, Noah, FeatheredSerpent }
    private LookState lookState = LookState.Player;

    /// <summary>记录国王当前台词内容，供 OnLineEnd 判断是否触发飞行</summary>
    private string currentKingLineContent = null;

    /// <summary>胖胖正在说"文化共鸣仪满格"台词，结束时飞向 target2</summary>
    private bool pendingFlyToTarget2 = false;

    // ------------------------------------------------------------------ //
    //  生命周期
    // ------------------------------------------------------------------ //

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) Debug.LogError("❌ BirdAnimController4：找不到 Animator！");

        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd             += OnLineEnd;
            }
            dialogueSystemAct2 = dialogRoot.GetComponent<DialogueSystemAct2>();
            if (dialogueSystemAct2 != null)
            {
                dialogueSystemAct2.OnLineStartWithContent += OnLineStart;
                dialogueSystemAct2.OnLineEnd             += OnLineEnd;
            }
            dialogueSystemMayaGlyph = dialogRoot.GetComponent<DialogueSystemMayaGlyph>();
            if (dialogueSystemMayaGlyph != null)
            {
                dialogueSystemMayaGlyph.OnLineStartWithContent += OnLineStart;
                dialogueSystemMayaGlyph.OnLineEnd             += OnLineEnd;
            }
        }
        else Debug.LogError("❌ 找不到 DialogueManager！");
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("⚠️ BirdAnimController4：找不到 Main Camera");
    }

    void OnEnable()
    {
        if (animator != null)
            animator.SetBool("isHovering", true);
    }

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
    }

    // ------------------------------------------------------------------ //
    //  Update：朝向目标平滑旋转
    // ------------------------------------------------------------------ //

    void Update()
    {
        if (isCruising) return;

        Vector3 dir = GetLookDirection();
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation   = Quaternion.Slerp(
            transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    Vector3 GetLookDirection()
    {
        Transform target = null;
        switch (lookState)
        {
            case LookState.Wheel:           target = wheelTarget;             break;
            case LookState.Noah:            target = noahTransform;           break;
            case LookState.FeatheredSerpent:target = featheredSerpentTarget;  break;
            default:
                if (mainCamera != null) target = mainCamera.transform;
                break;
        }
        if (target == null) return Vector3.zero;
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        return dir;
    }

    // ------------------------------------------------------------------ //
    //  对话系统回调
    // ------------------------------------------------------------------ //

    void OnLineStart(string speaker, string content)
    {
        content = content ?? string.Empty;

        // ── 国王台词：记录内容，OnLineEnd 里判断是否飞行 ─────────────
        if (IsKingSpeaker(speaker))
        {
            currentKingLineContent = content;
            return;
        }

        // ── 胖胖台词 ─────────────────────────────────────────────────
        if (IsBirdSpeaker(speaker))
        {
            // "看哪，我们的文化共鸣仪满格了！"
            if (content.Contains("文化共鸣仪满格") || content.Contains("满格"))
            {
                pendingFlyToTarget2 = true;
                lookState = LookState.Player;
            }

            // "诺亚，这些刻着奇怪符号的轮盘是干什么用的？"
            else if (content.Contains("轮盘") || content.Contains("奇怪符号"))
                lookState = LookState.Wheel;

            // "等等，诺亚！如果时间是个圆圈，那大家传得沸沸扬扬的'2012世界末日'是怎么回事？"
            else if (content.Contains("时间是个圆圈") || content.Contains("2012")
                  || content.Contains("世界末日"))
                lookState = LookState.Noah;

            // "左边内圈是神历的日数，数字 4，这个简单！"
            else if (content.Contains("左边内圈") || content.Contains("神历的日数")
                  || content.Contains("数字 4") || content.Contains("数字4"))
                lookState = LookState.Wheel;

            // "找四个点对吧？探险家，交给你了！"
            else if (content.Contains("找四个点") || content.Contains("交给你了"))
                lookState = LookState.Player;

            // "咔哒一声！左边内圈搞定！"
            else if (content.Contains("咔哒") || content.Contains("内圈搞定"))
                lookState = LookState.Wheel;

            // "探险家，我来帮你转轮盘"
            else if (content.Contains("帮你转轮盘") || content.Contains("我来帮你转"))
                lookState = LookState.Wheel;

            return;
        }

        // ── 诺亚台词 ─────────────────────────────────────────────────
        if (IsNoahSpeaker(speaker))
        {
            // "这是玛雅人用来记录'宇宙呼吸'的历法盘！...周而复始的齿轮。"
            if (content.Contains("宇宙呼吸") || content.Contains("历法盘")
             || content.Contains("周而复始"))
                lookState = LookState.Noah;

            // "左边这组是 260 天的【神历】...右边是365 天的【太阳历】..."
            else if (content.Contains("神历") || content.Contains("太阳历")
                  || content.Contains("260") || content.Contains("365"))
                lookState = LookState.Wheel;
        }
    }

    void OnLineEnd(string speaker)
    {
        // ── 胖胖台词结束：满格台词 → 飞向 target2 ────────────────────
        if (IsBirdSpeaker(speaker) && pendingFlyToTarget2)
        {
            pendingFlyToTarget2 = false;
            Debug.Log("胖胖鸟（幕4）：满格台词结束 → 飞向 target2");
            if (cruiseCoroutine != null) StopCoroutine(cruiseCoroutine);
            cruiseCoroutine = StartCoroutine(FlyToTarget2());
        }

        // ── 国王台词结束：若包含触发关键词，飞向 birdPoint2 ──────────
        if (IsKingSpeaker(speaker))
        {
            if (currentKingLineContent != null
             && (currentKingLineContent.Contains("时间的齿轮")
              || currentKingLineContent.Contains("左边的石墙")
              || currentKingLineContent.Contains("藏在")))
            {
                Debug.Log("胖胖鸟（幕4）：国王台词结束 → 飞向 birdPoint2");
                if (cruiseCoroutine != null) StopCoroutine(cruiseCoroutine);
                cruiseCoroutine = StartCoroutine(FlyToBirdPoint2());
            }
            currentKingLineContent = null;
        }
    }

    // ------------------------------------------------------------------ //
    //  Speaker 判断
    // ------------------------------------------------------------------ //

    bool IsBirdSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker)) return false;
        return speaker.Trim().Replace(" ", "").Contains("胖胖");
    }

    bool IsNoahSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker)) return false;
        return speaker.Trim().Replace(" ", "").Contains("诺亚");
    }

    bool IsKingSpeaker(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker)) return false;
        string s = speaker.Trim().Replace(" ", "");
        return s.Contains("国王") || s.Contains("虚声");
    }

    // ------------------------------------------------------------------ //
    //  飞向 birdPoint2 协程
    // ------------------------------------------------------------------ //

    IEnumerator FlyToBirdPoint2()
    {
        if (birdPoint2 == null)
        {
            Debug.LogWarning("⚠️ BirdAnimController4：birdPoint2 未赋值，跳过飞行");
            yield break;
        }

        isCruising = true;

        while (Vector3.Distance(transform.position, birdPoint2.position) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                birdPoint2.position,
                cruiseSpeed * Time.deltaTime);

            Vector3 dir = (birdPoint2.position - transform.position).normalized;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            yield return null;
        }

        transform.position = birdPoint2.position;
        isCruising         = false;
        cruiseCoroutine    = null;
        lookState          = LookState.Player;  // 到达后默认看玩家
        Debug.Log("胖胖鸟（幕4）：已到达 birdPoint2");
    }

    // ------------------------------------------------------------------ //
    //  飞向 target2 协程（满格后 → 看向羽蛇神）
    // ------------------------------------------------------------------ //

    IEnumerator FlyToTarget2()
    {
        if (target2 == null)
        {
            Debug.LogWarning("⚠️ BirdAnimController4：target2 未赋值，跳过飞行");
            yield break;
        }

        isCruising = true;

        while (Vector3.Distance(transform.position, target2.position) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target2.position,
                cruiseSpeed * Time.deltaTime);

            Vector3 dir = (target2.position - transform.position).normalized;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            yield return null;
        }

        transform.position = target2.position;
        isCruising         = false;
        cruiseCoroutine    = null;
        lookState          = LookState.FeatheredSerpent;   // 到达后看向羽蛇神
        Debug.Log("胖胖鸟（幕4）：已到达 target2，看向羽蛇神");
    }
}