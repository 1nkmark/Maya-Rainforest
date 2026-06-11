using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private DialogueSystem dialogueSystem;
    private NavMeshAgentAI patrol;

    private bool hasPlayed = false;

    [Header("自由探索设置")]
    public float idleThreshold = 8f;
    public float maxExploreTime = 30f;

    [Header("提示对话框设置")]
    public DialogueSystem dialogueSystemForPrompt;

    [Tooltip("巡航前提示文案列表。可以配置多句话、多个角色、每句话可选音频。")]
    public List<CruisePromptDialogueLine> promptDialogueLines = new List<CruisePromptDialogueLine>();

    [Header("播放驱动设置")]
    [Tooltip("没有音频时：文字直接显示后等待多少秒进入下一句。")]
    public float textModeDelay = 3.0f;

    [Tooltip("有音频时：音频播放完后额外等待多少秒进入下一句。")]
    public float audioLineDelay = 0.5f;

    [Header("输入检测模式")]
    [Tooltip("电脑测试时不要勾选；连VR头显/手柄测试时勾选")]
    public bool useControllerInput = false;

    [Tooltip("电脑测试时，用 WASD / 方向键 判断玩家正在活动")]
    public bool detectKeyboardInput = true;

    [Tooltip("VR模式下，摇杆输入超过该值，认为玩家正在活动")]
    public float controllerAxisThreshold = 0.2f;

    [Header("VR活动检测：头显/手柄移动")]
    [Tooltip("头显或手柄位置变化超过这个距离，认为玩家正在活动")]
    public float positionMoveThreshold = 0.015f;

    [Tooltip("头显或手柄旋转变化超过这个角度，认为玩家正在活动")]
    public float rotationMoveThreshold = 3f;

    [Tooltip("是否检测头显移动/转动")]
    public bool detectHeadMovement = true;

    [Tooltip("是否检测手柄移动/转动")]
    public bool detectHandMovement = true;

    private Vector3 lastHeadPosition;
    private Quaternion lastHeadRotation;
    private Vector3 lastLeftHandPosition;
    private Quaternion lastLeftHandRotation;
    private Vector3 lastRightHandPosition;
    private Quaternion lastRightHandRotation;

    [Header("调试")]
    public bool showExploreDebugLog = true;

    private bool isExploring = false;
    private float exploreStartTime;
    private float idleTime;

    private bool hasLastXRPoses = false;
    private bool hasTriggeredCruise = false;
    private bool dialogueEnded = false;
    private bool greetingFinished = false;

    private Coroutine freeExploreCoroutine;
    private Coroutine greetingCoroutine;
    private Coroutine disappointedCoroutine;
    private Coroutine cruisePromptCoroutine;
    private AudioSource promptAudioSource;

    void Awake()
    {
        animator = GetComponent<Animator>();
        patrol = GetComponent<NavMeshAgentAI>();

        GameObject dialogRoot = GameObject.Find("DialogueManager");

        if (dialogRoot != null)
        {
            dialogueSystem = dialogRoot.GetComponent<DialogueSystem>();

            if (dialogueSystem != null)
            {
                dialogueSystem.OnLineStartWithContent += OnLineStartWithContent;
                dialogueSystem.OnLineEnd += OnLineEnd;
                dialogueSystem.OnDialogueEnd += OnDialogueEnd;
                Debug.Log("诺亚博士动画事件注册完成");
            }
            else
            {
                Debug.LogError("❌ DialogueManager 上没有 DialogueSystem 组件！");
            }
        }
        else
        {
            Debug.LogError("❌ 场景中找不到 DialogueManager！");
        }
    }

    void Start()
    {
        if (animator == null) { Debug.LogError("❌ 找不到 Animator 组件！"); return; }
        if (patrol == null) { Debug.LogError("❌ 找不到 NavMeshAgentAI 组件！"); return; }

        patrol.StopPatrol();

        ResetNoahEmotionBools();
        animator.SetBool("isWalking", false);

    }

    void ResetNoahEmotionBools()
    {
        animator.SetBool("isGreeting", false);
        animator.SetBool("isDisappointed", false);
    }

    void OnLineStartWithContent(string speaker, string content)
    {
        Debug.Log($"【诺亚动画收到台词】speaker={speaker} content={content}");

        if (speaker != "诺亚博士") return;

        Debug.Log($"【诺亚博士台词】{content}");

        if (content.Contains("失联"))
        {
            Debug.Log("诺亚博士：台词开始后 5 秒触发 disappointed 动画");

            if (greetingCoroutine != null)
            {
                StopCoroutine(greetingCoroutine);
                greetingCoroutine = null;
            }

            if (disappointedCoroutine != null)
            {
                StopCoroutine(disappointedCoroutine);
                disappointedCoroutine = null;
            }

            hasPlayed = true;
            greetingFinished = true;

            ResetNoahEmotionBools();
            animator.SetBool("isWalking", false);
            disappointedCoroutine = StartCoroutine(PlayDisappointedAfterDelay(5.5f));

            if (dialogueEnded)
                StartFreeExplore();

            return;
        }

        if (!hasPlayed)
        {
            hasPlayed = true;
            Debug.Log("诺亚博士首次说话，触发打招呼动画");
            greetingCoroutine = StartCoroutine(PlayAnimSequence());
        }
    }

    void OnLineEnd(string speaker)
    {
        if (speaker != "诺亚博士") return;

        animator.SetBool("isDisappointed", false);
    }

    IEnumerator PlayDisappointedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
        {
            animator.SetBool("isDisappointed", true);
            Debug.Log("诺亚博士：失望动画已延迟触发");
        }
        disappointedCoroutine = null;
    }

    IEnumerator PlayAnimSequence()
    {
        Debug.Log("阶段1：打招呼");

        animator.SetBool("isDisappointed", false);
        animator.SetBool("isGreeting", true);
        yield return new WaitForSeconds(2.5f);

        // ✅ 修复：删除原来在这里手动 SetBool("isWalking", true/false) 的逻辑。
        //         isWalking 统一由 NavMeshAgentAI 管理，
        //         此处强行写入会与 NavMeshAgentAI.UpdateWalkAnimation() 发生争抢，
        //         导致诺亚巡航时走路动画被协程计时强制打断。

        animator.SetBool("isGreeting", false);

        greetingFinished = true;
        greetingCoroutine = null;

        if (dialogueEnded)
            StartFreeExplore();
    }

    void OnDialogueEnd()
    {
        // DialogueSystem 自己已经会输出“对话结束”。
        // 这里不再重复输出“对话已结束/等待动画完成”等日志。
        dialogueEnded = true;

        // 主对话列表最后一句播放完成后，直接进入自由探索准备。
        // 如果此时诺亚打招呼动画还在播放，就等动画协程结束后再进入。
        if (greetingCoroutine != null && !greetingFinished)
            return;

        greetingFinished = true;
        StartFreeExplore();
    }

    void StartFreeExplore()
    {
        if (isExploring)
        {
            Debug.LogWarning("⚠️ 自由探索已经开始，忽略重复启动");
            return;
        }

        Debug.Log("所有对话结束且打招呼完成，进入自由探索阶段");

        if (freeExploreCoroutine != null)
            StopCoroutine(freeExploreCoroutine);

        freeExploreCoroutine = StartCoroutine(FreeExploreRoutine());
    }

    IEnumerator FreeExploreRoutine()
    {
        isExploring = true;
        exploreStartTime = Time.time;
        idleTime = 0f;
        hasTriggeredCruise = false;

        patrol.StopPatrol();

        ResetNoahEmotionBools();
        animator.SetBool("isWalking", false);

        Debug.Log($"自由探索计时开始。当前模式：{(useControllerInput ? "VR手柄模式" : "电脑键盘模式")}");

        RecordCurrentXRPoses();

        while (isExploring && !hasTriggeredCruise)
        {
            float totalExploreTime = Time.time - exploreStartTime;
            bool playerActive = IsPlayerActive();

            if (playerActive)
                idleTime = 0f;
            else
                idleTime += Time.deltaTime;

            if (showExploreDebugLog && Time.frameCount % 60 == 0)
                Debug.Log($"自由探索中 - 空闲时间: {idleTime:F1}s / 总时间: {totalExploreTime:F1}s / 玩家活动: {playerActive}");

            if (idleTime >= idleThreshold)
            {
                Debug.Log($"空闲时间达到 {idleThreshold}s，触发提示机制");
                TriggerCruise();
                break;
            }

            if (totalExploreTime >= maxExploreTime)
            {
                Debug.Log($"自由探索总时间达到 {maxExploreTime}s，强制触发提示机制");
                TriggerCruise();
                break;
            }

            yield return null;
        }
    }

    bool IsPlayerActive()
    {
        if (useControllerInput)
            return HasControllerInput();

        return detectKeyboardInput && HasKeyboardInput();
    }

    bool HasKeyboardInput()
    {
        if (Keyboard.current == null) return false;

        return Keyboard.current.wKey.isPressed ||
               Keyboard.current.aKey.isPressed ||
               Keyboard.current.sKey.isPressed ||
               Keyboard.current.dKey.isPressed ||
               Keyboard.current.upArrowKey.isPressed ||
               Keyboard.current.downArrowKey.isPressed ||
               Keyboard.current.leftArrowKey.isPressed ||
               Keyboard.current.rightArrowKey.isPressed;
    }

    bool HasControllerInput()
    {
        bool active = false;

        if (detectHeadMovement)
        {
            if (CheckDeviceMovedOrRotated(XRNode.Head, ref lastHeadPosition, ref lastHeadRotation, "头显"))
                active = true;
        }

        if (detectHandMovement)
        {
            if (CheckDeviceMovedOrRotated(XRNode.LeftHand, ref lastLeftHandPosition, ref lastLeftHandRotation, "左手柄"))
                active = true;

            if (CheckDeviceMovedOrRotated(XRNode.RightHand, ref lastRightHandPosition, ref lastRightHandRotation, "右手柄"))
                active = true;
        }

        return active;
    }

    void RecordCurrentXRPoses()
    {
        TryRecordDevicePose(XRNode.Head, ref lastHeadPosition, ref lastHeadRotation);
        TryRecordDevicePose(XRNode.LeftHand, ref lastLeftHandPosition, ref lastLeftHandRotation);
        TryRecordDevicePose(XRNode.RightHand, ref lastRightHandPosition, ref lastRightHandRotation);
        hasLastXRPoses = true;
    }

    bool TryRecordDevicePose(XRNode node, ref Vector3 position, ref Quaternion rotation)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return false;

        bool hasPosition = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 currentPosition);
        bool hasRotation = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion currentRotation);

        if (hasPosition) position = currentPosition;
        if (hasRotation) rotation = currentRotation;

        return hasPosition || hasRotation;
    }

    bool CheckDeviceMovedOrRotated(
        XRNode node,
        ref Vector3 lastPosition,
        ref Quaternion lastRotation,
        string deviceName)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return false;

        bool hasPosition = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 currentPosition);
        bool hasRotation = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion currentRotation);

        if (!hasPosition && !hasRotation) return false;

        if (!hasLastXRPoses)
        {
            if (hasPosition) lastPosition = currentPosition;
            if (hasRotation) lastRotation = currentRotation;
            return false;
        }

        bool moved = false;
        bool rotated = false;

        if (hasPosition)
        {
            float distance = Vector3.Distance(currentPosition, lastPosition);
            moved = distance >= positionMoveThreshold;
            lastPosition = currentPosition;
        }

        if (hasRotation)
        {
            float angle = Quaternion.Angle(currentRotation, lastRotation);
            rotated = angle >= rotationMoveThreshold;
            lastRotation = currentRotation;
        }

        if (showExploreDebugLog && (moved || rotated))
            Debug.Log($"{deviceName}活动：移动={moved}，转动={rotated}");

        return moved || rotated;
    }

    void TriggerCruise()
    {
        if (hasTriggeredCruise) return;

        hasTriggeredCruise = true;
        isExploring = false;

        PlayCruisePromptThenStartPatrol();
    }

    void PlayCruisePromptThenStartPatrol()
    {
        if (dialogueSystemForPrompt == null)
        {
            Debug.LogWarning("⚠️ 提示用 DialogueSystem 未赋值，无法显示提示对话框，直接开始巡航。");
            StartPatrolAfterPrompt();
            return;
        }

        if (promptDialogueLines == null || promptDialogueLines.Count == 0)
        {
            Debug.LogWarning("⚠️ 巡航前提示文案列表为空，直接开始巡航。");
            StartPatrolAfterPrompt();
            return;
        }

        if (cruisePromptCoroutine != null)
        {
            StopCoroutine(cruisePromptCoroutine);
            cruisePromptCoroutine = null;
        }

        cruisePromptCoroutine = StartCoroutine(PlayCruisePromptRoutine());
    }

    IEnumerator PlayCruisePromptRoutine()
    {
        if (promptAudioSource == null && dialogueSystemForPrompt != null)
        {
            promptAudioSource = dialogueSystemForPrompt.GetComponent<AudioSource>();

            if (promptAudioSource == null)
                promptAudioSource = dialogueSystemForPrompt.gameObject.AddComponent<AudioSource>();
        }

        for (int i = 0; i < promptDialogueLines.Count; i++)
        {
            CruisePromptDialogueLine line = promptDialogueLines[i];

            if (line == null)
                continue;

            float duration;

            if (line.clip != null)
            {
                duration = line.clip.length + Mathf.Max(0f, audioLineDelay);

                if (promptAudioSource != null)
                {
                    promptAudioSource.Stop();
                    promptAudioSource.clip = line.clip;
                    promptAudioSource.Play();
                }

            }
            else
            {
                duration = Mathf.Max(0.1f, textModeDelay);

            }

            dialogueSystemForPrompt.ShowTemporaryLine(
                line.speaker,
                line.content,
                duration
            );

            yield return new WaitForSeconds(duration);
        }

        cruisePromptCoroutine = null;

        StartPatrolAfterPrompt();
    }

    void StartPatrolAfterPrompt()
    {
        Debug.Log("诺亚开始巡航！");

        GameEvents.TriggerPandaCruiseStarted();

        ResetNoahEmotionBools();

        if (patrol != null)
            patrol.BeginPatrol();
        else
            Debug.LogWarning("⚠️ 当前物体上没有 NavMeshAgentAI，无法开始巡航。请确认这个脚本挂在诺亚/巡航角色物体上。");
    }

    void OnDestroy()
    {
        if (cruisePromptCoroutine != null)
        {
            StopCoroutine(cruisePromptCoroutine);
            cruisePromptCoroutine = null;
        }

        if (dialogueSystem != null)
        {
            dialogueSystem.OnLineStartWithContent -= OnLineStartWithContent;
            dialogueSystem.OnLineEnd -= OnLineEnd;
            dialogueSystem.OnDialogueEnd -= OnDialogueEnd;
        }
    }
}

[System.Serializable]
public class CruisePromptDialogueLine
{
    public string speaker;

    public AudioClip clip;   // 有音频就拖，没有就留空

    [TextArea]
    public string content;
}