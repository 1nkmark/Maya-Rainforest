using UnityEngine;

/// <summary>
/// 玛雅文字关卡 —— 诺亚博士动画控制器
/// 根据 DialogueSystemMayaGlyph 广播的台词关键词，驱动诺亚博士的 Animator Bool 参数。
///
/// 对应 Noah Animator 中已有的 Bool 参数（截图核对）：
///   isIntroducing   → 指向场景 / 介绍文化背景（主讲姿态）
///   isPointing      → 指向具体碎片 / 提示玩家操作目标
///   isNodding       → 点头肯定 / 阶段操作正确反馈
///   isAcknowledging → 轻声应答 / 过渡性补充说明
///   isDisappointed  → 纠错提示（预留，暂未使用）
///   isIntroNumber   → 介绍数字/符号细节特写（复用自数字关键词环境）
///   isIntroUpper    → 指向上方区域（文字合体时向上叠放的引导）
///   isLowerupper    → 同时指向上下两个区域（讲解层级结构）
///
/// 依赖：场景中存在名为 "DialogueManager" 的 GameObject，挂载 DialogueSystemMayaGlyph 组件。
/// </summary>
public class NoahAnimController3 : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  私有引用
    // ─────────────────────────────────────────────
    private Animator animator;
    private DialogueSystemMayaGlyph dialogueSystem;
    private string currentLineContent;

    // ─────────────────────────────────────────────
    //  Unity 生命周期
    // ─────────────────────────────────────────────
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("❌ [NoahAnimController3] 找不到 Animator 组件！");

        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystemMayaGlyph>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd += OnLineEnd;
                Debug.Log("✅ [NoahAnimController3] DialogueSystemMayaGlyph 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [NoahAnimController3] DialogueManager 上没有 DialogueSystemMayaGlyph 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [NoahAnimController3] 场景中找不到 DialogueManager！");
        }
    }

    // ─────────────────────────────────────────────
    //  对话事件回调
    // ─────────────────────────────────────────────
    void OnLineStart(string speaker, string content)
    {
        if (speaker != "诺亚") return;

        currentLineContent = content;
        Debug.Log($"[NoahAnimController3] ▶ 收到台词 | 说话人：{speaker} | 内容：{content}");

        ResetAllBools();

        // ══════════════════════════════════════════════════════════════════
        //  开场介绍：金字塔与国王背景
        // ══════════════════════════════════════════════════════════════════

        // "我们已经进入了这金字塔，这座金字塔的主人是基尼奇·坎·巴赫拉姆二世。"
        if (content.Contains("金字塔的主人") || content.Contains("基尼奇·坎·巴赫拉姆二世"))
        {
            TriggerAnim("isIntroducing", "介绍（金字塔主人）");
        }
        // "他是一位伟大的建筑家，也是'十字架群'神庙的建造者。"
        else if (content.Contains("伟大的建筑家") || content.Contains("十字架群"))
        {
            TriggerAnim("isIntroducing", "介绍（国王建筑成就）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  任务说明：三大支柱
        // ══════════════════════════════════════════════════════════════════

        // "要获得国王的信任，我们必须集齐他统治的三大支柱：【生存的馈赠】、【家族的荣耀】，以及他那受命于天的【神圣真名】。"
        else if (content.Contains("三大支柱") || content.Contains("生存的馈赠") || content.Contains("神圣真名"))
        {
            TriggerAnim("isIntroducing", "介绍（三大支柱任务说明）");
        }
        // "探险家，注意观察这些文字的细节，玛雅人的智慧全在这些线条里。"
        else if (content.Contains("注意观察这些文字") || content.Contains("智慧全在这些线条里"))
        {
            TriggerAnim("isPointing", "指向（提示观察文字细节）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段一：玉米神（生存的馈赠）
        // ══════════════════════════════════════════════════════════════════

        // "探险家，注意看。我们要寻找的第一块碎片，是玛雅文明的命脉——玉米。"
        else if (content.Contains("第一块碎片") || content.Contains("玛雅文明的命脉") && content.Contains("玉米"))
        {
            TriggerAnim("isPointing", "指向（第一块碎片：玉米）");
        }
        // "在玛雅圣书《波波尔-乌》中记载，神灵在尝试了泥土和木头都失败后，最终用玉米捏出了人类的肉身。我们，即是'玉米之子'。"
        else if (content.Contains("波波尔") || content.Contains("玉米之子") || content.Contains("泥土和木头都失败"))
        {
            TriggerAnim("isIntroducing", "介绍（玉米与人类起源神话）");
        }
        // "那是 Waxak，年轻的玉米神。"
        else if (content.Contains("Waxak") || content.Contains("年轻的玉米神"))
        {
            TriggerAnim("isPointing", "指向（锁定Waxak玉米神）");
        }
        // "注意看他的轮廓：他拥有玛雅贵族最引以为傲的审美——如玉米苞片般向后大幅倾斜的额头。"
        else if (content.Contains("玉米苞片") || content.Contains("向后大幅倾斜的额头"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（玉米神额头特征）");
        }
        // "对他而言，这种常人眼中的奇特相貌，正是雨林里最神圣、最清秀的丰收神迹。"
        else if (content.Contains("最神圣、最清秀的丰收神迹") || content.Contains("奇特相貌"))
        {
            TriggerAnim("isAcknowledging", "应答（玉米神外貌文化含义）");
        }
        // "做得好。生存的根基已定，接下来我们需要找回这个家族的荣耀。"
        else if (content.Contains("生存的根基已定") || content.Contains("找回这个家族的荣耀"))
        {
            TriggerAnim("isNodding", "点头（玉米神放入成功）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段二：盾牌（家族的荣耀）
        // ══════════════════════════════════════════════════════════════════

        // "这扇门的主人——基尼奇·坎·巴赫拉姆二世，他的权力源自他伟大的父亲：帕卡尔大帝。"
        else if (content.Contains("帕卡尔大帝") || content.Contains("权力源自他伟大的父亲"))
        {
            TriggerAnim("isIntroducing", "介绍（帕卡尔大帝与盾牌渊源）");
        }
        // "在玛雅语中，PAKAL 就是'盾牌'的意思。盾牌不仅守护着战士的肉体，更守护着王室的血脉不被黑暗吞噬。"
        else if (content.Contains("PAKAL") || content.Contains("守护着战士的肉体") || content.Contains("王室的血脉"))
        {
            TriggerAnim("isIntroducing", "介绍（PAKAL=盾牌的文化含义）");
        }
        // "注意看细节！这个'盾牌'的边缘刻满了细密的网格纹路，那是玛雅人用植物纤维编织护盾的缩影。"
        else if (content.Contains("网格纹路") || content.Contains("植物纤维编织护盾"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（盾牌网格纹路特征）");
        }
        // "在盾牌的四周，还有四个像耳朵一样的小圆环，那是用来固定握带的支点。找到它，那是帕伦克不灭的尊严。"
        else if (content.Contains("像耳朵一样的小圆环") || content.Contains("帕伦克不灭的尊严"))
        {
            TriggerAnim("isPointing", "指向（盾牌四个圆环定位特征）");
        }
        // "帕伦克的尊严归位了。探险家，这是最后一步，也是这扇门的核心——国王的真名。"
        else if (content.Contains("帕伦克的尊严归位") || content.Contains("这扇门的核心"))
        {
            TriggerAnim("isNodding", "点头（盾牌放入成功，引出真名）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段三：国王真名（三部件叠合）
        // ══════════════════════════════════════════════════════════════════

        // "他的名号是 K'inich Kan Bahlam。在玛雅语中，这代表他是'耀眼的蛇豹'。我们必须重新激活这枚王者之印。"
        else if (content.Contains("K'inich Kan Bahlam") || content.Contains("耀眼的蛇豹") || content.Contains("王者之印"))
        {
            TriggerAnim("isIntroducing", "介绍（国王真名K'inich Kan Bahlam）");
        }
        // "最后一步，我们要唤醒这位国王的兽性之魂——美洲豹。"
        else if (content.Contains("兽性之魂") || content.Contains("唤醒这位国王") && content.Contains("美洲豹"))
        {
            TriggerAnim("isPointing", "指向（第三碎片：美洲豹）");
        }
        // "美洲豹不仅是森林的君主，更是夜晚与地底力量的化身。"
        else if (content.Contains("森林的君主") || content.Contains("夜晚与地底力量的化身"))
        {
            TriggerAnim("isIntroducing", "介绍（美洲豹的神话地位）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  结尾：国王显现后的虔诚回应
        // ══════════════════════════════════════════════════════════════════

        // "伟大的君主。我们跨越千年而来，只为探寻文明衰落的真相。"
        else if (content.Contains("跨越千年而来") || content.Contains("文明衰落的真相"))
        {
            TriggerAnim("isAcknowledging", "应答（向国王幽灵致敬）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】开场：历法盘登场
        // ══════════════════════════════════════════════════════════════════

        // "这不是普通的机关，胖达，这是玛雅人用来记录'宇宙呼吸'的历法盘！"
        else if (content.Contains("普通的机关") || content.Contains("历法盘") || content.Contains("宇宙呼吸"))
        {
            TriggerAnim("isIntroducing", "介绍（历法盘登场）");
        }
        // "探险家，玛雅人从未把时间看成一条有去无回的直线。"
        // "在他们眼里，时间就像这墙上的盘子一样，是周而复始的齿轮。"
        else if (content.Contains("有去无回的直线") || content.Contains("周而复始的齿轮") || content.Contains("时间就像这墙上的盘子"))
        {
            TriggerAnim("isIntroducing", "介绍（玛雅时间观：循环齿轮）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】神历（Tzolk'in）介绍
        // ══════════════════════════════════════════════════════════════════

        // "左边这组是【神历】。周期只有 260 天。由 13 个神圣数字和 20 个日符组成。"
        else if (content.Contains("神历") || content.Contains("260 天") || content.Contains("13 个神圣数字"))
        {
            TriggerAnim("isIntroducing", "介绍（神历：260天，左侧盘）");
        }
        // "它不看太阳升降，只负责祭祀、预言和生命降临的宿命。"
        else if (content.Contains("祭祀、预言") || content.Contains("生命降临的宿命"))
        {
            TriggerAnim("isAcknowledging", "应答（神历的文化功能）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】太阳历（Haab'）介绍
        // ══════════════════════════════════════════════════════════════════

        // "而右边，是【太阳历】，是标准的 365 天。有18 个月，每个月20天……"
        else if (content.Contains("太阳历") || content.Contains("365 天") || content.Contains("禁忌之日"))
        {
            TriggerAnim("isIntroducing", "介绍（太阳历：365天，右侧盘）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】2012 误解 & 长纪历
        // ══════════════════════════════════════════════════════════════════

        // "那是人们对玛雅文明的误解"
        else if (content.Contains("对玛雅文明的误解") || content.Contains("人们对玛雅"))
        {
            TriggerAnim("isAcknowledging", "应答（破除2012末日误解）");
        }
        // "除了左右这两套'轮回表'，玛雅还有一套宏大的'里程表'——【长纪历】"
        else if (content.Contains("轮回表") || content.Contains("里程表") || content.Contains("长纪历"))
        {
            TriggerAnim("isIntroducing", "介绍（引出长纪历）");
        }
        // "它记录着从创世那一秒起流逝的每一天，最大单位叫'伯克盾（Baktun）'，一跳就是 394 年。"
        else if (content.Contains("创世那一秒") || content.Contains("伯克盾") || content.Contains("Baktun") || content.Contains("394 年"))
        {
            TriggerAnim("isIntroducing", "介绍（Baktun单位讲解）");
        }
        // "就像汽车的里程表跑到了 99999 公里，'咔哒'一声，它归零重置了！"
        else if (content.Contains("99999") || content.Contains("归零重置"))
        {
            TriggerAnim("isAcknowledging", "应答（里程表归零比喻）");
        }
        // "那不是毁灭，而是第五个纪元的黎明。"
        else if (content.Contains("第五个纪元的黎明") || content.Contains("不是毁灭"))
        {
            TriggerAnim("isNodding", "点头（第五纪元黎明）");
        }
        // "玛雅圣书记载，神灵曾尝试创造过四次人类。"
        // "第一纪元用泥土造人……第二纪元用木头造人……"
        // "前四个纪元的人类，皆因傲慢而消失。"
        else if (content.Contains("四次人类") || content.Contains("第一纪元") || content.Contains("第二纪元") || content.Contains("前四个纪元"))
        {
            TriggerAnim("isIntroducing", "介绍（四个纪元创世失败史）");
        }
        // "让我们从盲目征服自然的第四纪元，跨越到学会共存与觉醒的第五纪元。"
        else if (content.Contains("第四纪元") || content.Contains("共存与觉醒") || content.Contains("最后一次尝试"))
        {
            TriggerAnim("isNodding", "点头（第五纪元跨越的主旨）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】解谜总目标
        // ══════════════════════════════════════════════════════════════════

        // "没错！探险家，我们要把这四面圆盘对齐到那个神圣的坐标：神历的【4 Ajaw（领主）】遇上太阳历的【8 Kumk'u（丰收）】！"
        else if (content.Contains("4 Ajaw") || content.Contains("8 Kumk") || content.Contains("神圣的坐标"))
        {
            TriggerAnim("isPointing", "指向（喊出创世坐标4Ajaw+8Kumku）");
        }
        // "这就是长纪历归零的瞬间，是羽蛇神降临的唯一窗口！"
        else if (content.Contains("长纪历归零") || content.Contains("羽蛇神降临的唯一窗口"))
        {
            TriggerAnim("isIntroducing", "介绍（羽蛇神降临窗口）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段一：神历内盘（数字 4）操作引导
        // ══════════════════════════════════════════════════════════════════

        // "看到墙上那个白色的方框了吗？…用你的右手射线指着左侧的内盘。按住右手中指扳机键，像转方向盘一样转动它。"
        else if (content.Contains("白色的方框") || content.Contains("右手射线") || content.Contains("右手中指扳机键") || content.Contains("转方向盘"))
        {
            TriggerAnim("isPointing", "指向（操作引导：右手射线转左侧内盘）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段二：神历外盘（Ajaw）操作引导
        // ══════════════════════════════════════════════════════════════════

        // "是神历的日名 Ajaw。在玛雅语中，它是'领主'或'太阳神'的意思，代表着至高无上的权力。"
        else if (content.Contains("Ajaw") || content.Contains("领主") && content.Contains("太阳神") || content.Contains("至高无上的权力"))
        {
            TriggerAnim("isIntroducing", "介绍（Ajaw=领主/太阳神含义）");
        }
        // "去转动那个最大的外盘，寻找一张领主的正面头像。"
        else if (content.Contains("最大的外盘") || content.Contains("领主的正面头像"))
        {
            TriggerAnim("isPointing", "指向（操作引导：转左侧外盘找Ajaw）");
        }
        // "看细节！这张脸非常宁静神圣，它有着清晰的双眼和高挺的鼻子。"
        else if (content.Contains("宁静神圣") || content.Contains("清晰的双眼") || content.Contains("高挺的鼻子"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（Ajaw面孔特征：眼鼻）");
        }
        // "最关键的是——它有一个完美的圆形嘴巴。"
        else if (content.Contains("圆形嘴巴") || content.Contains("完美的圆形"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（Ajaw特征：圆形嘴巴）");
        }
        // "这象征着领主在向天地发号施令，或呼出神圣的气息。"
        else if (content.Contains("发号施令") || content.Contains("呼出神圣的气息"))
        {
            TriggerAnim("isAcknowledging", "应答（圆嘴巴的文化含义）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段三：太阳历内盘（数字 8）操作引导
        // ══════════════════════════════════════════════════════════════════

        // "干得好。现在去右边，先在内盘输入太阳历的数字 8。"
        else if (content.Contains("现在去右边") || content.Contains("太阳历的数字 8") || content.Contains("太阳历的数字8"))
        {
            TriggerAnim("isNodding", "点头（肯定阶段二，引导阶段三）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段四：太阳历外盘（Kumk'u）操作引导
        // ══════════════════════════════════════════════════════════════════

        // "不，Kumk'u 代表着'大地的粮仓'。"
        else if (content.Contains("Kumk") || content.Contains("大地的粮仓"))
        {
            TriggerAnim("isAcknowledging", "应答（Kumku不是人脸）");
        }
        // "在玛雅历法中，它象征着丰收的种子被妥善保存在安全的躯壳里，等待下一个纪元的萌发。"
        else if (content.Contains("丰收的种子") || content.Contains("安全的躯壳") || content.Contains("下一个纪元的萌发"))
        {
            TriggerAnim("isIntroducing", "介绍（Kumku的文化象征）");
        }
        // "所以它不是一张脸，而是一个容器符号。"
        else if (content.Contains("不是一张脸") || content.Contains("容器符号"))
        {
            TriggerAnim("isAcknowledging", "应答（Kumku是容器符号）");
        }
        // "仔细观察：它的中心是一个圆润的椭圆，就像一颗饱满的玉米种子。"
        else if (content.Contains("圆润的椭圆") || content.Contains("饱满的玉米种子"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（Kumku中心椭圆特征）");
        }
        // "在种子的下方，包裹着几道平行的弯曲线条，看起来就像海螺的纹路。"
        else if (content.Contains("平行的弯曲线条") || content.Contains("海螺的纹路"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（Kumku弯曲线条特征）");
        }
        // "最特别的是，在图案的右上角，还散落着几个小圆点，像是溢出的谷物。"
        else if (content.Contains("右上角") && content.Contains("小圆点") || content.Contains("溢出的谷物"))
        {
            TriggerAnim("isIntroNumber", "细节讲解（Kumku右上角圆点特征）");
        }

        // ── 未命中任何关键词 ──────────────────────────────────────────────
        else
        {
            Debug.Log($"[NoahAnimController3] ⚪ 无匹配关键词，保持 Idle | 台词：{content}");
        }
    }

    void OnLineEnd(string speaker)
    {
        if (speaker != "诺亚") return;

        Debug.Log($"[NoahAnimController3] ⏹ 台词结束，重置所有动画 Bool → Idle | 说话人：{speaker}");
        ResetAllBools();
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    /// <summary>SetBool 置 true，并打印触发日志。</summary>
    void TriggerAnim(string boolName, string description)
    {
        animator.SetBool(boolName, true);
        string animName = GetCurrentAnimationClipName();
        Debug.Log($"[NoahAnimController3] 🎬 触发动画 | Bool：{boolName} | 描述：{description} | 当前台词：{currentLineContent} | 当前动画：{animName}");
    }

    /// <summary>获取当前 Animator 基础层正在播放的动画剪辑名称。</summary>
    string GetCurrentAnimationClipName()
    {
        if (animator == null) return "None";
        var clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos == null || clipInfos.Length == 0) return "Unknown";
        return clipInfos[0].clip != null ? clipInfos[0].clip.name : "Unnamed";
    }

    /// <summary>
    /// 在指定秒数后切换到另一个 Animator Bool，可选持续时长后自动复位。
    /// 用于同一句台词内分前后两段动作（如先指向后点头）。
    /// </summary>
    System.Collections.IEnumerator SwitchAnimAfterSeconds(
        string fromBool, string toBool, float switchAfterSeconds, string toDescription, float keepToBoolSeconds = 0f)
    {
        yield return new WaitForSeconds(switchAfterSeconds);
        if (animator == null) yield break;

        animator.SetBool(fromBool, false);
        animator.SetBool(toBool, true);
        Debug.Log($"[NoahAnimController3] 🎬 动画切换 | {fromBool} → {toBool} | 描述：{toDescription}");

        if (keepToBoolSeconds > 0f)
        {
            yield return new WaitForSeconds(keepToBoolSeconds);
            if (animator != null)
            {
                animator.SetBool(toBool, false);
                Debug.Log($"[NoahAnimController3] ⏱️ {toBool} 已在 {keepToBoolSeconds} 秒后重置为 false");
            }
        }
    }

    /// <summary>将诺亚博士 Animator 中所有自定义 Bool 重置为 false（与截图 Bool 列表一一对应）。</summary>
    void ResetAllBools()
    {
        if (animator == null) return;

        animator.SetBool("isWalking", false);
        animator.SetBool("isGreeting", false);
        animator.SetBool("isDisappointed", false);
        animator.SetBool("isPointing", false);
        animator.SetBool("isNodding", false);
        animator.SetBool("isAcknowledging", false);
        animator.SetBool("isIntroducing", false);
        animator.SetBool("isIntroNumber", false);
        animator.SetBool("isIntroUpper", false);
        animator.SetBool("isLowerupper", false);

        Debug.Log("[NoahAnimController3] 🔄 ResetAllBools 完成，所有 Bool → false");
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