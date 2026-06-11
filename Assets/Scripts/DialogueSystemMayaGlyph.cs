using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLineMayaGlyph
{
    public string speaker;

    [TextArea]
    public string content;

    public AudioClip clip;   // 有音频就拖，没有就留空

    [Header("本句标记")]
    [Tooltip("给这一句起一个唯一名字，例如 cornTry、shieldTry、leftInnerTry、ending。分支跳转时直接填这个名字，不用数 Element 编号。")]
    public string lineId;

    [Header("交互等待设置")]
    public MayaGlyphWaitType waitType = MayaGlyphWaitType.None;

    [Header("玛雅文字显示")]
    [Tooltip("勾选后：播放本句音频结束后，渐变显示拖入的图片。")]
    public bool showMayaImage = false;

    [Tooltip("需要显示的图片对象（UI Image 或 GameObject）")]
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

    [Tooltip("限时时长。只有 useTimeoutBranch 勾选且 waitType 不是 None 时才生效。")]
    public float timeLimit = 8f;

    [Tooltip("交互成功后跳到哪一句。填目标句子的 Line Id。留空表示正常进入下一句。")]
    public string successTargetLineId;

    [Tooltip("超时未完成后跳到哪一句。填目标句子的 Line Id。留空表示正常进入下一句。")]
    public string timeoutTargetLineId;

    [HideInInspector] public int successNextLineIndex = -1;
    [HideInInspector] public int timeoutNextLineIndex = -1;

    [Header("普通播完跳转设置")]
    [Tooltip("可选。普通台词播完后强制跳到指定 Line Id，用于成功分支播完后跳过失败提示等情况。留空表示正常进入下一句。")]
    public string afterLineTargetLineId;

    [Header("方块兜底通关")]
    [Tooltip("勾选后：本句普通播完时，强制触发第一关方块通关转场。建议只勾选最后一个兜底提示，例如诺亚13。")]
    public bool forceCompleteBallPuzzleAfterLine = false;

    [Header("转盘通关")]
    [Tooltip("勾选后：如果本句的交互等待成功，会立刻强制触发第二关转盘通关转场。建议勾选最后一个等待右内盘的句子。")]
    public bool forceCompleteCalendarPuzzleOnInteractionSuccess = false;

    [Tooltip("勾选后：本句普通播完时，强制触发第二关转盘通关转场。建议勾选最后一个转盘兜底提示，例如最后一次胖胖提示。")]
    public bool forceCompleteCalendarPuzzleAfterLine = false;

    [Header("视觉提示")]
    [Tooltip("此行对话显示时需要激活的提示物体（如转盘上方的箭头、手柄示意图等）")]
    public GameObject visualHintObject;
}

public enum MayaGlyphWaitType
{
    None,

    [Tooltip("这句提示后，等待玩家把玉米神碎片正确放入左侧台座")]
    WaitUntilCornPlaced,

    [Tooltip("这句提示后，等待玩家把盾牌碎片正确放入中间台座")]
    WaitUntilShieldPlaced,

    [Tooltip("这句提示后，等待玩家把斑点豹头碎片正确放入右侧台座")]
    WaitUntilJaguarPlaced,

    [Tooltip("这句提示后，等待玩家把左侧内盘转到目标角度")]
    WaitUntilLeftInnerDialCorrect,

    [Tooltip("这句提示后，等待玩家把左侧外盘转到目标角度")]
    WaitUntilLeftOuterDialCorrect,

    [Tooltip("这句提示后，等待玩家把右侧内盘转到目标角度")]
    WaitUntilRightInnerDialCorrect,

    [Tooltip("这句提示后，等待玩家把右侧外盘转到目标角度")]
    WaitUntilRightOuterDialCorrect
}
[System.Serializable]
public class SpeakerAvatarConfigMayaGlyph
{
    public string speaker;
    public Sprite avatar;
}
public class DialogueSystemMayaGlyph : MonoBehaviour
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
    public List<SpeakerAvatarConfigMayaGlyph> speakerAvatars = new List<SpeakerAvatarConfigMayaGlyph>();

    [Tooltip("找不到当前说话者头像时，是否隐藏头像 Image。")]
    public bool hideAvatarIfNotFound = true;

    [Header("对话内容")]
    public List<DialogueLineMayaGlyph> dialogueLines;

    [Tooltip("有音频时：音频播放完后等待多少秒进入下一句")]
    public float autoPlayDelay = 2.0f;

    [Header("播放驱动设置")]
    [Tooltip("没有音频时：文字直接显示后等待多少秒进入下一句")]
    public float textModeDelay = 3.0f;

    [Header("跳过键设置")]
    public bool useRightGrip = false;

    [Header("玛雅文字交互检测")]
    [Tooltip("拖入场景里的 PuzzleSequenceManager。脚本会读取它的 platformSockets。")]
    public PuzzleSequenceManager puzzleSequenceManager;

    [Tooltip("是否优先使用 PuzzleSequenceManager 里的 platformSockets")]
    public bool useSocketsFromPuzzleSequenceManager = true;

    [Tooltip("玉米神：左侧台座 Socket。若不手动拖，则默认读取 platformSockets[0]")]
    public XRSocketInteractor cornSocket;

    [Tooltip("盾牌：中间台座 Socket。若不手动拖，则默认读取 platformSockets[1]")]
    public XRSocketInteractor shieldSocket;

    [Tooltip("斑点豹头：右侧台座 Socket。若不手动拖，则默认读取 platformSockets[2]")]
    public XRSocketInteractor jaguarSocket;

    [Header("Socket 索引设置")]
    [Tooltip("玉米神对应 platformSockets 的索引，通常左侧台座是 0")]
    public int cornSocketIndex = 0;

    [Tooltip("盾牌对应 platformSockets 的索引，通常中间台座是 1")]
    public int shieldSocketIndex = 1;

    [Tooltip("斑点豹头对应 platformSockets 的索引，通常右侧台座是 2")]
    public int jaguarSocketIndex = 2;

    [Header("正确性判断")]
    [Tooltip("是否比较 Socket 和碎片上的 ColorID。建议保持勾选，和 PuzzleSequenceManager 一致。")]
    public bool verifyColorID = true;

    [Tooltip("如果物体上没有 ColorID，是否只要放进 Socket 就算正确。一般不建议勾选。")]
    public bool allowMissingColorIDAsCorrect = false;

    [Header("调试")]
    public bool showDebugLog = true;

    [Tooltip("Pico 真机测试时，把转盘角度调试信息直接显示在对话框里")]
    public bool showDialDebugOnPanel = true;

    [Tooltip("调试信息每隔多少秒刷新一次")]
    public float panelDebugRefreshInterval = 0.2f;

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
    private FieldInfo puzzleStateField;
    private bool hasCachedPuzzleStateField = false;
    private float lastPanelDebugTime = 0f;
    private string currentWaitingOriginalText = "";

    // --- 新增：记录当前激活的视觉提示物体 ---
    private GameObject activeVisualHint = null;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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

        ResolveSocketsFromPuzzleManager();

        StartDialogue();
    }

    void OnDisable()
    {
        skipAction?.Disable();
    }

    void ResolveSocketsFromPuzzleManager()
    {
        if (!useSocketsFromPuzzleSequenceManager)
            return;

        if (puzzleSequenceManager == null)
            return;

        XRSocketInteractor[] sockets = puzzleSequenceManager.platformSockets;

        if (sockets == null)
            return;

        if (cornSocket == null && IsValidSocketIndex(sockets, cornSocketIndex))
            cornSocket = sockets[cornSocketIndex];

        if (shieldSocket == null && IsValidSocketIndex(sockets, shieldSocketIndex))
            shieldSocket = sockets[shieldSocketIndex];

        if (jaguarSocket == null && IsValidSocketIndex(sockets, jaguarSocketIndex))
            jaguarSocket = sockets[jaguarSocketIndex];

        verifyColorID = puzzleSequenceManager.verifyColorID;
    }

    bool IsValidSocketIndex(XRSocketInteractor[] sockets, int index)
    {
        return sockets != null && index >= 0 && index < sockets.Length;
    }

    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            if (showDebugLog)
                Debug.LogWarning("DialogueSystemMayaGlyph：没有设置任何对话内容。");
            return;
        }

        ResolveSocketsFromPuzzleManager();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        currentLineIndex = 0;
        isWaitingForInteraction = false;

        OnDialogueStart?.Invoke();

        if (showDebugLog)
            Debug.Log("玛雅文字对话开始。");

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

        // --- 新增逻辑：切换行时先关闭之前的视觉提示 ---
        if (activeVisualHint != null)
        {
            activeVisualHint.SetActive(false);
            activeVisualHint = null;
        }

        isWaitingForNext = false;
        isWaitingForInteraction = false;

        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count)
            return;

        DialogueLineMayaGlyph line = dialogueLines[currentLineIndex];

        // --- 新增逻辑：开启此行配置的提示物体 ---
        if (line.visualHintObject != null)
        {
            line.visualHintObject.SetActive(true);
            activeVisualHint = line.visualHintObject;
        }

        if (speakerText != null)
            speakerText.text = line.speaker;
        UpdateSpeakerAvatar(line.speaker);
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        _onLineStart?.Invoke(line.speaker);
        _onLineStartWithContent?.Invoke(line.speaker, line.content);

        if (showDebugLog)
        {
            string driveMode = line.clip != null ? "音频驱动" : "文字驱动";
            Debug.Log($"玛雅文字播放第 {currentLineIndex + 1} 句：{line.speaker} / LineId：{line.lineId} / {driveMode} / 等待类型：{line.waitType} / 限时分支：{line.useTimeoutBranch} / {line.content}");
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

        DialogueLineMayaGlyph line = dialogueLines[currentLineIndex];

        // 如果这一句设置了图片：
        // 音频/文字播完后，先显示图片，图片消失后再继续下一句或进入交互等待。
        if (line.showMayaImage && line.mayaImageObject != null)
        {
            if (mayaImageCoroutine != null)
            {
                StopCoroutine(mayaImageCoroutine);
                mayaImageCoroutine = null;
            }

            mayaImageCoroutine = StartCoroutine(ShowMayaImageThenContinue(line));
            return;
        }

        ContinueAfterCurrentLine(line);
    }

    IEnumerator ShowMayaImageThenContinue(DialogueLineMayaGlyph line)
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

        float elapsed = 0f;
        float fadeInDuration = Mathf.Max(0.01f, line.mayaImageFadeInDuration);

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        float stayDuration = Mathf.Max(0f, line.mayaImageStayDuration);
        yield return new WaitForSeconds(stayDuration);

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

        ContinueAfterCurrentLine(line);
    }

    void ContinueAfterCurrentLine(DialogueLineMayaGlyph line)
    {
        if (line == null)
            return;

        if (line.waitType == MayaGlyphWaitType.None)
        {
            float delay = line.clip != null ? autoPlayDelay : textModeDelay;
            StartWaitForNext(delay);
        }
        else
        {
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
            DialogueLineMayaGlyph currentLine = dialogueLines[currentLineIndex];

            _onLineEnd?.Invoke(currentLine.speaker);

            TryForceCompleteBallPuzzleAfterLine(currentLine);
            TryForceCompleteCalendarPuzzleAfterLine(currentLine);

            if (JumpByAfterLineTarget(currentLine))
                yield break;
        }

        NextLine();
    }

    void StartInteractionWait(MayaGlyphWaitType waitType)
    {
        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        isWaitingForInteraction = true;
        interactionCoroutine = StartCoroutine(WaitForInteraction(waitType));
    }

    IEnumerator WaitForInteraction(MayaGlyphWaitType waitType)
    {
        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count)
            yield break;

        DialogueLineMayaGlyph line = dialogueLines[currentLineIndex];

        if (showDebugLog)
            Debug.Log($"玛雅文字开始等待交互：{waitType}，是否启用限时分支：{line.useTimeoutBranch}，限时：{line.timeLimit} 秒");

        currentWaitingOriginalText = line.content;

        float startTime = Time.time;
        bool timeoutHappened = false;

        while (true)
        {
            if (showDialDebugOnPanel)
            {
                UpdatePanelDebugForWaitType(waitType);
            }

            if (IsInteractionCompleted(waitType))
            {
                if (showDebugLog)
                    Debug.Log($"玛雅文字交互完成：{waitType}，进入 Success 分支或下一句。");

                timeoutHappened = false;
                break;
            }

            if (line.useTimeoutBranch && line.timeLimit > 0f && Time.time - startTime >= line.timeLimit)
            {
                if (showDebugLog)
                    Debug.Log($"玛雅文字交互超时：{waitType}，进入 Timeout 分支。");

                timeoutHappened = true;
                break;
            }

            yield return null;
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
        }

        if (!timeoutHappened)
        {
            TryForceCompleteCalendarPuzzleOnInteractionSuccess(line);
        }

        if (line.useTimeoutBranch)
        {
            bool jumped = JumpByBranchTarget(line, timeoutHappened);
            if (jumped)
                yield break;
        }

        NextLine();
    }

    // ... 判断逻辑代码保持不变 ...
    bool IsInteractionCompleted(MayaGlyphWaitType waitType)
    {
        switch (waitType)
        {
            case MayaGlyphWaitType.WaitUntilCornPlaced:
                return IsSocketCorrect(cornSocket, "玉米神/对应台座");
            case MayaGlyphWaitType.WaitUntilShieldPlaced:
                return IsSocketCorrect(shieldSocket, "盾牌/对应台座");
            case MayaGlyphWaitType.WaitUntilJaguarPlaced:
                return IsSocketCorrect(jaguarSocket, "斑点豹头/对应台座");
            case MayaGlyphWaitType.WaitUntilLeftInnerDialCorrect:
                return IsDialCorrect(puzzleSequenceManager.leftInnerDial, puzzleSequenceManager.targetLeftInner, "左侧内盘");
            case MayaGlyphWaitType.WaitUntilLeftOuterDialCorrect:
                return IsDialCorrect(puzzleSequenceManager.leftOuterDial, puzzleSequenceManager.targetLeftOuter, "左侧外盘");
            case MayaGlyphWaitType.WaitUntilRightInnerDialCorrect:
                return IsDialCorrect(puzzleSequenceManager.rightInnerDial, puzzleSequenceManager.targetRightInner, "右侧内盘");
            case MayaGlyphWaitType.WaitUntilRightOuterDialCorrect:
                return IsDialCorrect(puzzleSequenceManager.rightOuterDial, puzzleSequenceManager.targetRightOuter, "右侧外盘")
                       || IsPuzzleSequenceInOrAfterSecondTransition();
        }
        return false;
    }

    void UpdatePanelDebugForWaitType(MayaGlyphWaitType waitType)
    {
        if (dialogueText == null) return;
        if (Time.time - lastPanelDebugTime < panelDebugRefreshInterval) return;
        lastPanelDebugTime = Time.time;
        string debugText = "";
        switch (waitType)
        {
            case MayaGlyphWaitType.WaitUntilLeftInnerDialCorrect:
                debugText = GetDialDebugText(puzzleSequenceManager?.leftInnerDial, puzzleSequenceManager?.targetLeftInner ?? 0, "左侧内盘");
                break;
            case MayaGlyphWaitType.WaitUntilLeftOuterDialCorrect:
                debugText = GetDialDebugText(puzzleSequenceManager?.leftOuterDial, puzzleSequenceManager?.targetLeftOuter ?? 0, "左侧外盘");
                break;
            case MayaGlyphWaitType.WaitUntilRightInnerDialCorrect:
                debugText = GetDialDebugText(puzzleSequenceManager?.rightInnerDial, puzzleSequenceManager?.targetRightInner ?? 0, "右侧内盘");
                break;
            case MayaGlyphWaitType.WaitUntilRightOuterDialCorrect:
                debugText = GetDialDebugText(puzzleSequenceManager?.rightOuterDial, puzzleSequenceManager?.targetRightOuter ?? 0, "右侧外盘");
                break;
        }
        if (string.IsNullOrEmpty(debugText)) return;
        dialogueText.text = currentWaitingOriginalText + "\n\n" + debugText;
    }

    bool IsSocketCorrect(XRSocketInteractor socket, string debugName)
    {
        if (socket == null) return false;
        if (!socket.hasSelection) return false;
        IXRSelectInteractable selected = socket.GetOldestInteractableSelected();
        if (selected == null) return false;
        if (!verifyColorID) return true;
        ColorID socketColor = FindColorID(socket.transform);
        ColorID selectedColor = FindColorID(selected.transform);
        if (socketColor == null || selectedColor == null) return allowMissingColorIDAsCorrect;
        return socketColor.objectColor == selectedColor.objectColor;
    }

    bool IsDialCorrect(Transform dial, float targetAngle, string debugName)
    {
        if (puzzleSequenceManager == null || dial == null) return false;
        float currentAngle = GetDialAngle(dial);
        float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
        return difference <= puzzleSequenceManager.angleTolerance;
    }

    bool IsPuzzleSequenceInOrAfterSecondTransition()
    {
        if (puzzleSequenceManager == null) return false;
        if (!hasCachedPuzzleStateField)
        {
            puzzleStateField = typeof(PuzzleSequenceManager).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            hasCachedPuzzleStateField = true;
        }
        if (puzzleStateField == null) return false;
        object stateValue = puzzleStateField.GetValue(puzzleSequenceManager);
        if (stateValue == null) return false;
        string stateName = stateValue.ToString();
        return stateName == "SecondTransition" || stateName == "Finished";
    }

    string GetDialDebugText(Transform dial, float targetAngle, string debugName)
    {
        if (puzzleSequenceManager == null || dial == null) return "";
        float currentAngle = GetDialAngle(dial);
        float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
        bool matched = difference <= puzzleSequenceManager.angleTolerance;
        return $"<color=yellow>[调试] {debugName}</color>\n当前角度：{currentAngle:F1}\n目标角度：{targetAngle:F1}\n差值：{difference:F1}\n是否通过：{matched}";
    }

    float GetDialAngle(Transform dial)
    {
        Vector3 euler = dial.localEulerAngles;
        switch (puzzleSequenceManager.dialRotationAxis)
        {
            case PuzzleSequenceManager.RotationAxis.X: return euler.x;
            case PuzzleSequenceManager.RotationAxis.Y: return euler.y;
            case PuzzleSequenceManager.RotationAxis.Z: return euler.z;
            default: return euler.x;
        }
    }

    ColorID FindColorID(Transform target)
    {
        if (target == null) return null;
        ColorID id = target.GetComponent<ColorID>();
        if (id != null) return id;
        id = target.GetComponentInParent<ColorID>();
        if (id != null) return id;
        return target.GetComponentInChildren<ColorID>();
    }

    void OnSkipInput()
    {
#if UNITY_EDITOR
        if (!allowSkipInteractionInEditor && !useRightGrip) return;
#endif
        if (dialoguePanel == null || !dialoguePanel.activeSelf) return;
        if (currentLineIndex < 0 || currentLineIndex >= dialogueLines.Count) return;

        if (isWaitingForInteraction)
        {
#if UNITY_EDITOR
            if (allowSkipInteractionInEditor)
            {
                if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
                interactionCoroutine = null;
                isWaitingForInteraction = false;

                // 跳过时确保关闭视觉提示
                if (activeVisualHint != null) activeVisualHint.SetActive(false);

                DialogueLineMayaGlyph currentLine = dialogueLines[currentLineIndex];

                _onLineEnd?.Invoke(currentLine.speaker);

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
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
            if (dialogueText != null) dialogueText.text = dialogueLines[currentLineIndex].content;
            if (waitCoroutine != null) StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            AfterLineFullyDisplayed();
        }
        else if (isWaitingForNext)
        {
            if (waitCoroutine != null) StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            isWaitingForNext = false;
            DialogueLineMayaGlyph currentLine = dialogueLines[currentLineIndex];

            _onLineEnd?.Invoke(currentLine.speaker);

            TryForceCompleteBallPuzzleAfterLine(currentLine);
            TryForceCompleteCalendarPuzzleAfterLine(currentLine);

            if (JumpByAfterLineTarget(currentLine))
                return;

            NextLine();
        }
    }
    void TryForceCompleteBallPuzzleAfterLine(DialogueLineMayaGlyph line)
    {
        if (line == null || !line.forceCompleteBallPuzzleAfterLine)
            return;

        if (puzzleSequenceManager == null)
        {
            Debug.LogWarning("本句设置了强制完成方块关卡，但 DialogueSystemMayaGlyph 没有拖入 PuzzleSequenceManager。");
            return;
        }

        if (showDebugLog)
            Debug.Log($"玛雅文字：Line Id {line.lineId} 播完后，强制触发方块关卡通关转场。");

        puzzleSequenceManager.ForceCompleteBallPuzzle();
    }

    void TryForceCompleteCalendarPuzzleOnInteractionSuccess(DialogueLineMayaGlyph line)
    {
        if (line == null || !line.forceCompleteCalendarPuzzleOnInteractionSuccess)
            return;

        ForceCompleteCalendarPuzzleFromDialogue(line, "交互成功");
    }

    void TryForceCompleteCalendarPuzzleAfterLine(DialogueLineMayaGlyph line)
    {
        if (line == null || !line.forceCompleteCalendarPuzzleAfterLine)
            return;

        ForceCompleteCalendarPuzzleFromDialogue(line, "普通播完");
    }

    void ForceCompleteCalendarPuzzleFromDialogue(DialogueLineMayaGlyph line, string triggerReason)
    {
        if (puzzleSequenceManager == null)
        {
            Debug.LogWarning("本句设置了强制完成转盘关卡，但 DialogueSystemMayaGlyph 没有拖入 PuzzleSequenceManager。");
            return;
        }

        if (showDebugLog)
            Debug.Log($"玛雅文字：Line Id {line.lineId} {triggerReason}后，强制触发转盘关卡通关转场。");

        puzzleSequenceManager.ForceCompleteCalendarPuzzle();
    }

    bool JumpByAfterLineTarget(DialogueLineMayaGlyph line)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.afterLineTargetLineId))
            return false;

        int targetIndex = FindLineIndexById(line.afterLineTargetLineId);

        if (IsValidLineIndex(targetIndex))
        {
            if (showDebugLog)
                Debug.Log($"玛雅文字普通播完跳转：Line Id {line.lineId} -> {line.afterLineTargetLineId}，也就是 Element {targetIndex}。");

            JumpToLine(targetIndex);
            return true;
        }

        Debug.LogWarning($"玛雅文字普通播完跳转失败：找不到 Line Id = {line.afterLineTargetLineId}。请检查目标句子的 Line Id 是否完全一致。将改为进入下一句。");
        return false;
    }

    bool JumpByBranchTarget(DialogueLineMayaGlyph line, bool timeoutHappened)
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
                    Debug.Log($"玛雅文字 {branchName} 分支跳转到 Line Id：{targetLineId}，也就是 Element {targetIndex}。");

                JumpToLine(targetIndex);
                return true;
            }

            Debug.LogWarning($"玛雅文字跳转失败：找不到 Line Id = {targetLineId}。请检查目标句子的 Line Id 是否完全一致。将改为进入下一句。");
            return false;
        }

        // 兼容旧版 Element 编号
        if (IsValidLineIndex(fallbackIndex))
        {
            if (showDebugLog)
                Debug.Log($"玛雅文字 {branchName} 分支跳转到旧版 Element {fallbackIndex}。");

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

        if (showDebugLog)
            Debug.Log("玛雅文字对话结束。");
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

    // ... TemporaryLine 相关逻辑保持不变 ...
    public void ShowTemporaryLine(string speaker, string content, float duration = 3f)
    {
        if (temporaryLineCoroutine != null) StopCoroutine(temporaryLineCoroutine);
        temporaryLineCoroutine = StartCoroutine(ShowTemporaryLineRoutine(speaker, content, duration));
    }

    IEnumerator ShowTemporaryLineRoutine(string speaker, string content, float duration)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        string originalSpeaker = (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count) ? dialogueLines[currentLineIndex].speaker : "";
        string originalText = (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count) ? dialogueLines[currentLineIndex].content : "";

        if (speakerText != null)
            speakerText.text = originalSpeaker;

        UpdateSpeakerAvatar(originalSpeaker);

        if (dialogueText != null)
            dialogueText.text = originalText;

        yield return new WaitForSeconds(duration);

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count)
        {
            if (speakerText != null) speakerText.text = originalSpeaker;
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