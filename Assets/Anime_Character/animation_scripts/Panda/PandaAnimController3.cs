using UnityEngine;

/// <summary>
/// 玛雅文字关卡 —— 胖达动画控制器
/// 根据 DialogueSystemMayaGlyph 广播的台词关键词，驱动胖达的 Animator Bool 参数。
///
/// 对应 Panda Animator 中已有的 Bool 参数（截图核对）：
///   isHappy     → 开心 / 恍然大悟 / 好奇感叹
///   isPointing  → 指向碎片 / 讲解操作规则
///   isLooking   → 四处张望 / 搜寻漂浮碎片中的目标
///   isAgreeing  → 点头附和诺亚 / 确认理解
///   isCheering  → 欢呼 / 阶段碎片放入成功
///   isVictory   → 最终合体成功 / 国王幽灵现身后的震惊兴奋
///
/// 依赖：场景中存在名为 "DialogueManager" 的 GameObject，挂载 DialogueSystemMayaGlyph 组件。
/// </summary>
public class PandaAnimController3 : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  私有引用
    // ─────────────────────────────────────────────
    private Animator animator;
    private DialogueSystemMayaGlyph dialogueSystem;

    // ─────────────────────────────────────────────
    //  Unity 生命周期
    // ─────────────────────────────────────────────
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("❌ [PandaAnimController3] 找不到 Animator 组件！");

        GameObject dialogRoot = GameObject.Find("DialogueManager");
        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystemMayaGlyph>();
            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStart;
                dialogueSystem.OnLineEnd += OnLineEnd;
                Debug.Log("✅ [PandaAnimController3] DialogueSystemMayaGlyph 事件注册完成");
            }
            else
            {
                Debug.LogError("❌ [PandaAnimController3] DialogueManager 上没有 DialogueSystemMayaGlyph 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ [PandaAnimController3] 场景中找不到 DialogueManager！");
        }
    }

    // ─────────────────────────────────────────────
    //  对话事件回调
    // ─────────────────────────────────────────────
    void OnLineStart(string speaker, string content)
    {
        if (speaker != "胖达") return;

        Debug.Log($"[PandaAnimController3] ▶ 收到台词 | 说话人：{speaker} | 内容：{content}");

        ResetAllBools();

        // ══════════════════════════════════════════════════════════════════
        //  开场：进入金字塔，看见漂浮文字碎片
        // ══════════════════════════════════════════════════════════════════

        // "诺亚，这绕着我们转圈的'表情包'是什么呀！这回咱们要找什么？"
        if (content.Contains("绕着我们转圈") || content.Contains("表情包") && content.Contains("这回咱们"))
        {
            TriggerAnim("isIntroUpper", "好奇（漂浮文字碎片是什么）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  操作规则讲解
        // ══════════════════════════════════════════════════════════════════

        // "嘿，伙计，听好了！这次的规矩变了。看准目标后，用你的左手射线指着它，按住左手中指扳机键把它抓过来！"
        else if (content.Contains("这次的规矩变了") || content.Contains("左手射线") && content.Contains("扳机键"))
        {
            TriggerAnim("isPointing", "指向（新操作规则说明：射线抓取）");
        }
        // "然后左手摇杆往前推，放进对应的台座上那个发着蓝光的透明方块里。"
        else if (content.Contains("左手摇杆往前推") || content.Contains("发着蓝光的透明方块"))
        {
            TriggerAnim("isPointing", "指向（操作说明：摇杆推入台座）");
        }
        // "放对了，它就会'咔哒'一声吸进去！"
        else if (content.Contains("咔哒") || content.Contains("自动吸进去"))
        {
            TriggerAnim("isHappy", "开心（演示成功吸附效果）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段一：寻找玉米神
        // ══════════════════════════════════════════════════════════════════

        // "所以他们对玉米简直崇拜到了骨子里，对吧？"
        else if (content.Contains("对玉米简直崇拜") || content.Contains("崇拜到了骨子里"))
        {
            TriggerAnim("isAgreeing", "附和（赞同玛雅对玉米的崇拜）");
        }
        // "快看那堆漂浮的字符，里面有一个侧脸特别怪的家伙！"
        else if (content.Contains("漂浮的字符") || content.Contains("侧脸特别怪的家伙"))
        {
            TriggerAnim("isLooking", "张望（在碎片堆里找玉米神侧脸）");
        }
        // "看到了！他的前额上还顶着一个螺旋状的卷曲符号，诺亚说那是'玉米卷须'。"
        else if (content.Contains("螺旋状的卷曲符号") || content.Contains("玉米卷须"))
        {
            TriggerAnim("isHappy", "开心（找到玉米卷须特征）");
        }
        // "先用你的左手射线指着这位玉米神的侧脸，按住左手中指扳机键把他抓过来！"
        else if (content.Contains("玉米神的侧脸") || content.Contains("把他抓过来") && content.Contains("玉米"))
        {
            TriggerAnim("isPointing", "指向（操作引导：抓取玉米神）");
        }
        // "然后左手摇杆往前推，把他稳稳地送进最左侧台座上那个发着蓝光的透明方块里。"
        else if (content.Contains("最左侧台座") || content.Contains("稳稳地送进"))
        {
            TriggerAnim("isPointing", "指向左侧台座（送入玉米神）");
        }
        // "只要位置对上了，它就会自动吸进去的！"
        else if (content.Contains("位置对上了") || content.Contains("会自动吸进去"))
        {
            TriggerAnim("isHappy", "开心（鼓励玩家完成放入）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段二：寻找盾牌
        // ══════════════════════════════════════════════════════════════════

        // "喔！'盾牌'听起来就很厚实！探险家，快在那堆乱飞的符号里找找。"
        else if (content.Contains("盾牌") && content.Contains("很厚实") || content.Contains("乱飞的符号里找找"))
        {
            TriggerAnim("isLooking", "张望（在符号堆里找盾牌）");
        }
        // "我们要找的是一个圆润的、像石盘一样的印记。"
        else if (content.Contains("圆润的") && content.Contains("石盘一样的印记"))
        {
            TriggerAnim("isLooking", "张望（描述盾牌外形特征）");
        }
        // "我也看到了！中间还有个像眼睛一样的图案。"
        else if (content.Contains("我也看到了") || content.Contains("像眼睛一样的图案"))
        {
            TriggerAnim("isHappy", "开心（找到盾牌中央眼形图案）");
        }
        // "快，再次按住你的左手中指，抓住那个'带网格边缘的圆盾'，把它送进中间的蓝色台座里！"
        else if (content.Contains("带网格边缘的圆盾") || content.Contains("中间的蓝色台座"))
        {
            TriggerAnim("isPointing", "指向中间台座（操作引导：抓取盾牌）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  阶段三：寻找美洲豹
        // ══════════════════════════════════════════════════════════════════

        // "嘿，这不就是序幕里追得我们满地找牙的那位嘛！"
        else if (content.Contains("满地找牙") || content.Contains("序幕里追得我们"))
        {
            TriggerAnim("isHappy", "开心（认出序幕的美洲豹）");
        }
        // "认准那个张着大嘴怒吼、圆圆的耳朵上布满黑斑点的豹头碎片。"
        else if (content.Contains("张着大嘴怒吼") || content.Contains("圆圆的耳朵上布满黑斑点"))
        {
            TriggerAnim("isLooking", "张望（描述美洲豹特征，搜寻碎片）");
        }
        // "快，按住左手中指抓住最后这个'斑点豹头'，把它放在最右边的台座上，完成国王的名字！"
        else if (content.Contains("斑点豹头") || content.Contains("最右边的台座"))
        {
            TriggerAnim("isPointing", "指向右侧台座（操作引导：放入美洲豹头）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  结局：国王幽灵现身
        // ══════════════════════════════════════════════════════════════════

        // 国王幽灵现身后，胖达的震惊/兴奋反应
        // （台词见国王幽灵部分后紧接的胖达反应，此处以上下文关键词预留）
        // "哇……他真的出现了！"  /  "漫长的黑夜……"后胖达的任何感叹
        else if (content.Contains("真的出现了") || content.Contains("国王") && content.Contains("真名"))
        {
            TriggerAnim("isVictory", "胜利（国王幽灵现身，任务阶段完成）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】开场：被历法机械墙震撼
        // ══════════════════════════════════════════════════════════════════

        // "老天爷……这墙里居然嵌着这么精密的石头机关。诺亚，这些刻着奇怪符号的轮盘是干什么用的？"
        else if (content.Contains("精密的石头机关") || content.Contains("奇怪符号的轮盘"))
        {
            TriggerAnim("isHappy", "好奇震撼（历法盘是什么）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】2012 好奇提问
        // ══════════════════════════════════════════════════════════════════

        // "等等，诺亚！如果时间是个圆圈，那大家传得沸沸扬扬的'2012世界末日'是怎么回事？"
        else if (content.Contains("2012世界末日") || content.Contains("沸沸扬扬"))
        {
            TriggerAnim("isHappy", "好奇提问（2012末日）");
        }
        // "第五个纪元？你是说，咱们之前已经'报废'过四个世界了？"
        else if (content.Contains("第五个纪元") || content.Contains("报废") && content.Contains("四个世界"))
        {
            TriggerAnim("isHappy", "恍然大悟（四个世界报废了）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】明确解谜目标
        // ══════════════════════════════════════════════════════════════════

        // "明白了！左边找数字 4 和领主图像，右边找数字 8 和粮仓图案！"
        else if (content.Contains("左边找数字") || content.Contains("右边找数字") || content.Contains("粮仓图案"))
        {
            TriggerAnim("isCheering", "欢呼（明白解谜目标）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段一：神历内盘（数字 4）
        // ══════════════════════════════════════════════════════════════════

        // "左边内圈是神历的日数，数字 4，这个简单！"
        else if (content.Contains("左边内圈") || content.Contains("神历的日数"))
        {
            TriggerAnim("isHappy", "开心（数字4很简单！）");
        }
        // "找四个点对吧？探险家，交给你了！"
        else if (content.Contains("找四个点") || content.Contains("交给你了"))
        {
            TriggerAnim("isPointing", "指向（操作引导：找四个点=数字4）");
        }
        // "咔哒一声！左边内圈搞定！"
        else if (content.Contains("左边内圈搞定") || content.Contains("咔哒一声") && content.Contains("左边"))
        {
            TriggerAnim("isCheering", "欢呼（神历内盘数字4对齐！）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段二：神历外盘（Ajaw 领主）
        // ══════════════════════════════════════════════════════════════════

        // "左边外圈要找的是什么？"
        else if (content.Contains("左边外圈要找"))
        {
            TriggerAnim("isLooking", "张望（期待诺亚介绍Ajaw）");
        }
        // "领主头像？这石盘上全是脸啊，怎么认？"
        else if (content.Contains("石盘上全是脸") || content.Contains("怎么认"))
        {
            TriggerAnim("isHappy", "好奇困惑（满盘都是脸怎么找）");
        }
        // "圆形的嘴巴！记住了！探险家，快转动左侧大盘，把那个'圆嘴巴'的领主头像转进白色方框里！"
        else if (content.Contains("圆形的嘴巴") || content.Contains("圆嘴巴") || content.Contains("左侧大盘"))
        {
            TriggerAnim("isPointing", "指向（操作引导：转左侧外盘找Ajaw）");
        }
        // "左边彻底搞定了！"
        else if (content.Contains("左边彻底搞定"))
        {
            TriggerAnim("isCheering", "欢呼（神历外盘Ajaw对齐！）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段三：太阳历内盘（数字 8）
        // ══════════════════════════════════════════════════════════════════

        // "数字 8 就是一条横线和三个点，对吧？探险家，转动右边的小盘子，把它也对准白色方框！"
        else if (content.Contains("一条横线和三个点") || content.Contains("右边的小盘子"))
        {
            TriggerAnim("isPointing", "指向（操作引导：转右侧内盘找数字8）");
        }
        // "右边内圈也对上了！"
        else if (content.Contains("右边内圈也对上"))
        {
            TriggerAnim("isCheering", "欢呼（太阳历内盘数字8对齐！）");
        }

        // ══════════════════════════════════════════════════════════════════
        //  【历法关卡】阶段四：太阳历外盘（Kumk'u 粮仓）
        // ══════════════════════════════════════════════════════════════════

        // "最后一步了！右边外圈我们要找的这个'Kumk'u'，它长什么样？也是个人脸吗？"
        else if (content.Contains("最后一步了") || content.Contains("右边外圈") || content.Contains("也是个人脸吗"))
        {
            TriggerAnim("isLooking", "张望（期待诺亚介绍Kumku）");
        }
        // "容器？那我该怎么在这些图案里认出它？"
        else if (content.Contains("容器？") || content.Contains("怎么在这些图案里认出"))
        {
            TriggerAnim("isHappy", "好奇困惑（容器怎么认）");
        }
        // "明白了！中心是玉米种子，下面是弯曲线条，右上角有小圆点！探险家，快转动最右侧的大盘……"
        else if (content.Contains("中心是玉米种子") || content.Contains("最右侧的大盘"))
        {
            TriggerAnim("isPointing", "指向（操作引导：转右侧外盘找Kumku）");
        }

        // ── 未命中任何关键词 ──────────────────────────────────────────────
        else
        {
            Debug.Log($"[PandaAnimController3] ⚪ 无匹配关键词，保持 Idle | 台词：{content}");
        }
    }

    void OnLineEnd(string speaker)
    {
        if (speaker != "胖达") return;

        Debug.Log($"[PandaAnimController3] ⏹ 台词结束，重置所有动画 Bool → Idle | 说话人：{speaker}");
        ResetAllBools();
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    /// <summary>SetBool 置 true，并打印触发日志。</summary>
    void TriggerAnim(string boolName, string description)
    {
        animator.SetBool(boolName, true);
        Debug.Log($"[PandaAnimController3] 🎬 触发动画 | Bool：{boolName} | 描述：{description}");
    }

    /// <summary>将胖达 Animator 中所有自定义 Bool 重置为 false（与截图 Bool 列表一一对应）。</summary>
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

        Debug.Log("[PandaAnimController3] 🔄 ResetAllBools 完成，所有 Bool → false");
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