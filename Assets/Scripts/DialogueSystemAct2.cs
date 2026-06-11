using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLineAct2
{
    public string speaker;

    [TextArea]
    public string content;

    public AudioClip clip;   // 有音频就拖，没有就留空

    [Header("本句标记")]
    [Tooltip("给这一句起一个唯一名字，例如 teach9、try9_1、hint9、teach20、try20_1、ending。分支跳转时直接填这个名字，不用数 Element 编号。")]
    public string lineId;

    [Header("进入本句前强制更新玛雅绘制阶段")]
    [Tooltip("可选。进入本句前只重置 MayaPainter 阶段，不播放成功动画/音效/特效。数字9尝试句一般选 ResetToInput9。正常对话选 None。")]
    public MayaStageResetActionAct2 beforeLineStageAction = MayaStageResetActionAct2.None;

    [Header("交互等待设置")]
    public DialogueInteractionWaitTypeAct2 waitType = DialogueInteractionWaitTypeAct2.None;

    [Header("玛雅数字显示")]
    [Tooltip("勾选后：播放本句音频结束后，渐变显示拖入的玛雅数字图片。")]
    public bool showMayaImage = false;

    [Tooltip("需要显示的玛雅数字图片对象（UI Image 或 GameObject）")]
    public GameObject mayaImageObject;

    [Tooltip("图片淡入需要多久")]
    public float mayaImageFadeInDuration = 0.5f;

    [Tooltip("图片完整显示多久")]
    public float mayaImageStayDuration = 3f;

    [Tooltip("图片淡出需要多久")]
    public float mayaImageFadeOutDuration = 0.5f;

    [Header("限时分支设置（用于 if / else）")]
    [Tooltip("勾选后：这一句会在等待交互时计时。时间内成功则跳到 Success Target Line Id；超时则跳到 Timeout Target Line Id。")]
    public bool useTimeoutBranch = false;

    [Tooltip("限时时长。比如第一轮 t1、第二轮 t2。只有 useTimeoutBranch 勾选且 waitType 不是 None 时才生效。")]
    public float timeLimit = 8f;

    [Tooltip("交互成功后跳到哪一句。填目标句子的 Line Id，例如 teach20 或 ending。留空表示正常进入下一句。")]
    public string successTargetLineId;

    [Tooltip("超时未完成后跳到哪一句。填目标句子的 Line Id，例如 hint9 或 noahHelp20。留空表示正常进入下一句。")]
    public string timeoutTargetLineId;

    [HideInInspector] public int successNextLineIndex = -1;
    [HideInInspector] public int timeoutNextLineIndex = -1;

    [Header("强制更新玛雅绘制阶段")]
    [Tooltip("用于‘诺亚帮玩家写正确答案’这种分支，会播放对应成功动画/音效/特效。普通成功台词不要选。正常对话选 None。")]
    public MayaStageFeedbackActionAct2 afterLineStageAction = MayaStageFeedbackActionAct2.None;

    [Header("普通播完跳转设置")]
    [Tooltip("可选。普通台词播完后强制跳到指定 Line Id，用于成功分支播完后跳过失败提示等情况。留空表示正常进入下一句。")]
    public string afterLineTargetLineId;

    [Header("视觉提示")]
    [Tooltip("此行对话显示时需要激活的提示物体（如画板旁的手势示意图、正确答案图等）")]
    public GameObject visualHintObject;

    // +++ NEW +++ 动画触发器名称
    [Header("动画触发")]
    public string animTriggerName;   // 例如 "Wave", "Point", "Talk"
    [System.NonSerialized]
    public bool animationTriggered = false;
}

[System.Serializable]
public class SpeakerAvatarConfigAct2
{
    public string speaker;
    public Sprite avatar;
}

public enum DialogueInteractionWaitTypeAct2
{
    None,

    [Tooltip("这句提示后，等待玩家正确输入数字 9")]
    WaitUntilMaya9Completed,

    [Tooltip("已保留兼容旧数据。新文案取消数字 13 交互，后续不要再选这个。")]
    WaitUntilMaya13Completed,

    [Tooltip("这句提示后，等待玩家正确输入数字 20")]
    WaitUntilMaya20Completed
}

public enum MayaStageResetActionAct2
{
    None,

    [Tooltip("只把 MayaPainter.currentStage 重置为 Input9，不播放成功动画/音效/特效。用于数字9第一次或第二次尝试前。")]
    ResetToInput9,

    [Tooltip("只把 MayaPainter.currentStage 重置为 Input20，不播放成功动画/音效/特效。一般很少用。")]
    ResetToInput20,

    [Tooltip("只把 MayaPainter.currentStage 重置为 Completed，不播放成功动画/音效/特效。一般很少用。")]
    ResetToCompleted
}

public enum MayaStageFeedbackActionAct2
{
    None,

    [Tooltip("用于诺亚帮写数字9：播放数字9成功反馈，然后进入 Input20。不要给普通成功台词使用。")]
    HelpCompleteInput9WithFeedback,

    [Tooltip("用于诺亚帮写数字20：播放数字20成功反馈和最终机关流程，然后进入 Completed。不要给普通成功台词使用。")]
    HelpCompleteInput20WithFeedback,

    [Tooltip("只切到 Input20，不播放成功反馈。通常不需要；玩家自己写对9时 MayaPainter 会自动进入 Input20。")]
    SetInput20Only,

    [Tooltip("只切到 Completed，不播放成功反馈。通常不需要；玩家自己写对20时 MayaPainter 会自动进入 Completed。")]
    SetCompletedOnly
}

public class DialogueSystemAct2 : MonoBehaviour
{
    public System.Action OnDialogueStart;
    public System.Action OnDialogueEnd;

    private System.Action<string> _onLineStart;
    public event System.Action<string> OnLineStart
    {
        add { _onLineStart -= value; _onLineStart += value; }
        remove { _onLineStart -= value; }
    }

    private System.Action<string, string> _onLineStartWithContent;
    public event System.Action<string, string> OnLineStartWithContent
    {
        add { _onLineStartWithContent -= value; _onLineStartWithContent += value; }
        remove { _onLineStartWithContent -= value; }
    }

    private System.Action<string> _onLineEnd;
    public event System.Action<string> OnLineEnd
    {
        add { _onLineEnd -= value; _onLineEnd += value; }
        remove { _onLineEnd -= value; }
    }

    [Header("电脑测试")]
    [Tooltip("电脑测试时，是否允许按空格跳过当前句/交互等待")]
    public bool allowSkipInteractionInEditor = true;

    [Header("对话 UI")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("说话者头像")]
    [Tooltip("拖入对话框里用于显示头像的 Image 组件。")]
    public Image speakerAvatarImage;

    [Tooltip("按说话者名字匹配头像。这里的 speaker 必须和 Dialogue Lines 里的 speaker 完全一致。")]
    public List<SpeakerAvatarConfigAct2> speakerAvatars = new List<SpeakerAvatarConfigAct2>();

    [Tooltip("找不到当前说话者头像时，是否隐藏头像 Image。")]
    public bool hideAvatarIfNotFound = true;

    [Header("对话内容")]
    public List<DialogueLineAct2> dialogueLines;

    [Tooltip("有音频时：音频播放完后等待多少秒进入下一句")]
    public float autoPlayDelay = 2.0f;

    [Header("播放驱动设置")]
    [Tooltip("没有音频时：文字直接显示后等待多少秒进入下一句")]
    public float textModeDelay = 3.0f;

    [Header("跳过键设置")]
    public bool useRightGrip = false;

    [Header("第二幕交互检测")]
    [Tooltip("拖入场景中挂有 MayaPainter.cs 的画板物体。不需要修改 MayaPainter.cs。")]
    public MayaPainter mayaPainter;

    [Header("调试")]
    public bool showDebugLog = true;

    // +++ NEW +++ 动画控制
    [Header("动画控制")]
    public Animator targetAnimator;   // 要触发动画的角色 Animator

    private int currentLineIndex = 0;
    private bool isWaitingForNext = false;
    private bool isWaitingForInteraction = false;

    private Coroutine displayCoroutine = null;
    private Coroutine waitCoroutine = null;
    private Coroutine interactionCoroutine = null;
    private Coroutine temporaryLineCoroutine = null;
    private Coroutine mayaImageCoroutine = null;
    private GameObject activeMayaImageObject = null;

    private AudioSource audioSource;
    private InputAction skipAction;

    // 反射读取 MayaPainter 的 private currentStage
    private FieldInfo mayaCurrentStageField;

    // 记录当前激活的视觉提示物体
    private GameObject activeVisualHint = null;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        CacheMayaPainterStageField();
    }

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        skipAction = new InputAction("Skip", binding: "<Keyboard>/space");

        if (useRightGrip)
            skipAction = new InputAction("Skip", binding: "<XRController>{RightHand}/gripPressed");

        skipAction.performed += ctx => OnSkipInput();
        skipAction.Enable();

        StartDialogue();
    }

    void OnDisable()
    {
        skipAction?.Disable();
    }

    void CacheMayaPainterStageField()
    {
        if (mayaPainter == null)
            return;

        mayaCurrentStageField = typeof(MayaPainter).GetField(
            "currentStage",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (showDebugLog)
        {
            if (mayaCurrentStageField != null)
                Debug.Log("DialogueSystemAct2：成功找到 MayaPainter.currentStage。");
            else
                Debug.LogWarning("DialogueSystemAct2：没有找到 MayaPainter.currentStage，请确认 MayaPainter.cs 中字段名没有改。");
        }
    }

    public void StartDialogue()
    {
       
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            if (showDebugLog)
                Debug.LogWarning("DialogueSystemAct2：没有设置任何对话内容。");
            return;
        }

        foreach (var line in dialogueLines) line.animationTriggered = false;

        if (mayaCurrentStageField == null)
            CacheMayaPainterStageField();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        currentLineIndex = 0;
        isWaitingForInteraction = false;

        OnDialogueStart?.Invoke();

        if (showDebugLog)
            Debug.Log("第二幕对话开始。");

        DisplayCurrentLine();
    }

    void UpdateSpeakerAvatar(string speaker)
    {
        if (speakerAvatarImage == null)
            return;

        Sprite targetAvatar = null;

        for (int i = 0; i < speakerAvatars.Count; i++)
        {
            if (speakerAvatars[i] != null && speakerAvatars[i].speaker == speaker)
            {
                targetAvatar = speakerAvatars[i].avatar;
                break;
            }
        }

        if (targetAvatar != null)
        {
            speakerAvatarImage.sprite = targetAvatar;
            speakerAvatarImage.gameObject.SetActive(true);
        }
        else
        {
            if (hideAvatarIfNotFound)
            {
                speakerAvatarImage.gameObject.SetActive(false);
            }
            else
            {
                speakerAvatarImage.sprite = null;
                speakerAvatarImage.gameObject.SetActive(true);
            }
        }
    }

    void DisplayCurrentLine()
    {
        StopCurrentLineCoroutines();

        if (activeVisualHint != null)
        {
            activeVisualHint.SetActive(false);
            activeVisualHint = null;
        }

        isWaitingForNext = false;
        isWaitingForInteraction = false;

        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count)
            return;

        DialogueLineAct2 line = dialogueLines[currentLineIndex];

        ApplyBeforeLineStageAction(line);

        if (line.visualHintObject != null)
        {
            line.visualHintObject.SetActive(true);
            activeVisualHint = line.visualHintObject;
        }

        if (speakerText != null)
            speakerText.text = line.speaker;

        UpdateSpeakerAvatar(line.speaker);

        if (targetAnimator != null && !string.IsNullOrEmpty(line.animTriggerName) && !line.animationTriggered)
        {
            StartCoroutine(DelayedTrigger(line.animTriggerName));
            line.animationTriggered = true;
        }

       

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        _onLineStart?.Invoke(line.speaker);
        _onLineStartWithContent?.Invoke(line.speaker, line.content);

        if (showDebugLog)
        {
            string driveMode = line.clip != null ? "音频驱动" : "文字驱动";
            Debug.Log($"第二幕播放第 {currentLineIndex + 1} 句：{line.speaker} / LineId：{line.lineId} / {driveMode} / 等待类型：{line.waitType} / 限时分支：{line.useTimeoutBranch} / {line.content}");
        }

        if (line.clip != null)
        {
            audioSource.clip = line.clip;
            audioSource.Play();
            displayCoroutine = StartCoroutine(SyncTextWithAudio(line.content));
        }
        else
        {
            if (dialogueText != null)
                dialogueText.text = line.content;

            displayCoroutine = null;
            AfterLineFullyDisplayed();
        }
    }

    IEnumerator DelayedTrigger(string triggerName)
    {
        yield return null; // 等待一帧
        targetAnimator.SetTrigger(triggerName);
        if (showDebugLog) Debug.Log($"延迟触发动画：{triggerName}");
    }

    IEnumerator SyncTextWithAudio(string fullText)
    {
        if (dialogueText != null)
            dialogueText.text = "";

        float startTime = Time.time;
        int totalChars = fullText.Length;
        float clipLength = audioSource.clip.length;

        while (audioSource.isPlaying)
        {
            float elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / clipLength);
            int charsToShow = Mathf.Clamp(Mathf.FloorToInt(progress * totalChars), 0, totalChars);

            if (dialogueText != null)
                dialogueText.text = fullText.Substring(0, charsToShow);

            yield return null;
        }

        if (dialogueText != null)
            dialogueText.text = fullText;

        displayCoroutine = null;

        AfterLineFullyDisplayed();
    }

    void AfterLineFullyDisplayed()
    {
        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count)
            return;

        DialogueLineAct2 line = dialogueLines[currentLineIndex];

        // 1?? 播放玛雅数字图片
        if (line.showMayaImage && line.mayaImageObject != null)
        {
            if (mayaImageCoroutine != null)
            {
                StopCoroutine(mayaImageCoroutine);
                mayaImageCoroutine = null;
            }

            mayaImageCoroutine = StartCoroutine(ShowMayaImageThenContinueWithFeedback(line));
            return;
        }

        // 普通台词
        ContinueAfterCurrentLine(line);
    }
    IEnumerator ShowMayaImageThenContinueWithFeedback(DialogueLineAct2 line)
{
    GameObject imageObj = line.mayaImageObject;
    if (imageObj == null)
    {
        ContinueAfterCurrentLine(line);
        yield break;
    }

    activeMayaImageObject = imageObj;
    CanvasGroup canvasGroup = imageObj.GetComponent<CanvasGroup>();
    if (canvasGroup == null)
        canvasGroup = imageObj.AddComponent<CanvasGroup>();

    imageObj.SetActive(true);
    canvasGroup.alpha = 0f;

    // 淡入
    float elapsed = 0f;
    float fadeInDuration = Mathf.Max(0.01f, line.mayaImageFadeInDuration);
    while (elapsed < fadeInDuration)
    {
        elapsed += Time.deltaTime;
        canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
        yield return null;
    }
    canvasGroup.alpha = 1f;

    // 停留
    yield return new WaitForSeconds(line.mayaImageStayDuration);

    // 图片淡出
    float fadeOutDuration = Mathf.Max(0.01f, line.mayaImageFadeOutDuration);
    elapsed = 0f;
    while (elapsed < fadeOutDuration)
    {
        elapsed += Time.deltaTime;
        canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
        yield return null;
    }
    canvasGroup.alpha = 0f;
    imageObj.SetActive(false);
    activeMayaImageObject = null;
    mayaImageCoroutine = null;

    // 2?? 玛雅数字显示完毕后再触发 afterLineStageAction
    if (line.afterLineStageAction != MayaStageFeedbackActionAct2.None)
    {
        ApplyAfterLineStageAction(line); // 播放音效/动效/成功反馈
    }

    // 3?? 再进入下一句
    ContinueAfterCurrentLine(line);
}
    IEnumerator ShowMayaImageThenContinue(DialogueLineAct2 line)
    {
        GameObject imageObj = line.mayaImageObject;

        if (imageObj == null)
        {
            ContinueAfterCurrentLine(line);
            yield break;
        }

        activeMayaImageObject = imageObj;

        CanvasGroup canvasGroup = imageObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = imageObj.AddComponent<CanvasGroup>();

        imageObj.SetActive(true);
        canvasGroup.alpha = 0f;

        // 淡入
        float fadeInDuration = Mathf.Max(0.01f, line.mayaImageFadeInDuration);
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // 停留
        float stayDuration = Mathf.Max(0f, line.mayaImageStayDuration);
        yield return new WaitForSeconds(stayDuration);

        // 淡出
        float fadeOutDuration = Mathf.Max(0.01f, line.mayaImageFadeOutDuration);
        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        imageObj.SetActive(false);

        if (activeMayaImageObject == imageObj)
            activeMayaImageObject = null;

        mayaImageCoroutine = null;

        // 图片完整显示并消失后，再继续流程
        ContinueAfterCurrentLine(line);
    }

    void ContinueAfterCurrentLine(DialogueLineAct2 line)
    {
        if (line == null)
            return;

        if (line.waitType == DialogueInteractionWaitTypeAct2.None)
        {
            // 普通台词：图片消失后立刻进入下一句
            // 如果你还想额外等一会儿，可以把 0f 改成 autoPlayDelay
            StartWaitForNext(0f);
        }
        else
        {
            // 有交互等待的台词：图片显示完后，再开始等待玩家输入
            StartInteractionWait(line.waitType);
        }
    }
    

    void StartWaitForNext(float delay)
    {
        if (waitCoroutine != null)
            StopCoroutine(waitCoroutine);

        waitCoroutine = StartCoroutine(WaitAndNext(delay));
    }

    IEnumerator WaitAndNext(float delay)
    {
        isWaitingForNext = true;
        yield return new WaitForSeconds(delay);
        isWaitingForNext = false;
        waitCoroutine = null;

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count)
        {
            DialogueLineAct2 currentLine = dialogueLines[currentLineIndex];
            _onLineEnd?.Invoke(currentLine.speaker);
            ApplyAfterLineStageAction(currentLine);

            if (JumpByAfterLineTarget(currentLine))
                yield break;
        }

        NextLine();
    }

    void StartInteractionWait(DialogueInteractionWaitTypeAct2 waitType)
    {
        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        isWaitingForInteraction = true;
        interactionCoroutine = StartCoroutine(WaitForInteraction(waitType));
    }

    IEnumerator WaitForInteraction(DialogueInteractionWaitTypeAct2 waitType)
    {
        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count)
            yield break;

        DialogueLineAct2 line = dialogueLines[currentLineIndex];
        float startTime = Time.time;
        bool timeoutHappened = false;

        if (showDebugLog)
            Debug.Log($"第二幕开始等待交互：{waitType}，当前 MayaPainter 阶段：{GetMayaPainterStageName()}，是否启用限时分支：{line.useTimeoutBranch}，限时：{line.timeLimit} 秒");

        while (true)
        {
            if (IsInteractionCompleted(waitType))
            {
                if (showDebugLog)
                    Debug.Log($"第二幕交互完成：{waitType}。");
                timeoutHappened = false;
                break;
            }

            if (line.useTimeoutBranch && line.timeLimit > 0f && Time.time - startTime >= line.timeLimit)
            {
                if (showDebugLog)
                    Debug.Log($"第二幕交互超时：{waitType}，进入 Timeout 分支。");
                timeoutHappened = true;
                break;
            }

            yield return null;
        }

        if (timeoutHappened && mayaPainter != null)
        {
            // 超时分支时，玩家可能还按着扳机、画笔还没 EndDrawing。
            // 这里立刻取消当前笔画，避免松手时 MayaPainter 把刚才的残留笔画识别成“正确输入9/20”，从而误触发成功动效。
            mayaPainter.CancelCurrentDrawingWithoutFeedback();
        }

        if (activeVisualHint != null)
        {
            activeVisualHint.SetActive(false);
            activeVisualHint = null;
        }

        isWaitingForInteraction = false;
        interactionCoroutine = null;

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count)
        {
            _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);
            ApplyAfterLineStageAction(dialogueLines[currentLineIndex]);
        }

        if (line.useTimeoutBranch)
        {
            bool jumped = JumpByBranchTarget(line, timeoutHappened);
            if (jumped)
                yield break;
        }

        NextLine();
    }

    bool IsInteractionCompleted(DialogueInteractionWaitTypeAct2 waitType)
    {
        string stageName = GetMayaPainterStageName();

        if (string.IsNullOrEmpty(stageName))
            return false;

        if (showDebugLog && Time.frameCount % 120 == 0)
        {
            Debug.Log($"当前 MayaPainter 阶段：{stageName}，等待类型：{waitType}");
        }

        switch (waitType)
        {
            case DialogueInteractionWaitTypeAct2.WaitUntilMaya9Completed:
                return stageName != "Input9";

            case DialogueInteractionWaitTypeAct2.WaitUntilMaya13Completed:
                return stageName != "Input13";

            case DialogueInteractionWaitTypeAct2.WaitUntilMaya20Completed:
                return stageName == "Completed";
        }

        return false;
    }

    string GetMayaPainterStageName()
    {
        if (mayaPainter == null) return "";

        if (mayaCurrentStageField == null)
        {
            CacheMayaPainterStageField();
            if (mayaCurrentStageField == null) return "";
        }

        object stageValue = mayaCurrentStageField.GetValue(mayaPainter);
        if (stageValue == null) return "";

        return stageValue.ToString();
    }

    void ApplyBeforeLineStageAction(DialogueLineAct2 line)
    {
        if (line == null || line.beforeLineStageAction == MayaStageResetActionAct2.None)
            return;

        // 进入一句“等待玩家输入”的台词前，只重置阶段，不播放成功动画。
        ApplyMayaStageResetAction(line.beforeLineStageAction, $"进入本句前：{line.lineId}");
    }

    void ApplyAfterLineStageAction(DialogueLineAct2 line)
    {
        if (line == null || line.afterLineStageAction == MayaStageFeedbackActionAct2.None)
            return;

        // 台词播完后的动作：只有“诺亚帮写”才播放成功反馈。
        ApplyMayaStageFeedbackAction(line.afterLineStageAction, $"本句播完后：{line.lineId}");
    }

    void ApplyMayaStageResetAction(MayaStageResetActionAct2 action, string reason)
    {
        if (mayaPainter != null)
        {
            if (action == MayaStageResetActionAct2.ResetToInput9)
            {
                mayaPainter.ForceStageInput9Only();
                return;
            }
            else if (action == MayaStageResetActionAct2.ResetToInput20)
            {
                mayaPainter.ForceStageInput20Only();
                return;
            }
            else if (action == MayaStageResetActionAct2.ResetToCompleted)
            {
                mayaPainter.ForceStageCompletedOnly();
                return;
            }
        }

        if (action == MayaStageResetActionAct2.ResetToInput9)
            ForceMayaPainterStage("Input9", reason);
        else if (action == MayaStageResetActionAct2.ResetToInput20)
            ForceMayaPainterStage("Input20", reason);
        else if (action == MayaStageResetActionAct2.ResetToCompleted)
            ForceMayaPainterStage("Completed", reason);
    }

    void ApplyMayaStageFeedbackAction(MayaStageFeedbackActionAct2 action, string reason)
    {
        if (mayaPainter != null)
        {
            if (action == MayaStageFeedbackActionAct2.HelpCompleteInput9WithFeedback)
            {
                // 诺亚帮写数字9：播放数字9成功反馈，然后进入 Input20。
                mayaPainter.ForceCompleteInput9WithFeedback();
                return;
            }
            else if (action == MayaStageFeedbackActionAct2.HelpCompleteInput20WithFeedback)
            {
                // 诺亚帮写数字20：播放数字20成功反馈和最终机关流程。
                mayaPainter.ForceCompleteInput20WithFeedback();
                return;
            }
            else if (action == MayaStageFeedbackActionAct2.SetInput20Only)
            {
                mayaPainter.ForceStageInput20Only();
                return;
            }
            else if (action == MayaStageFeedbackActionAct2.SetCompletedOnly)
            {
                mayaPainter.ForceStageCompletedOnly();
                return;
            }
        }

        if (action == MayaStageFeedbackActionAct2.HelpCompleteInput9WithFeedback || action == MayaStageFeedbackActionAct2.SetInput20Only)
            ForceMayaPainterStage("Input20", reason);
        else if (action == MayaStageFeedbackActionAct2.HelpCompleteInput20WithFeedback || action == MayaStageFeedbackActionAct2.SetCompletedOnly)
            ForceMayaPainterStage("Completed", reason);
    }

    void ForceMayaPainterStage(string targetStageName, string reason = "")
    {
        if (mayaPainter == null)
        {
            if (showDebugLog)
                Debug.LogWarning($"无法强制设置 MayaPainter 阶段为 {targetStageName}：mayaPainter 没有拖入。 ");
            return;
        }

        if (mayaCurrentStageField == null)
            CacheMayaPainterStageField();

        if (mayaCurrentStageField == null)
            return;

        try
        {
            Type stageType = mayaCurrentStageField.FieldType;
            object parsedValue = Enum.Parse(stageType, targetStageName);
            mayaCurrentStageField.SetValue(mayaPainter, parsedValue);

            if (showDebugLog)
                Debug.Log($"已强制设置 MayaPainter.currentStage = {targetStageName}。原因：{reason}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"设置 MayaPainter.currentStage = {targetStageName} 失败。请确认 MayaPainter 里的阶段名称是否叫 {targetStageName}。错误：{e.Message}");
        }
    }

    void OnSkipInput()
    {
#if UNITY_EDITOR
        if (!allowSkipInteractionInEditor && !useRightGrip)
            return;
#endif

        if (dialoguePanel == null || !dialoguePanel.activeSelf)
            return;

        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count)
            return;

        DialogueLineAct2 currentLine = dialogueLines[currentLineIndex];

        if (isWaitingForInteraction)
        {
#if UNITY_EDITOR
            if (allowSkipInteractionInEditor)
            {
                if (interactionCoroutine != null)
                {
                    StopCoroutine(interactionCoroutine);
                    interactionCoroutine = null;
                }

                isWaitingForInteraction = false;

                if (activeVisualHint != null)
                {
                    activeVisualHint.SetActive(false);
                    activeVisualHint = null;
                }

                _onLineEnd?.Invoke(currentLine.speaker);
                ApplyAfterLineStageAction(currentLine);

                if (currentLine.useTimeoutBranch && JumpByBranchTarget(currentLine, false))
                    return;

                NextLine();
                return;
            }
#endif
            return;
        }

        if (displayCoroutine != null)
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            StopCoroutine(displayCoroutine);
            displayCoroutine = null;

            if (dialogueText != null)
                dialogueText.text = currentLine.content;

            if (waitCoroutine != null)
                StopCoroutine(waitCoroutine);

            waitCoroutine = null;
            AfterLineFullyDisplayed();
        }
        else if (isWaitingForNext)
        {
            if (waitCoroutine != null)
                StopCoroutine(waitCoroutine);

            waitCoroutine = null;
            isWaitingForNext = false;
            _onLineEnd?.Invoke(currentLine.speaker);
            ApplyAfterLineStageAction(currentLine);

            if (JumpByAfterLineTarget(currentLine))
                return;

            NextLine();
        }
    }


    bool JumpByAfterLineTarget(DialogueLineAct2 line)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.afterLineTargetLineId))
            return false;

        int targetIndex = FindLineIndexById(line.afterLineTargetLineId);
        if (IsValidLineIndex(targetIndex))
        {
            if (showDebugLog)
                Debug.Log($"第二幕普通播完跳转：Line Id {line.lineId} -> {line.afterLineTargetLineId}，也就是 Element {targetIndex}。");

            JumpToLine(targetIndex);
            return true;
        }

        Debug.LogWarning($"第二幕普通播完跳转失败：找不到 Line Id = {line.afterLineTargetLineId}。请检查目标句子的 Line Id 是否完全一致。将改为进入下一句。");
        return false;
    }

    bool JumpByBranchTarget(DialogueLineAct2 line, bool timeoutHappened)
    {
        string targetLineId = timeoutHappened ? line.timeoutTargetLineId : line.successTargetLineId;
        int fallbackIndex = timeoutHappened ? line.timeoutNextLineIndex : line.successNextLineIndex;
        string branchName = timeoutHappened ? "Timeout" : "Success";

        if (!string.IsNullOrWhiteSpace(targetLineId))
        {
            int targetIndex = FindLineIndexById(targetLineId);
            if (IsValidLineIndex(targetIndex))
            {
                if (showDebugLog)
                    Debug.Log($"第二幕 {branchName} 分支跳转到 Line Id：{targetLineId}，也就是 Element {targetIndex}。");

                JumpToLine(targetIndex);
                return true;
            }

            Debug.LogWarning($"第二幕跳转失败：找不到 Line Id = {targetLineId}。请检查目标句子的 Line Id 是否完全一致。将改为进入下一句。");
            return false;
        }

        // 兼容旧版本：如果以前已经填过 Element 编号，这里仍然可用。
        if (IsValidLineIndex(fallbackIndex))
        {
            if (showDebugLog)
                Debug.Log($"第二幕 {branchName} 分支跳转到旧版 Element {fallbackIndex}。");

            JumpToLine(fallbackIndex);
            return true;
        }

        return false;
    }

    int FindLineIndexById(string lineId)
    {
        if (dialogueLines == null || string.IsNullOrWhiteSpace(lineId))
            return -1;

        for (int i = 0; i < dialogueLines.Count; i++)
        {
            if (dialogueLines[i] != null &&
                !string.IsNullOrWhiteSpace(dialogueLines[i].lineId) &&
                dialogueLines[i].lineId.Trim() == lineId.Trim())
                return i;
        }

        return -1;
    }

    [ContextMenu("自动给空的 Line Id 编号")]
    public void AutoFillEmptyLineIds()
    {
        if (dialogueLines == null)
            return;

        for (int i = 0; i < dialogueLines.Count; i++)
        {
            if (dialogueLines[i] != null && string.IsNullOrWhiteSpace(dialogueLines[i].lineId))
                dialogueLines[i].lineId = $"line_{i + 1}";
        }
    }

    bool IsValidLineIndex(int index)
    {
        return dialogueLines != null && index >= 0 && index < dialogueLines.Count;
    }

    void JumpToLine(int index)
    {
        if (!IsValidLineIndex(index))
        {
            if (showDebugLog)
                Debug.LogWarning($"跳转失败：Element {index} 不存在。将进入下一句。");
            NextLine();
            return;
        }

        currentLineIndex = index;
        DisplayCurrentLine();
    }

    void NextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Count) DisplayCurrentLine();
        else EndDialogue();
    }

    void EndDialogue()
    {
        StopCurrentLineCoroutines();

        if (activeVisualHint != null)
        {
            activeVisualHint.SetActive(false);
            activeVisualHint = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (speakerAvatarImage != null)
            speakerAvatarImage.gameObject.SetActive(false);

        OnDialogueEnd?.Invoke();
        if (showDebugLog) Debug.Log("第二幕对话结束。");
    }

    void StopCurrentLineCoroutines()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }

        if (temporaryLineCoroutine != null)
        {
            StopCoroutine(temporaryLineCoroutine);
            temporaryLineCoroutine = null;
        }

        if (mayaImageCoroutine != null)
        {
            StopCoroutine(mayaImageCoroutine);
            mayaImageCoroutine = null;
        }

        if (activeMayaImageObject != null)
        {
            CanvasGroup cg = activeMayaImageObject.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 0f;

            activeMayaImageObject.SetActive(false);
            activeMayaImageObject = null;
        }
    }
    public void ShowTemporaryLine(string speaker, string content, float duration = 3f)
    {
        if (temporaryLineCoroutine != null) StopCoroutine(temporaryLineCoroutine);
        temporaryLineCoroutine = StartCoroutine(ShowTemporaryLineRoutine(speaker, content, duration));
    }

    IEnumerator ShowTemporaryLineRoutine(string speaker, string content, float duration)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        string originalSpeaker = (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count) ? dialogueLines[currentLineIndex].speaker : "";
        string originalText = (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count) ? dialogueLines[currentLineIndex].content : "";

        if (speakerText != null) speakerText.text = speaker;
        UpdateSpeakerAvatar(speaker);
        if (dialogueText != null) dialogueText.text = content;

        yield return new WaitForSeconds(duration);

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count)
        {
            if (speakerText != null) speakerText.text = originalSpeaker;
            UpdateSpeakerAvatar(originalSpeaker);
            if (dialogueText != null) dialogueText.text = originalText;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
        temporaryLineCoroutine = null;
    }
}