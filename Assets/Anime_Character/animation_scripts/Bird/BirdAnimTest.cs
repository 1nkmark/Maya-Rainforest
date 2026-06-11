using UnityEngine;
using System.Collections;

/// <summary>
/// 胖胖鸟动画时序测试脚本（不依赖对话系统）
///
///   [前置测试序列]
///   Pre-0: isFlying = true        → 持续 preFlyDuration  秒（测试飞行）
///   Pre-1: isHopping = true       → 持续 preHopDuration  秒（测试 hop forward）
///   Pre-2: isWalkingForward= true → 持续 preWalkDuration 秒（测试 walk forward）
///
///   [原有对话序列]
///   Step 0: 所有Bool关闭 → idle 持续 idleDuration 秒
///   Step 1: isHopping = true  → 持续 hoppingDuration 秒（胖胖1 挥翅膀）
///   Step 2: isFlying  = true  → 持续 flyingDuration  秒（胖胖2 起飞）
///   Step 3: isFlying  = true  → 持续 flyingDuration2 秒（胖胖3 飞行中）
///   Step 4: isFlying  = true  → 持续 flyingDuration3 秒（胖胖4 准备巡航）
///   Step 5: 从当前位置平移到 cruiseTarget，全程 isFlying
///   Step 6: 到达后保持 isFlying 悬停
/// </summary>
public class BirdAnimTest : MonoBehaviour
{
    [Header("──── 前置动画测试 ────")]
    public float preFlyDuration  = 8f;   // Pre-0: 飞行
    public float preHopDuration  = 5f;   // Pre-1: hop forward
    public float preWalkDuration = 5f;   // Pre-2: walk forward

    [Header("──── 原有各步骤持续时长（秒）────")]
    public float idleDuration    = 2f;   // Step0：静息 idle
    public float hoppingDuration = 2f;   // 胖胖1：挥翅膀
    public float flyingDuration  = 2f;   // 胖胖2：起飞后悬停
    public float flyingDuration2 = 2f;   // 胖胖3：飞行中
    public float flyingDuration3 = 2f;   // 胖胖4：飞行准备巡航

    [Header("巡航目标（神像前的空物体，可不填则跳过巡航）")]
    public Transform cruiseTarget;

    [Header("巡航移动速度")]
    public float cruiseSpeed = 3f;

    // ------------------------------------------------------------------ //

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("BirdAnimTest: 找不到 Animator！");
            return;
        }

        StartCoroutine(PlaySequence());
    }

    // ------------------------------------------------------------------ //
    //  工具方法
    // ------------------------------------------------------------------ //

    void ResetAllBools()
    {
        animator.SetBool("isFlying",         false);
        animator.SetBool("isHopping",        false);
        animator.SetBool("isLanding",        false);
        animator.SetBool("isSpeaking",       false);
        animator.SetBool("isWalkingForward", false);
        animator.SetBool("isWalkingRight",   false);
        animator.SetBool("isWalkingLeft",    false);
        animator.SetBool("isWalkingBack",    false);
    }

    // ------------------------------------------------------------------ //
    //  时序主序列
    // ------------------------------------------------------------------ //

    IEnumerator PlaySequence()
    {
        // ── Pre-0：isFlying（测试飞行动画）─────────────────────────────
        Debug.Log("[BirdAnimTest] Pre-0: isFlying=true → " + preFlyDuration + "s");
        ResetAllBools();
        animator.SetBool("isFlying", true);
        yield return new WaitForSeconds(preFlyDuration);

        // ── Pre-1：isHopping（测试 hop forward 动画）────────────────────
        Debug.Log("[BirdAnimTest] Pre-1: isHopping=true (hop forward) → " + preHopDuration + "s");
        ResetAllBools();
        animator.SetBool("isHopping", true);
        yield return new WaitForSeconds(preHopDuration);

        // ── Pre-2：isWalkingForward（测试 walk forward 动画）────────────
        Debug.Log("[BirdAnimTest] Pre-2: isWalkingForward=true → " + preWalkDuration + "s");
        ResetAllBools();
        animator.SetBool("isWalkingForward", true);
        yield return new WaitForSeconds(preWalkDuration);

        // ── Step 0：idle（所有Bool关闭，观察静息动画）────────────────────
        Debug.Log("[BirdAnimTest] Step0: idle → " + idleDuration + "s");
        ResetAllBools();
        yield return new WaitForSeconds(idleDuration);

        // ── Step 1：isHopping（胖胖1 挥翅膀）────────────────────────────
        Debug.Log("[BirdAnimTest] Step1: isHopping → " + hoppingDuration + "s");
        ResetAllBools();
        animator.SetBool("isHopping", true);
        yield return new WaitForSeconds(hoppingDuration);

        // ── Step 2：isFlying（胖胖2 起飞）────────────────────────────────
        Debug.Log("[BirdAnimTest] Step2: isFlying → " + flyingDuration + "s");
        ResetAllBools();
        animator.SetBool("isFlying", true);
        yield return new WaitForSeconds(flyingDuration);

        // ── Step 3：isFlying（胖胖3 飞行中）──────────────────────────────
        Debug.Log("[BirdAnimTest] Step3: isFlying → " + flyingDuration2 + "s");
        yield return new WaitForSeconds(flyingDuration2);

        // ── Step 4：isFlying（胖胖4 飞行准备巡航）────────────────────────
        Debug.Log("[BirdAnimTest] Step4: isFlying → " + flyingDuration3 + "s");
        yield return new WaitForSeconds(flyingDuration3);

        // ── Step 5：巡航平移到 cruiseTarget ──────────────────────────────
        if (cruiseTarget != null)
        {
            Debug.Log("[BirdAnimTest] Step5: cruising to target...");
            yield return StartCoroutine(CruiseToTarget());
        }
        else
        {
            Debug.LogWarning("[BirdAnimTest] Step5: cruiseTarget 未赋值，跳过巡航");
        }

        // ── Step 6：到达后悬停（isFlying 保持）───────────────────────────
        Debug.Log("[BirdAnimTest] Step6: 到达目标，悬停待命");
    }

    // ------------------------------------------------------------------ //
    //  巡航协程（从当前位置线性平移到 cruiseTarget）
    // ------------------------------------------------------------------ //

    IEnumerator CruiseToTarget()
    {
        Vector3 startPos  = transform.position;
        float   totalDist = Vector3.Distance(startPos, cruiseTarget.position);

        if (totalDist < 0.01f)
        {
            Debug.Log("[BirdAnimTest] 已在目标位置，跳过移动");
            yield break;
        }

        animator.SetBool("isFlying", true);

        float traveled = 0f;

        while (traveled < totalDist)
        {
            traveled += cruiseSpeed * Time.deltaTime;
            float t = Mathf.Clamp01(traveled / totalDist);
            transform.position = Vector3.Lerp(startPos, cruiseTarget.position, t);
            yield return null;
        }

        transform.position = cruiseTarget.position;
        Debug.Log("[BirdAnimTest] 到达 cruiseTarget：" + cruiseTarget.position);
    }
}