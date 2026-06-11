using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea]
    public string content;

    public AudioClip clip;   // 配音音频

    [Header("交互等待设置")]
    public DialogueInteractionWaitType waitType = DialogueInteractionWaitType.None;

    [Header("视觉提示（新增）")]
    [Tooltip("这一行对话显示时，场景中需要激活的3D提示物体（如黄色手柄边框）")]
    public GameObject visualHintObject;

    // +++ NEW +++ 动画触发器名称
    [Header("动画触发")]
    public string animTriggerName;   // 例如 "Wave", "Point", "Talk"
}

[System.Serializable]
public class TemporaryDialogueLine
{
    public string speaker;

    [TextArea]
    public string content;

    public AudioClip clip;
}

[System.Serializable]
public class SpeakerAvatarConfig
{
    public string speaker;
    public Sprite avatar;
}

public enum DialogueInteractionWaitType
{
    None,

    [Tooltip("这句提示后，等待玩家抓住红外成像仪")]
    WaitUntilInfraredHeld,

    [Tooltip("这句提示后，等待玩家打开红外成像仪")]
    WaitUntilInfraredOpened
}

public enum InfraredOpenDetectMode
{
    [Tooltip("通过 Heatmap 的 MeshRenderer 是否显示来判断红外成像仪是否开启。推荐优先用这个。")]
    DetectByHeatmapRendererVisible,

    [Tooltip("通过 Heatmap 物体是否 Active 来判断红外成像仪是否开启。")]
    DetectByHeatmapGameObjectActive,

    [Tooltip("兜底方案：玩家抓住红外成像仪后，按一次左手食指扳机键，就认为红外成像仪已开启。")]
    DetectByLeftTriggerPressedWhileHolding
}

public class DialogueSystem : MonoBehaviour
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
    public bool allowSkipInteractionInEditor = true;
    public bool disableReleaseWarningInEditor = true;

    [Header("对话 UI")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("说话者头像")]
    public Image speakerAvatarImage;
    public List<SpeakerAvatarConfig> speakerAvatars = new List<SpeakerAvatarConfig>();
    public bool hideAvatarIfNotFound = true;

    [Header("对话内容")]
    public List<DialogueLine> dialogueLines;

    [Tooltip("有音频时：音频播放完后等待多少秒进入下一句")]
    public float autoPlayDelay = 2.0f;

    [Header("播放驱动设置")]
    [Tooltip("没有音频时：文字直接显示后等待多少秒进入下一句")]
    public float textModeDelay = 3.0f;

    [Header("跳过键设置")]
    public bool useRightGrip = false;

    [Header("红外成像仪检测")]
    [Tooltip("把红外成像仪物体上的 XR Grab Interactable 拖进来，用来判断玩家是否正在抓住它。")]
    public XRGrabInteractable infraredGrabInteractable;

    [Tooltip("如果上面没拖，可以直接拖红外成像仪物体，脚本会自动找 XR Grab Interactable。")]
    public GameObject infraredObject;

    [Tooltip("判断红外成像仪是否开启的方式。优先推荐 DetectByHeatmapRendererVisible。")]
    public InfraredOpenDetectMode openDetectMode = InfraredOpenDetectMode.DetectByHeatmapRendererVisible;

    [Tooltip("Heatmap 物体的 MeshRenderer。用来判断热力图是否显示。")]
    public MeshRenderer heatmapRenderer;

    [Tooltip("Heatmap 物体。如果用 DetectByHeatmapGameObjectActive，就拖这个。")]
    public GameObject heatmapObject;

    [Header("松手提示")]
    public string warningSpeaker = "提示";

    [TextArea]
    public string releaseWarningText = "请长按左手中指抓握键，松开按键红外成像仪会掉。";

    public float releaseWarningDuration = 2.5f;

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
    private Coroutine sequenceTemporaryCoroutine = null;

    private AudioSource audioSource;
    private InputAction skipAction;
    private InputAction leftTriggerAction;

    private bool leftTriggerPressedOnce = false;
    private bool wasInfraredHeldLastFrame = false;
    private bool isShowingReleaseWarningUntilPickedUp = false;
    private bool hasDialogueEnded = false;

    private GameObject activeVisualHint = null;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (infraredGrabInteractable == null && infraredObject != null)
            infraredGrabInteractable = infraredObject.GetComponentInChildren<XRGrabInteractable>();

        if (heatmapRenderer == null && heatmapObject != null)
            heatmapRenderer = heatmapObject.GetComponent<MeshRenderer>();
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

        leftTriggerAction = new InputAction(
            "LeftTriggerForInfrared",
            binding: "<XRController>{LeftHand}/triggerPressed"
        );

        leftTriggerAction.performed += ctx =>
        {
            leftTriggerPressedOnce = true;

            if (showDebugLog)
                Debug.Log("检测到左手食指扳机键按下。");
        };

        leftTriggerAction.Enable();

        StartDialogue();
    }

    void Update()
    {
        CheckInfraredReleasedWarning();
    }

    void OnDisable()
    {
        skipAction?.Disable();
        leftTriggerAction?.Disable();
    }

    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
            return;

        hasDialogueEnded = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        currentLineIndex = 0;
        isWaitingForInteraction = false;
        leftTriggerPressedOnce = false;
        wasInfraredHeldLastFrame = IsInfraredHeld();

        OnDialogueStart?.Invoke();
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

        DialogueLine line = dialogueLines[currentLineIndex];

        if (line.visualHintObject != null)
        {
            line.visualHintObject.SetActive(true);
            activeVisualHint = line.visualHintObject;
        }

        if (speakerText != null)
            speakerText.text = line.speaker;
        UpdateSpeakerAvatar(line.speaker);

        // +++ NEW +++ 触发动画（如果配置了触发器名称）
        if (targetAnimator != null && !string.IsNullOrEmpty(line.animTriggerName))
        {
            targetAnimator.SetTrigger(line.animTriggerName);
            if (showDebugLog) Debug.Log($"触发动画：{line.animTriggerName}");
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        _onLineStart?.Invoke(line.speaker);
        _onLineStartWithContent?.Invoke(line.speaker, line.content);

        if (showDebugLog)
        {
            string driveMode = line.clip != null ? "音频驱动" : "文字驱动";
            Debug.Log($"播放第 {currentLineIndex + 1} 句：{line.speaker} / {driveMode} / 等待类型：{line.waitType} / {line.content}");
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

            if (!isShowingReleaseWarningUntilPickedUp)
            {
                if (dialogueText != null)
                    dialogueText.text = fullText.Substring(0, charsToShow);
            }

            yield return null;
        }

        if (!isShowingReleaseWarningUntilPickedUp)
        {
            if (dialogueText != null)
                dialogueText.text = fullText;
        }

        displayCoroutine = null;

        AfterLineFullyDisplayed();
    }

    void AfterLineFullyDisplayed()
    {
        DialogueLine line = dialogueLines[currentLineIndex];

        if (line.waitType == DialogueInteractionWaitType.None)
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

        while (isShowingReleaseWarningUntilPickedUp)
        {
            yield return null;
        }

        isWaitingForNext = false;
        waitCoroutine = null;

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count)
        {
            _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);
        }

        NextLine();
    }

    void StartInteractionWait(DialogueInteractionWaitType waitType)
    {
        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        isWaitingForInteraction = true;

        if (waitType == DialogueInteractionWaitType.WaitUntilInfraredOpened)
        {
            leftTriggerPressedOnce = false;
        }

        interactionCoroutine = StartCoroutine(WaitForInteraction(waitType));
    }

    IEnumerator WaitForInteraction(DialogueInteractionWaitType waitType)
    {
        if (showDebugLog)
            Debug.Log($"开始等待交互：{waitType}");

        while (true)
        {
            if (waitType == DialogueInteractionWaitType.WaitUntilInfraredHeld)
            {
                if (IsInfraredHeld())
                {
                    if (showDebugLog)
                        Debug.Log("红外成像仪已被玩家抓住，交互提示1完成。");

                    break;
                }
            }
            else if (waitType == DialogueInteractionWaitType.WaitUntilInfraredOpened)
            {
                if (IsInfraredOpened())
                {
                    if (showDebugLog)
                        Debug.Log("红外成像仪已开启，交互提示2完成，准备进入下一句或自由探索。");

                    break;
                }

                if (!IsInfraredHeld() && !isShowingReleaseWarningUntilPickedUp)
                {
                    ShowReleaseWarning();
                }
            }

            yield return null;
        }

        if (temporaryLineCoroutine != null)
        {
            StopCoroutine(temporaryLineCoroutine);
            temporaryLineCoroutine = null;
            isShowingReleaseWarningUntilPickedUp = false;
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

        NextLine();
    }

    bool IsInfraredHeld()
    {
        if (infraredGrabInteractable == null)
        {
            if (infraredObject != null)
                infraredGrabInteractable = infraredObject.GetComponentInChildren<XRGrabInteractable>();

            if (infraredGrabInteractable == null)
                return false;
        }

        return infraredGrabInteractable.isSelected;
    }

    bool IsInfraredOpened()
    {
        if (openDetectMode == InfraredOpenDetectMode.DetectByHeatmapRendererVisible)
        {
            if (heatmapRenderer == null && heatmapObject != null)
                heatmapRenderer = heatmapObject.GetComponent<MeshRenderer>();

            if (heatmapRenderer == null)
            {
                if (showDebugLog)
                    Debug.LogWarning("IsInfraredOpened：Heatmap Renderer 没有赋值，所以无法判断已开启。");

                return false;
            }

            bool opened = heatmapRenderer.enabled && heatmapRenderer.gameObject.activeInHierarchy;

            if (showDebugLog && Time.frameCount % 60 == 0)
                Debug.Log($"IsInfraredOpened Renderer检测：enabled={heatmapRenderer.enabled}, activeInHierarchy={heatmapRenderer.gameObject.activeInHierarchy}, opened={opened}");

            return opened;
        }

        if (openDetectMode == InfraredOpenDetectMode.DetectByHeatmapGameObjectActive)
        {
            if (heatmapObject == null)
            {
                if (showDebugLog)
                    Debug.LogWarning("IsInfraredOpened：Heatmap Object 没有赋值，所以无法判断已开启。");

                return false;
            }

            bool opened = heatmapObject.activeInHierarchy;

            if (showDebugLog && Time.frameCount % 60 == 0)
                Debug.Log($"IsInfraredOpened 物体Active检测：activeInHierarchy={heatmapObject.activeInHierarchy}, opened={opened}");

            return opened;
        }

        if (openDetectMode == InfraredOpenDetectMode.DetectByLeftTriggerPressedWhileHolding)
        {
            bool held = IsInfraredHeld();
            bool opened = held && leftTriggerPressedOnce;

            if (showDebugLog && Time.frameCount % 60 == 0)
                Debug.Log($"IsInfraredOpened 扳机检测：held={held}, leftTriggerPressedOnce={leftTriggerPressedOnce}, opened={opened}");

            return opened;
        }

        return false;
    }

    void CheckInfraredReleasedWarning()
    {
        bool isHeldNow = IsInfraredHeld();

        if (isShowingReleaseWarningUntilPickedUp)
        {
            wasInfraredHeldLastFrame = isHeldNow;
            return;
        }

        if (wasInfraredHeldLastFrame && !isHeldNow)
        {
            ShowReleaseWarning();
        }

        wasInfraredHeldLastFrame = isHeldNow;
    }

    void ShowReleaseWarning()
    {
#if UNITY_EDITOR
        if (disableReleaseWarningInEditor)
            return;
#endif

        if (string.IsNullOrEmpty(releaseWarningText))
            return;

        if (showDebugLog)
            Debug.Log("玩家松开红外成像仪，显示持续松手提示，直到重新拾取。");

        if (temporaryLineCoroutine != null)
        {
            StopCoroutine(temporaryLineCoroutine);
            temporaryLineCoroutine = null;
        }

        temporaryLineCoroutine = StartCoroutine(ShowReleaseWarningUntilPickedUp());
    }

    IEnumerator ShowReleaseWarningUntilPickedUp()
    {
        isShowingReleaseWarningUntilPickedUp = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        string originalSpeaker = "";
        string originalText = "";

        bool hasValidCurrentLine =
            !hasDialogueEnded &&
            currentLineIndex >= 0 &&
            currentLineIndex < dialogueLines.Count;

        if (hasValidCurrentLine)
        {
            originalSpeaker = dialogueLines[currentLineIndex].speaker;
            originalText = dialogueLines[currentLineIndex].content;
        }

        if (speakerText != null)
            speakerText.text = warningSpeaker;

        UpdateSpeakerAvatar("诺亚");

        if (dialogueText != null)
            dialogueText.text = releaseWarningText;

        while (!IsInfraredHeld())
            yield return null;

        isShowingReleaseWarningUntilPickedUp = false;

        if (hasValidCurrentLine)
        {
            if (speakerText != null)
                speakerText.text = originalSpeaker;

            UpdateSpeakerAvatar(originalSpeaker);

            if (dialogueText != null)
                dialogueText.text = originalText;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        temporaryLineCoroutine = null;
    }

    void OnSkipInput()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf)
            return;

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

                if (activeVisualHint != null) activeVisualHint.SetActive(false);

                _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);

                NextLine();
                return;
            }
#endif
            return;
        }

        if (displayCoroutine != null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            StopCoroutine(displayCoroutine);
            displayCoroutine = null;

            if (dialogueText != null)
                dialogueText.text = dialogueLines[currentLineIndex].content;

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

            _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);

            NextLine();
        }
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < dialogueLines.Count)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        StopCurrentLineCoroutines();

        hasDialogueEnded = true;

        if (activeVisualHint != null)
        {
            activeVisualHint.SetActive(false);
            activeVisualHint = null;
        }

        if (dialoguePanel != null && !isShowingReleaseWarningUntilPickedUp)
            dialoguePanel.SetActive(false);

        OnDialogueEnd?.Invoke();

        if (showDebugLog)
            Debug.Log("对话结束");
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
    }

    public void ShowTemporaryLine(string speaker, string content, float duration = 3f)
    {
        if (isShowingReleaseWarningUntilPickedUp)
            return;

        if (temporaryLineCoroutine != null)
            StopCoroutine(temporaryLineCoroutine);

        temporaryLineCoroutine = StartCoroutine(ShowTemporaryLineRoutine(speaker, content, duration));
    }

    IEnumerator ShowTemporaryLineRoutine(string speaker, string content, float duration)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        string originalSpeaker = "";
        string originalText = "";

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count)
        {
            originalSpeaker = dialogueLines[currentLineIndex].speaker;
            originalText = dialogueLines[currentLineIndex].content;
        }

        if (speakerText != null)
            speakerText.text = speaker;
        UpdateSpeakerAvatar(speaker);
        if (dialogueText != null)
            dialogueText.text = content;

        yield return new WaitForSeconds(duration);

        if (isWaitingForInteraction)
        {
            if (speakerText != null)
                speakerText.text = originalSpeaker;
            UpdateSpeakerAvatar(originalSpeaker);
            if (dialogueText != null)
                dialogueText.text = originalText;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        temporaryLineCoroutine = null;
    }

    public void PlayTemporaryDialogueSequence(
        List<TemporaryDialogueLine> lines,
        System.Action onComplete = null,
        float textLineDelay = 2.5f,
        float audioLineDelay = 0.5f
    )
    {
        if (sequenceTemporaryCoroutine != null)
        {
            StopCoroutine(sequenceTemporaryCoroutine);
            sequenceTemporaryCoroutine = null;
        }

        sequenceTemporaryCoroutine = StartCoroutine(
            PlayTemporaryDialogueSequenceRoutine(lines, onComplete, textLineDelay, audioLineDelay)
        );
    }

    IEnumerator PlayTemporaryDialogueSequenceRoutine(
        List<TemporaryDialogueLine> lines,
        System.Action onComplete,
        float textLineDelay,
        float audioLineDelay
    )
    {
        if (lines == null || lines.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (temporaryLineCoroutine != null)
        {
            StopCoroutine(temporaryLineCoroutine);
            temporaryLineCoroutine = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        for (int i = 0; i < lines.Count; i++)
        {
            TemporaryDialogueLine line = lines[i];

            if (speakerText != null)
                speakerText.text = line.speaker;
            UpdateSpeakerAvatar(line.speaker);
            if (dialogueText != null)
                dialogueText.text = line.content;

            _onLineStart?.Invoke(line.speaker);
            _onLineStartWithContent?.Invoke(line.speaker, line.content);

            if (line.clip != null)
            {
                audioSource.clip = line.clip;
                audioSource.Play();

                yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
                yield return new WaitForSeconds(audioLineDelay);
            }
            else
            {
                yield return new WaitForSeconds(textLineDelay);
            }

            _onLineEnd?.Invoke(line.speaker);
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        sequenceTemporaryCoroutine = null;
        onComplete?.Invoke();
    }
}