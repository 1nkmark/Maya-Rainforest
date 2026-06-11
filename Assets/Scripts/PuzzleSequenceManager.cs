using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class PuzzleSequenceManager : MonoBehaviour
{
    private enum FlowState
    {
        WaitingForBalls,
        FirstTransition,
        WaitingForCalendarPuzzle,
        SecondTransition,
        Finished
    }

    public enum InitialStep
    {
        BallPuzzle,
        CalendarPuzzle
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    public enum CalendarSolveSource
    {
        InternalAngleCheck,
        ExistingCalendarPuzzleManager,
        IndicatorGreen,
        Any
    }

    [Header("流程入口")]
    [Tooltip("正式流程选 BallPuzzle；如果只测试转盘关卡，选 CalendarPuzzle。")]
    public InitialStep initialStep = InitialStep.BallPuzzle;

    [Header("第一关：三个平台 Socket")]
    public XRSocketInteractor[] platformSockets;
    public bool verifyColorID = true;
    public float socketSolvedStableTime = 0.35f;

    [Header("第一关通过后：下降对象")]
    public Transform orbitManager;
    public bool disableOrbitBillboardingBeforeMove = true;
    public Vector3 orbitManagerTargetPosition;

    public Transform[] platforms;
    public Vector3[] platformTargetPositions;

    [Header("第一关通过后：转盘整体移入")]
    public Transform dialRoot;
    public bool moveDialRootOnlyOnX = true;
    public float dialRootTargetX;
    public Vector3 dialRootTargetPosition;

    [Header("坐标模式")]
    [Tooltip("关闭时使用世界坐标 position；开启时使用 localPosition。")]
    public bool useLocalPosition = false;

    [Header("第一段转场参数")]
    public float firstTransitionDuration = 3f;
    public AudioClip firstTransitionAudio;

    [Header("第二关：识别来源")]
    [Tooltip("推荐选 Any。它会兼容你原来的 CalendarPuzzleManager，同时保留内部角度检测。")]
    public CalendarSolveSource calendarSolveSource = CalendarSolveSource.Any;

    [Tooltip("把你原来场景里的 CalendarPuzzleManager 拖到这里。")]
    public CalendarPuzzleManager existingCalendarPuzzleManager;

    [Tooltip("如果没有拖 Existing Calendar Puzzle Manager，也可以单独拖它控制的绿色指示球。")]
    public MeshRenderer calendarIndicatorSphere;

    [Header("第二关：四个转盘引用")]
    public Transform leftOuterDial;
    public Transform leftInnerDial;
    public Transform rightOuterDial;
    public Transform rightInnerDial;

    [Header("第二关：目标角度")]
    public float targetLeftOuter = 90f;
    public float targetLeftInner = 180f;
    public float targetRightOuter = 45f;
    public float targetRightInner = 0f;

    [Header("第二关：角度判定")]
    [Tooltip("你的当前 CalendarPuzzleManager 读的是 localEulerAngles.x，所以这里默认 X。")]
    public RotationAxis dialRotationAxis = RotationAxis.X;

    public float angleTolerance = 10f;
    public float dialSolvedStableTime = 0.35f;

    [Header("第二关：锁盘")]
    [Tooltip("转盘进入正确角度后，自动吸附到目标角度并锁住，不再允许继续旋转。")]
    public bool lockSolvedDials = true;

    [Tooltip("锁住时是否把角度吸附到精确目标值。")]
    public bool snapLockedDialsToTargetAngle = true;

    [Header("第二关通过后：三面墙下降")]
    public Transform[] walls;
    public Vector3[] wallTargetPositions;

    [Header("第二关通过后：天花板消失")]
    [Tooltip("转盘过关后需要直接隐藏的天花板物体。会 SetActive(false)。")]
    public GameObject[] ceilingsToHide;

    [Tooltip("勾选：转盘过关瞬间天花板消失；不勾选：三面墙下降结束后天花板消失。")]
    public bool hideCeilingsAtSecondTransitionStart = true;

    [Header("第二段转场参数")]
    public float secondTransitionDuration = 3f;
    public AudioClip secondTransitionAudio;

    [Header("移动曲线")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("视角震动")]
    [Tooltip("建议拖 XR Origin 或 Main Camera 的父物体，不建议直接拖 Main Camera。")]
    public Transform shakeTarget;

    public bool shakeDuringFirstTransition = true;
    public bool shakeDuringSecondTransition = true;
    public float shakePositionAmplitude = 0.035f;
    public float shakeRotationAmplitude = 1.2f;
    public float shakeFrequency = 22f;

    [Header("音效")]
    public AudioSource audioSource;
    public bool loopTransitionAudio = true;

    [Header("流程事件")]
    public UnityEvent onBallPuzzleSolved;
    public UnityEvent onFirstTransitionFinished;
    public UnityEvent onCalendarPuzzleSolved;
    public UnityEvent onGameFinished;

    [Header("调试")]
    public bool printCalendarDebug = false;

    private FlowState state;
    private float socketStableTimer = 0f;
    private float dialStableTimer = 0f;
    private bool isSecondTransitionRunning = false;

    private Vector3 shakeBaseLocalPosition;
    private Quaternion shakeBaseLocalRotation;

    private bool hasCachedSolvedField = false;
    private FieldInfo calendarSolvedField;
    private bool leftOuterDialLocked = false;
    private bool leftInnerDialLocked = false;
    private bool rightInnerDialLocked = false;

    private void Start()
    {
        ResetManagedDialLocks();

        state = initialStep == InitialStep.CalendarPuzzle
            ? FlowState.WaitingForCalendarPuzzle
            : FlowState.WaitingForBalls;
    }

    private void Update()
    {
        switch (state)
        {
            case FlowState.WaitingForBalls:
                UpdateBallPuzzleCheck();
                break;

            case FlowState.WaitingForCalendarPuzzle:
                UpdateCalendarPuzzleCheck();
                break;
        }
    }

    private void UpdateBallPuzzleCheck()
    {
        if (AreAllPlatformsCorrect())
        {
            socketStableTimer += Time.deltaTime;

            if (socketStableTimer >= socketSolvedStableTime)
            {
                StartCoroutine(RunFirstTransition());
            }
        }
        else
        {
            socketStableTimer = 0f;
        }
    }

    private void UpdateCalendarPuzzleCheck()
    {
        TryLockSolvedDials();

        if (IsCalendarPuzzleSolved())
        {
            dialStableTimer += Time.deltaTime;

            if (dialStableTimer >= dialSolvedStableTime)
            {
                StartCoroutine(RunSecondTransition());
            }
        }
        else
        {
            dialStableTimer = 0f;
        }
    }

    private bool AreAllPlatformsCorrect()
    {
        if (platformSockets == null || platformSockets.Length == 0)
            return false;

        foreach (XRSocketInteractor socket in platformSockets)
        {
            if (socket == null)
                return false;

            if (!socket.hasSelection)
                return false;

            IXRSelectInteractable selected = socket.GetOldestInteractableSelected();

            if (selected == null)
                return false;

            if (verifyColorID)
            {
                ColorID socketColor = FindColorID(socket.transform);
                ColorID ballColor = FindColorID(selected.transform);

                if (socketColor == null || ballColor == null)
                    return false;

                if (socketColor.objectColor != ballColor.objectColor)
                    return false;
            }
        }

        return true;
    }

    private ColorID FindColorID(Transform target)
    {
        if (target == null)
            return null;

        ColorID id = target.GetComponent<ColorID>();

        if (id != null)
            return id;

        id = target.GetComponentInParent<ColorID>();

        if (id != null)
            return id;

        return target.GetComponentInChildren<ColorID>();
    }

    private IEnumerator RunFirstTransition()
    {
        state = FlowState.FirstTransition;
        onBallPuzzleSolved?.Invoke();

        if (disableOrbitBillboardingBeforeMove && orbitManager != null)
        {
            MayaOrbitBillboarding billboarding = orbitManager.GetComponent<MayaOrbitBillboarding>();

            if (billboarding != null)
                billboarding.enabled = false;
        }

        List<Transform> moveObjects = new List<Transform>();
        List<Vector3> targetPositions = new List<Vector3>();

        AddMoveTarget(moveObjects, targetPositions, orbitManager, orbitManagerTargetPosition);

        int platformCount = Mathf.Min(
            platforms == null ? 0 : platforms.Length,
            platformTargetPositions == null ? 0 : platformTargetPositions.Length
        );

        for (int i = 0; i < platformCount; i++)
        {
            AddMoveTarget(moveObjects, targetPositions, platforms[i], platformTargetPositions[i]);
        }

        if (dialRoot != null)
        {
            Vector3 currentDialPosition = GetPosition(dialRoot);

            Vector3 targetDialPosition = moveDialRootOnlyOnX
                ? new Vector3(dialRootTargetX, currentDialPosition.y, currentDialPosition.z)
                : dialRootTargetPosition;

            AddMoveTarget(moveObjects, targetPositions, dialRoot, targetDialPosition);
        }

        yield return MoveObjects(
            moveObjects,
            targetPositions,
            firstTransitionDuration,
            shakeDuringFirstTransition,
            firstTransitionAudio
        );

        onFirstTransitionFinished?.Invoke();
        state = FlowState.WaitingForCalendarPuzzle;
    }

    private bool IsCalendarPuzzleSolved()
    {
        bool internalSolved = false;
        bool existingManagerSolved = false;
        bool indicatorSolved = false;

        if (calendarSolveSource == CalendarSolveSource.InternalAngleCheck ||
            calendarSolveSource == CalendarSolveSource.Any)
        {
            internalSolved = AreAllDialsCorrect();
        }

        if (calendarSolveSource == CalendarSolveSource.ExistingCalendarPuzzleManager ||
            calendarSolveSource == CalendarSolveSource.Any)
        {
            existingManagerSolved = IsExistingCalendarPuzzleManagerSolved();
        }

        if (calendarSolveSource == CalendarSolveSource.IndicatorGreen ||
            calendarSolveSource == CalendarSolveSource.Any)
        {
            indicatorSolved = IsCalendarIndicatorGreen();
        }

        if (printCalendarDebug)
        {
            Debug.Log(
                $"[PuzzleSequenceManager] CalendarSolved " +
                $"Internal={internalSolved}, ExistingManager={existingManagerSolved}, Indicator={indicatorSolved}, State={state}"
            );
        }

        return internalSolved || existingManagerSolved || indicatorSolved;
    }

    private bool IsExistingCalendarPuzzleManagerSolved()
    {
        if (existingCalendarPuzzleManager == null)
            return false;

        if (!hasCachedSolvedField)
        {
            calendarSolvedField = typeof(CalendarPuzzleManager).GetField(
                "isSolved",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            hasCachedSolvedField = true;
        }

        if (calendarSolvedField != null && calendarSolvedField.FieldType == typeof(bool))
        {
            bool solved = (bool)calendarSolvedField.GetValue(existingCalendarPuzzleManager);

            if (solved)
                return true;
        }

        if (existingCalendarPuzzleManager.indicatorSphere != null)
        {
            return IsGreen(existingCalendarPuzzleManager.indicatorSphere.material.color);
        }

        return false;
    }

    private bool IsCalendarIndicatorGreen()
    {
        if (calendarIndicatorSphere != null)
        {
            return IsGreen(calendarIndicatorSphere.material.color);
        }

        if (existingCalendarPuzzleManager != null && existingCalendarPuzzleManager.indicatorSphere != null)
        {
            return IsGreen(existingCalendarPuzzleManager.indicatorSphere.material.color);
        }

        return false;
    }

    private bool IsGreen(Color color)
    {
        return color.g > 0.6f && color.r < 0.45f && color.b < 0.45f;
    }

    private bool AreAllDialsCorrect()
    {
        bool result =
            CheckDial(leftOuterDial, targetLeftOuter) &&
            CheckDial(leftInnerDial, targetLeftInner) &&
            CheckDial(rightInnerDial, targetRightInner);

        if (printCalendarDebug)
        {
            Debug.Log(
                $"[PuzzleSequenceManager] DialAngles " +
                $"LO={GetDialAngleSafe(leftOuterDial):F1}/{targetLeftOuter}, " +
                $"LI={GetDialAngleSafe(leftInnerDial):F1}/{targetLeftInner}, " +
                $"RO(ignored)={GetDialAngleSafe(rightOuterDial):F1}/{targetRightOuter}, " +
                $"RI={GetDialAngleSafe(rightInnerDial):F1}/{targetRightInner}, " +
                $"Axis={dialRotationAxis}, Result={result}"
            );
        }

        return result;
    }

    private void TryLockSolvedDials()
    {
        if (!lockSolvedDials)
            return;

        TryLockDial(leftOuterDial, targetLeftOuter, ref leftOuterDialLocked, "LeftOuter");
        TryLockDial(leftInnerDial, targetLeftInner, ref leftInnerDialLocked, "LeftInner");
        TryLockDial(rightInnerDial, targetRightInner, ref rightInnerDialLocked, "RightInner");
    }

    private void TryLockDial(Transform dial, float targetAngle, ref bool dialLocked, string debugName)
    {
        if (dialLocked || dial == null)
            return;

        if (!CheckDial(dial, targetAngle))
            return;

        if (snapLockedDialsToTargetAngle)
        {
            SetDialAngle(dial, targetAngle);
        }

        UniversalRayDial rayDial = dial.GetComponent<UniversalRayDial>();

        if (rayDial != null)
        {
            rayDial.SetLocked(true);
        }
        else
        {
            XRSimpleInteractable interactable = dial.GetComponent<XRSimpleInteractable>();

            if (interactable != null)
            {
                interactable.enabled = false;
            }
        }

        dialLocked = true;

        if (printCalendarDebug)
        {
            Debug.Log($"[PuzzleSequenceManager] Locked dial {debugName} at {targetAngle:F1}");
        }
    }

    private bool CheckDial(Transform dial, float targetAngle)
    {
        if (dial == null)
            return false;

        float currentAngle = GetDialAngle(dial);
        float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        return difference <= angleTolerance;
    }

    private void SetDialAngle(Transform dial, float targetAngle)
    {
        if (dial == null)
            return;

        Vector3 euler = dial.localEulerAngles;
        float normalizedTarget = Mathf.Repeat(targetAngle, 360f);

        switch (dialRotationAxis)
        {
            case RotationAxis.X:
                euler.x = normalizedTarget;
                break;

            case RotationAxis.Y:
                euler.y = normalizedTarget;
                break;

            case RotationAxis.Z:
                euler.z = normalizedTarget;
                break;
        }

        dial.localEulerAngles = euler;
    }

    private float GetDialAngleSafe(Transform dial)
    {
        if (dial == null)
            return -999f;

        return GetDialAngle(dial);
    }

    private float GetDialAngle(Transform dial)
    {
        Vector3 euler = dial.localEulerAngles;

        switch (dialRotationAxis)
        {
            case RotationAxis.X:
                return euler.x;

            case RotationAxis.Y:
                return euler.y;

            case RotationAxis.Z:
                return euler.z;

            default:
                return euler.x;
        }
    }

    private void ResetManagedDialLocks()
    {
        leftOuterDialLocked = false;
        leftInnerDialLocked = false;
        rightInnerDialLocked = false;

        ResetDialLock(leftOuterDial);
        ResetDialLock(leftInnerDial);
        ResetDialLock(rightInnerDial);
    }

    private void ResetDialLock(Transform dial)
    {
        if (dial == null)
            return;

        UniversalRayDial rayDial = dial.GetComponent<UniversalRayDial>();

        if (rayDial != null)
        {
            rayDial.SetLocked(false);
            return;
        }

        XRSimpleInteractable interactable = dial.GetComponent<XRSimpleInteractable>();

        if (interactable != null)
        {
            interactable.enabled = true;
        }
    }

    private IEnumerator RunSecondTransition()
    {
        if (isSecondTransitionRunning || state == FlowState.SecondTransition || state == FlowState.Finished)
            yield break;

        isSecondTransitionRunning = true;
        state = FlowState.SecondTransition;
        onCalendarPuzzleSolved?.Invoke();

        if (hideCeilingsAtSecondTransitionStart)
        {
            HideCeilings();
        }

        List<Transform> moveObjects = new List<Transform>();
        List<Vector3> targetPositions = new List<Vector3>();

        int wallCount = Mathf.Min(
            walls == null ? 0 : walls.Length,
            wallTargetPositions == null ? 0 : wallTargetPositions.Length
        );

        for (int i = 0; i < wallCount; i++)
        {
            AddMoveTarget(moveObjects, targetPositions, walls[i], wallTargetPositions[i]);
        }

        yield return MoveObjects(
            moveObjects,
            targetPositions,
            secondTransitionDuration,
            shakeDuringSecondTransition,
            secondTransitionAudio
        );

        if (!hideCeilingsAtSecondTransitionStart)
        {
            HideCeilings();
        }

        onGameFinished?.Invoke();
        state = FlowState.Finished;
        isSecondTransitionRunning = false;
    }

    private void HideCeilings()
    {
        if (ceilingsToHide == null)
            return;

        foreach (GameObject ceiling in ceilingsToHide)
        {
            if (ceiling == null)
                continue;

            ceiling.SetActive(false);
        }
    }

    private void AddMoveTarget(List<Transform> objects, List<Vector3> targets, Transform obj, Vector3 targetPosition)
    {
        if (obj == null)
            return;

        objects.Add(obj);
        targets.Add(targetPosition);
    }

    private IEnumerator MoveObjects(
        List<Transform> objects,
        List<Vector3> targetPositions,
        float duration,
        bool enableShake,
        AudioClip transitionAudio
    )
    {
        Vector3[] startPositions = new Vector3[objects.Count];

        for (int i = 0; i < objects.Count; i++)
        {
            startPositions[i] = GetPosition(objects[i]);
        }

        StartTransitionAudio(transitionAudio);

        if (enableShake)
            CaptureShakeBaseTransform();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float curveT = moveCurve != null ? moveCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] == null)
                    continue;

                Vector3 nextPosition = Vector3.LerpUnclamped(startPositions[i], targetPositions[i], curveT);
                SetPosition(objects[i], nextPosition);
            }

            if (enableShake)
                ApplyShake();

            yield return null;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null)
                continue;

            SetPosition(objects[i], targetPositions[i]);
        }

        if (enableShake)
            RestoreShakeBaseTransform();

        StopTransitionAudio();
    }

    private Vector3 GetPosition(Transform target)
    {
        return useLocalPosition ? target.localPosition : target.position;
    }

    private void SetPosition(Transform target, Vector3 position)
    {
        if (useLocalPosition)
            target.localPosition = position;
        else
            target.position = position;
    }

    private void StartTransitionAudio(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.clip = clip;
        audioSource.loop = loopTransitionAudio;
        audioSource.Play();
    }

    private void StopTransitionAudio()
    {
        if (audioSource == null)
            return;

        if (loopTransitionAudio)
            audioSource.Stop();

        audioSource.loop = false;
    }

    private void CaptureShakeBaseTransform()
    {
        if (shakeTarget == null)
            return;

        shakeBaseLocalPosition = shakeTarget.localPosition;
        shakeBaseLocalRotation = shakeTarget.localRotation;
    }

    private void ApplyShake()
    {
        if (shakeTarget == null)
            return;

        float t = Time.time * shakeFrequency;

        float px = (Mathf.PerlinNoise(t, 0.11f) - 0.5f) * 2f;
        float py = (Mathf.PerlinNoise(0.23f, t) - 0.5f) * 2f;
        float pz = (Mathf.PerlinNoise(t, 0.47f) - 0.5f) * 2f;

        Vector3 positionOffset = new Vector3(px, py, pz) * shakePositionAmplitude;

        float rx = (Mathf.PerlinNoise(t, 1.31f) - 0.5f) * 2f;
        float ry = (Mathf.PerlinNoise(1.73f, t) - 0.5f) * 2f;
        float rz = (Mathf.PerlinNoise(t, 2.17f) - 0.5f) * 2f;

        Vector3 rotationOffset = new Vector3(rx, ry, rz) * shakeRotationAmplitude;

        shakeTarget.localPosition = shakeBaseLocalPosition + positionOffset;
        shakeTarget.localRotation = shakeBaseLocalRotation * Quaternion.Euler(rotationOffset);
    }

    private void RestoreShakeBaseTransform()
    {
        if (shakeTarget == null)
            return;

        shakeTarget.localPosition = shakeBaseLocalPosition;
        shakeTarget.localRotation = shakeBaseLocalRotation;
    }

    [ContextMenu("Force Complete Ball Puzzle")]
    public void ForceCompleteBallPuzzle()
    {
        if (state == FlowState.FirstTransition ||
            state == FlowState.WaitingForCalendarPuzzle ||
            state == FlowState.SecondTransition ||
            state == FlowState.Finished)
            return;

        socketStableTimer = 0f;
        StartCoroutine(RunFirstTransition());
    }

    [ContextMenu("Force Complete Calendar Puzzle")]
    public void ForceCompleteCalendarPuzzle()
    {
        if (isSecondTransitionRunning || state == FlowState.SecondTransition || state == FlowState.Finished)
            return;

        dialStableTimer = 0f;
        StartCoroutine(RunSecondTransition());
    }

    [ContextMenu("Start Waiting For Calendar Puzzle")]
    public void StartWaitingForCalendarPuzzle()
    {
        if (state == FlowState.FirstTransition || state == FlowState.SecondTransition || state == FlowState.Finished)
            return;

        state = FlowState.WaitingForCalendarPuzzle;
    }
}
