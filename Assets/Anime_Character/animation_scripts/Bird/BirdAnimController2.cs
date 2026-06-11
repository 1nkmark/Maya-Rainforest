using UnityEngine;
using System.Collections;

/// <summary>
/// 胖胖鸟动画控制器 —— 第二幕（玛雅数字）
///
/// 全程 isHovering=true。
///
/// 朝向优先级（Update）：
///   巡航中 > 看slab > 看Noah > 看玩家（默认）
///
/// 胖胖台词触发：
///   "大板子/石碑"        → 看slab，台词结束回看玩家
///   "进位区/什么时候用"   → 看slab，台词结束回看玩家
///   "上面点一下/画圈/20" → 看slab，台词结束回看玩家
///   "玛雅文明/敞开/进去" → 停止看slab，台词结束等3秒后飞向cruiseTarget
///
/// 诺亚台词触发：
///   "玛雅数字不只是抽象数学…"  → 胖胖看向Noah，台词结束回看玩家
///   "当数字大于等于20时"        → 胖胖看向Noah，台词结束回看玩家
/// </summary>
public class BirdAnimController2 : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Inspector
    // ------------------------------------------------------------------ //

    [Header("石碑方向空物体（slab）")]
    public Transform slabTarget;

    [Header("诺亚的 Transform（用于胖胖看向诺亚）")]
    public Transform noahTransform;

    [Header("巡航目标（门口/出口方向空物体）")]
    public Transform cruiseTarget;

    [Header("巡航移动速度")]
    public float cruiseSpeed = 3f;

    [Header("身体转向速度（Slerp 系数，越大越快）")]
    public float turnSpeed = 5f;

    [Header("最后一句台词结束后等待几秒再起飞")]
    public float waitBeforeCruise = 3f;

    // ------------------------------------------------------------------ //
    //  私有字段
    // ------------------------------------------------------------------ //

    private Animator                animator;
    private DialogueSystem          dialogueSystem;
    private DialogueSystemAct2      dialogueSystemAct2;
    private DialogueSystemMayaGlyph dialogueSystemMayaGlyph;

    private Camera mainCamera;

    /// <summary>朝向状态枚举，决定 Update 里的旋转目标</summary>
    private enum LookState { Player, Slab, Noah }
    private LookState lookState = LookState.Player;

    /// <summary>是否正在巡航（巡航协程自己控制旋转）</summary>
    private bool isCruising = false;

    private enum BirdLine { None, Slab1, Slab2, Slab3, Last }
    private BirdLine currentBirdLine = BirdLine.None;

    private enum NoahLine { None, MayaMath, GreaterThan20 }
    private NoahLine currentNoahLine = NoahLine.None;

    private Coroutine cruiseCoroutine;

    // ------------------------------------------------------------------ //
    //  生命周期
    // ------------------------------------------------------------------ //

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) Debug.LogError("❌ BirdAnimController2：找不到 Animator！");

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
        else
        {
            Debug.LogError("❌ 找不到 DialogueManager！");
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("⚠️ BirdAnimController2：找不到 Main Camera");

        // 全程保持悬停
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
    //  Update：根据 lookState 平滑转向对应目标
    // ------------------------------------------------------------------ //

    void Update()
    {
        if (isCruising) return;     // 巡航时旋转由协程控制

        Vector3 lookDir = GetLookDirection();
        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        transform.rotation   = Quaternion.Slerp(
            transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    /// <summary>根据当前 lookState 返回水平朝向向量</summary>
    Vector3 GetLookDirection()
    {
        Transform lookTarget = null;

        switch (lookState)
        {
            case LookState.Slab:
                lookTarget = slabTarget;
                break;
            case LookState.Noah:
                lookTarget = noahTransform;
                break;
            case LookState.Player:
            default:
                if (mainCamera != null) lookTarget = mainCamera.transform;
                break;
        }

        if (lookTarget == null) return Vector3.zero;

        Vector3 dir = lookTarget.position - transform.position;
        dir.y = 0f;     // 水平旋转，不仰头/低头
        return dir;
    }

    // ------------------------------------------------------------------ //
    //  对话系统回调
    // ------------------------------------------------------------------ //

    void OnLineStart(string speaker, string content)
    {
        content = content ?? string.Empty;

        // ── 胖胖台词 ─────────────────────────────────────────────────
        if (IsBirdSpeaker(speaker))
        {
            Debug.Log($"【BirdAct2 胖胖台词开始】{content}");

            // "哦看哪，这面大板子莫非就是传闻中的玛雅数字石碑？"
            if (content.Contains("大板子") || content.Contains("石碑"))
            {
                currentBirdLine = BirdLine.Slab1;
                lookState       = LookState.Slab;
            }
            // "刚才只用到了下面的大区域，那么上面的进位区什么时候用呢？"
            else if (content.Contains("进位区") || content.Contains("什么时候用"))
            {
                currentBirdLine = BirdLine.Slab2;
                lookState       = LookState.Slab;
            }
            // "上面点一下，下面画圈。这个就是 20！"
            else if (content.Contains("上面点一下") || content.Contains("画圈")
                  || content.Contains("这个就是"))
            {
                currentBirdLine = BirdLine.Slab3;
                lookState       = LookState.Slab;
            }
            // "看！玛雅文明已经向我们敞开了大门，我们进去一探究竟吧！"
            else if (content.Contains("玛雅文明已经") || content.Contains("敞开了大门")
                  || content.Contains("进去一探究竟"))
            {
                currentBirdLine = BirdLine.Last;
                lookState       = LookState.Player;     // 转回看玩家，准备起飞
            }
            else
            {
                lookState = LookState.Player;           // 默认看玩家
            }
        }

        // ── 诺亚台词 ─────────────────────────────────────────────────
        else if (IsNoahSpeaker(speaker))
        {
            // "玛雅数字不只是抽象数学，还是对自然节律的记录…"
            if (content.Contains("玛雅数字不只是") || content.Contains("自然节律")
             || content.Contains("生长周期"))
            {
                currentNoahLine = NoahLine.MayaMath;
                lookState       = LookState.Noah;
                Debug.Log("胖胖鸟（幕2）：诺亚讲数学文化 → 看向诺亚");
            }
            // "当数字大于等于20时"
            else if (content.Contains("大于等于20") || content.Contains("大于等于２０"))
            {
                currentNoahLine = NoahLine.GreaterThan20;
                lookState       = LookState.Noah;
                Debug.Log("胖胖鸟（幕2）：诺亚讲进位 → 看向诺亚");
            }
        }
    }

    void OnLineEnd(string speaker)
    {
        // ── 胖胖台词结束 ─────────────────────────────────────────────
        if (IsBirdSpeaker(speaker))
        {
            switch (currentBirdLine)
            {
                case BirdLine.Slab1:
                case BirdLine.Slab2:
                case BirdLine.Slab3:
                    // 石碑相关台词说完，回看玩家
                    lookState = LookState.Player;
                    Debug.Log("胖胖鸟（幕2）：slab台词结束 → 回看玩家");
                    break;

                case BirdLine.Last:
                    // 最后一句说完，等待后巡航
                    Debug.Log($"胖胖鸟（幕2）：最后一句结束，{waitBeforeCruise}秒后起飞");
                    if (cruiseCoroutine != null) StopCoroutine(cruiseCoroutine);
                    cruiseCoroutine = StartCoroutine(WaitThenCruise());
                    break;
            }
            currentBirdLine = BirdLine.None;
        }

        // ── 诺亚台词结束 ─────────────────────────────────────────────
        else if (IsNoahSpeaker(speaker))
        {
            if (currentNoahLine == NoahLine.MayaMath
             || currentNoahLine == NoahLine.GreaterThan20)
            {
                lookState = LookState.Player;
                Debug.Log("胖胖鸟（幕2）：诺亚台词结束 → 回看玩家");
            }
            currentNoahLine = NoahLine.None;
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

    // ------------------------------------------------------------------ //
    //  等待后巡航
    // ------------------------------------------------------------------ //

    IEnumerator WaitThenCruise()
    {
        yield return new WaitForSeconds(waitBeforeCruise);

        if (cruiseTarget == null)
        {
            Debug.LogWarning("⚠️ BirdAnimController2：cruiseTarget 未赋值，跳过巡航");
            yield break;
        }

        isCruising = true;
        Debug.Log("胖胖鸟（幕2）：开始飞向 Target");

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

        transform.position = cruiseTarget.position;
        isCruising         = false;
        cruiseCoroutine    = null;
        Debug.Log("胖胖鸟（幕2）：已到达 Target，悬停待命");
    }
}