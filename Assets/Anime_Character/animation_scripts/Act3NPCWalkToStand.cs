using System.Collections;
using UnityEngine;

/// <summary>
/// 第三幕历法关卡 —— NPC 入场走位控制器
/// 监听 DialogueSystemMayaGlyph 事件：
/// 当胖达说出"精密的石头机关"这句台词时，
/// 驱动胖达走向 standpointpanda、诺亚走向 standpointnoah。
///
/// 移动方式：协程逐帧 MoveTowards，不依赖 NavMesh。
/// 移动期间自动切换 isWalking=true，到位后还原 false。
/// </summary>
public class Act3NPCWalkToStand : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector 配置
    // ─────────────────────────────────────────────
    [Header("NPC 引用")]
    [Tooltip("胖达的 GameObject（挂有 Animator）")]
    public Transform panda;

    [Tooltip("诺亚的 GameObject（挂有 Animator）")]
    public Transform noah;

    [Header("目标站位（空物体）")]
    [Tooltip("胖达的目标站位，场景中空物体命名为 standpointpanda")]
    public Transform standpointPanda;

    [Tooltip("诺亚的目标站位，场景中空物体命名为 standpointnoah")]
    public Transform standpointNoah;

    [Header("移动参数")]
    [Tooltip("行走速度（单位/秒）")]
    public float walkSpeed = 1.5f;

    [Tooltip("判定「到位」的距离阈值（米）")]
    public float arrivalThreshold = 0.08f;

    // ─────────────────────────────────────────────
    //  私有引用
    // ─────────────────────────────────────────────
    private Animator pandaAnim;
    private Animator noahAnim;
    private DialogueSystemMayaGlyph dialogueSystem;

    private bool hasTriggered = false; // 只触发一次

    // ─────────────────────────────────────────────
    //  Unity 生命周期
    // ─────────────────────────────────────────────
    void Start()
    {
        // ── 自动查找 NPC（Inspector 未拖入时兜底）──
        if (panda == null)
        {
            GameObject go = GameObject.Find("Panda");
            if (go != null) panda = go.transform;
            else Debug.LogWarning("⚠️ [Act3NPCWalkToStand] 找不到 'Panda'，请手动拖入。");
        }
        if (noah == null)
        {
            GameObject go = GameObject.Find("Noah");
            if (go != null) noah = go.transform;
            else Debug.LogWarning("⚠️ [Act3NPCWalkToStand] 找不到 'Noah'，请手动拖入。");
        }

        // ── 自动查找目标站位（Inspector 未拖入时兜底）──
        if (standpointPanda == null)
        {
            GameObject go = GameObject.Find("standpointpanda");
            if (go != null) standpointPanda = go.transform;
            else Debug.LogWarning("⚠️ [Act3NPCWalkToStand] 找不到 'standpointpanda'，请手动拖入。");
        }
        if (standpointNoah == null)
        {
            GameObject go = GameObject.Find("standpointnoah");
            if (go != null) standpointNoah = go.transform;
            else Debug.LogWarning("⚠️ [Act3NPCWalkToStand] 找不到 'standpointnoah'，请手动拖入。");
        }

        // ── 获取 Animator ──
        if (panda != null)
        {
            pandaAnim = panda.GetComponent<Animator>();
            if (pandaAnim == null) pandaAnim = panda.GetComponentInChildren<Animator>();
        }
        if (noah != null)
        {
            noahAnim = noah.GetComponent<Animator>();
            if (noahAnim == null) noahAnim = noah.GetComponentInChildren<Animator>();
        }

        // ── 注册对话事件 ──
        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystemMayaGlyph>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                Debug.Log("✅ [Act3NPCWalkToStand] DialogueSystemMayaGlyph 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [Act3NPCWalkToStand] DialogueManager 上没有 DialogueSystemMayaGlyph 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [Act3NPCWalkToStand] 场景中找不到 DialogueManager！");
        }
    }

    // ─────────────────────────────────────────────
    //  对话事件回调
    // ─────────────────────────────────────────────
    void OnLineStart(string speaker, string content)
    {
        if (hasTriggered) return;

        // 触发条件：胖达说出历法关卡开场台词
        if (speaker == "胖达" &&
            (content.Contains("精密的石头机关") || content.Contains("奇怪符号的轮盘")))
        {
            hasTriggered = true;
            Debug.Log("[Act3NPCWalkToStand] 🚶 触发入场走位");

            if (panda != null && standpointPanda != null)
                StartCoroutine(WalkTo(panda, pandaAnim, standpointPanda, "胖达"));

            if (noah != null && standpointNoah != null)
                StartCoroutine(WalkTo(noah, noahAnim, standpointNoah, "诺亚"));
        }
    }

    // ─────────────────────────────────────────────
    //  走位协程
    // ─────────────────────────────────────────────

    /// <summary>
    /// 让 npc 平滑移动到 target 位置。
    /// 移动途中：isWalking = true，面朝移动方向。
    /// 到位后：isWalking = false，旋转对齐 target 的朝向（若有）。
    /// </summary>
    IEnumerator WalkTo(Transform npc, Animator anim, Transform target, string npcName)
    {
        // 开始行走动画
        SetWalking(anim, true);
        Debug.Log($"[Act3NPCWalkToStand] 🚶 {npcName} 开始走向 {target.name}");

        while (true)
        {
            float dist = Vector3.Distance(npc.position, target.position);
            if (dist <= arrivalThreshold)
                break;

            // 逐帧向目标移动（只在 XZ 平面，保留原 Y 值）
            Vector3 targetPos = new Vector3(target.position.x, npc.position.y, target.position.z);
            npc.position = Vector3.MoveTowards(npc.position, targetPos, walkSpeed * Time.deltaTime);

            // 面朝移动方向（只转 Y 轴）
            Vector3 dir = (targetPos - npc.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                npc.rotation = Quaternion.Slerp(npc.rotation, lookRot, Time.deltaTime * 8f);
            }

            yield return null;
        }

        // 到位：对齐到目标站位的精确坐标和朝向
        npc.position = new Vector3(target.position.x, npc.position.y, target.position.z);
        npc.rotation = target.rotation;

        // 停止行走动画
        SetWalking(anim, false);
        Debug.Log($"[Act3NPCWalkToStand] ✅ {npcName} 到位：{target.name}");
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    void SetWalking(Animator anim, bool value)
    {
        if (anim == null) return;
        anim.SetBool("isWalking", value);
    }

    // ─────────────────────────────────────────────
    //  清理
    // ─────────────────────────────────────────────
    void OnDestroy()
    {
        if (dialogueSystem != null)
            dialogueSystem.OnLineStartWithContent -= OnLineStart;
    }
}