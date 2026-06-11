using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class MayaPainter : MonoBehaviour
{
    public enum MayaSymbol
    {
        Dot,
        Bar,
        Circle
    }

    public enum InputZone
    {
        MainZero,
        Carry,
        Invalid
    }

    public enum PuzzleStage
    {
        Input9,
        Input13,
        Input20,
        Completed
    }

    [Header("核心引用")]
    public XRRayInteractor rayInteractor;
    public GameObject strokePrefab;
    public LayerMask drawingLayer;
    public InputActionProperty drawAction;
    public InputActionProperty clearAction;

    [Header("输入区域 Collider")]
    public Collider mainZeroZoneCollider;
    public Collider carryZoneCollider;

    [Header("区域边框发光")]
    public Renderer mainZeroZoneBorder;
    public Renderer carryZoneBorder;
    public Color normalBorderColor = Color.red;
    public Color activeBorderColor = Color.yellow;
    public Color successBorderColor = Color.green;
    public float successBorderShowTime = 0.6f;

    [Header("区域边框 Emission 设置")]
    public bool useEmissionForZoneBorder = true;
    public float normalEmissionIntensity = 0.8f;
    public float activeEmissionIntensity = 2.5f;
    public float successEmissionIntensity = 3.5f;

    [Header("物理反馈物体")]
    public MeshRenderer sphereIndicator;
    public MeshRenderer cubeIndicator;

    [Header("识别参数")]
    public float dotThreshold = 0.09f;
    public float barDistanceThreshold = 0.10f;
    public float circleMinSpan = 0.08f;
    public float circleCloseThreshold = 0.15f;
    public float pointSampleDistance = 0.008f;

    [Header("视角震动反馈")]
    public Transform xrOriginTransform;
    public float shakeDuration = 0.6f;
    public float shakeStrength = 0.035f;
    public float shakeFrequency = 35f;

    [Header("阶段成功音效")]
    public AudioSource stageAudioSource;
    public AudioClip input9SuccessClip;
    public AudioClip input13SuccessClip;
    public AudioClip input20SuccessClip;

    [Header("金字塔反馈")]
    public PyramidGlowPulse pyramidGlowPulse;

    [Header("最终机关流程")]
    public MayaFinalSequenceController finalSequenceController;

    [Header("调试")]
    public bool logHitInfo = false;

    private LineRenderer currentLine;
    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<GameObject> allStrokes = new List<GameObject>();

    private bool isDrawing = false;
    private InputZone currentDrawingZone = InputZone.Invalid;

    private PuzzleStage currentStage = PuzzleStage.Input9;
    private readonly List<MayaSymbol> currentInput = new List<MayaSymbol>();

    private bool sphereIsGreen = false;
    private bool isShaking = false;
    private bool isShowingSuccessBorder = false;
    private bool isStageTransitioning = false;

    // 超时/重置后，如果玩家还按着绘制键，先忽略绘制，直到玩家松开一次。
    // 否则旧笔画可能在松手瞬间继续被识别，甚至只剩音效残留。
    private bool suppressDrawingUntilDrawReleased = false;

    private Material[] mainZeroBorderMaterials;
    private Material[] carryBorderMaterials;

    void Start()
    {
        CacheBorderMaterials();

        ResetBorderColors();

        if (pyramidGlowPulse != null)
        {
            pyramidGlowPulse.StopGlow();
        }
    }

    void Update()
    {
        HandleClearInput();

        if (currentStage == PuzzleStage.Completed || isStageTransitioning)
            return;

        HandleDrawingRay();
    }

    void HandleClearInput()
    {
        if (!clearAction.action.WasPressedThisFrame())
            return;

        ClearAllStrokes();
        currentInput.Clear();

        StopAllCoroutines();

        isDrawing = false;
        currentLine = null;
        currentDrawingZone = InputZone.Invalid;

        isShaking = false;
        isShowingSuccessBorder = false;

        ResetBorderColors();

        /*
         * 注意：
         * 这里不要调用 pyramidGlowPulse.StopGlow()。
         * 13 成功后金字塔应该一直闪烁，直到 20 成功后才停止。
         */
    }

    void HandleDrawingRay()
    {
        if (rayInteractor == null)
            return;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            float triggerValue = drawAction.action.ReadValue<float>();

            if (suppressDrawingUntilDrawReleased)
            {
                if (triggerValue <= 0.1f)
                {
                    suppressDrawingUntilDrawReleased = false;
                }
                else
                {
                    // 仍然按着绘制键时，不允许继续生成/结束旧笔画。
                    return;
                }
            }

            InputZone hitZone = GetInputZone(hit.collider);

            /*
             * 原来的逻辑依赖 drawingLayer：
             * bool isHittingDrawingLayer = ((1 << hit.collider.gameObject.layer) & drawingLayer) != 0;
             *
             * 现在改为：
             * 只要 hit.collider 精确等于 mainZeroZoneCollider 或 carryZoneCollider，就允许响应。
             * 这样可以避免 LayerMask 配错导致边框完全不变色。
             */
            bool isValidDrawingZone = hitZone != InputZone.Invalid;

            if (logHitInfo)
            {
                Debug.Log($"[MayaPainter] Hit: {hit.collider.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Zone: {hitZone}");
            }

            if (!isDrawing && !isShowingSuccessBorder)
            {
                ResetBorderColors();

                if (isValidDrawingZone)
                    SetZoneBorderColor(hitZone, activeBorderColor, activeEmissionIntensity);
            }

            if (isValidDrawingZone && triggerValue > 0.5f)
            {
                if (!isDrawing)
                    StartDrawing(hit.point, hit.normal, hitZone);
                else
                    UpdateDrawing(hit.point, hit.normal);
            }
            else if (isDrawing)
            {
                EndDrawing();
            }
        }
        else
        {
            if (!isDrawing && !isShowingSuccessBorder)
                ResetBorderColors();

            if (isDrawing)
                EndDrawing();
        }
    }

    void StartDrawing(Vector3 hitPoint, Vector3 normal, InputZone zone)
    {
        isDrawing = true;
        currentDrawingZone = zone;

        GameObject newStroke = Instantiate(strokePrefab);
        allStrokes.Add(newStroke);

        currentLine = newStroke.GetComponent<LineRenderer>();
        points.Clear();

        AddPoint(hitPoint + normal * 0.02f);

        if (!isShowingSuccessBorder)
            SetZoneBorderColor(zone, activeBorderColor, activeEmissionIntensity);
    }

    void UpdateDrawing(Vector3 hitPoint, Vector3 normal)
    {
        Vector3 currentPos = hitPoint + normal * 0.02f;

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], currentPos) > pointSampleDistance)
            AddPoint(currentPos);
    }

    void AddPoint(Vector3 position)
    {
        points.Add(position);

        if (currentLine == null)
            return;

        currentLine.positionCount = points.Count;
        currentLine.SetPosition(points.Count - 1, position);
    }

    void EndDrawing()
    {
        isDrawing = false;

        if (TryRecognizeGesture(out MayaSymbol symbol))
        {
            HandleRecognizedSymbol(symbol, currentDrawingZone);
        }
        else if (currentStage == PuzzleStage.Input20 && currentDrawingZone == InputZone.MainZero)
        {
            if (HasEnoughStrokeForZero())
                HandleRecognizedSymbol(MayaSymbol.Circle, currentDrawingZone);
        }

        currentLine = null;
        currentDrawingZone = InputZone.Invalid;

        if (!isShowingSuccessBorder)
            ResetBorderColors();
    }

    bool TryRecognizeGesture(out MayaSymbol symbol)
    {
        symbol = MayaSymbol.Dot;

        if (points.Count < 2)
            return false;

        StrokeStats stats = CalculateStrokeStats();

        bool looksLikeDot = stats.maxSpan < dotThreshold;

        bool looksLikeCircle =
            stats.maxSpan > circleMinSpan &&
            stats.closeDistance < circleCloseThreshold &&
            stats.pathLength > stats.maxSpan * 2.2f;

        bool looksLikeBar =
            stats.startEndDistanceXZ > barDistanceThreshold &&
            stats.pathLength < stats.startEndDistanceXZ * 2.2f;

        if (looksLikeDot)
        {
            symbol = MayaSymbol.Dot;
            return true;
        }

        if (currentStage == PuzzleStage.Input20 && currentDrawingZone == InputZone.MainZero && looksLikeCircle)
        {
            symbol = MayaSymbol.Circle;
            return true;
        }

        if (looksLikeBar)
        {
            symbol = MayaSymbol.Bar;
            return true;
        }

        if (looksLikeCircle)
        {
            symbol = MayaSymbol.Circle;
            return true;
        }

        return false;
    }

    bool HasEnoughStrokeForZero()
    {
        if (points.Count < 4)
            return false;

        StrokeStats stats = CalculateStrokeStats();

        return stats.maxSpan >= circleMinSpan || stats.pathLength >= circleMinSpan * 2f;
    }

    StrokeStats CalculateStrokeStats()
    {
        Vector3 start = points[0];
        Vector3 end = points[points.Count - 1];

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        float pathLength = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];

            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);

            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);

            minZ = Mathf.Min(minZ, p.z);
            maxZ = Mathf.Max(maxZ, p.z);

            if (i > 0)
                pathLength += Vector3.Distance(points[i - 1], points[i]);
        }

        float widthX = maxX - minX;
        float heightY = maxY - minY;
        float depthZ = maxZ - minZ;

        Vector2 startXZ = new Vector2(start.x, start.z);
        Vector2 endXZ = new Vector2(end.x, end.z);

        return new StrokeStats
        {
            widthX = widthX,
            heightY = heightY,
            depthZ = depthZ,
            maxSpan = Mathf.Max(widthX, heightY, depthZ),
            pathLength = pathLength,
            closeDistance = Vector3.Distance(start, end),
            startEndDistanceXZ = Vector2.Distance(startXZ, endXZ)
        };
    }

    void HandleRecognizedSymbol(MayaSymbol symbol, InputZone zone)
    {
        Debug.Log($"识别到符号：{symbol}，输入区域：{zone}，当前阶段：{currentStage}");

        if (symbol == MayaSymbol.Dot && sphereIndicator != null)
        {
            sphereIsGreen = !sphereIsGreen;
            sphereIndicator.material.color = sphereIsGreen ? Color.green : Color.red;
        }

        if (symbol == MayaSymbol.Bar && cubeIndicator != null)
        {
            cubeIndicator.material.color = new Color(Random.value, Random.value, Random.value);
        }

        if (!IsSymbolAllowedInCurrentStage(symbol, zone))
        {
            Debug.LogWarning("输入区域或符号不符合当前阶段要求，已忽略。");
            currentInput.Clear();
            return;
        }

        currentInput.Add(symbol);

        if (CheckCurrentStageSuccess())
            OnStageSuccess();
    }

    bool IsSymbolAllowedInCurrentStage(MayaSymbol symbol, InputZone zone)
    {
        switch (currentStage)
        {
            case PuzzleStage.Input9:
            case PuzzleStage.Input13:
                return zone == InputZone.MainZero && (symbol == MayaSymbol.Dot || symbol == MayaSymbol.Bar);

            case PuzzleStage.Input20:
                if (zone == InputZone.MainZero)
                    return symbol == MayaSymbol.Circle;

                if (zone == InputZone.Carry)
                    return symbol == MayaSymbol.Dot;

                return false;
        }

        return false;
    }

    bool CheckCurrentStageSuccess()
    {
        int dotCount = 0;
        int barCount = 0;
        int circleCount = 0;

        foreach (MayaSymbol s in currentInput)
        {
            if (s == MayaSymbol.Dot) dotCount++;
            else if (s == MayaSymbol.Bar) barCount++;
            else if (s == MayaSymbol.Circle) circleCount++;
        }

        switch (currentStage)
        {
            case PuzzleStage.Input9:
                if (currentInput.Count > 5)
                {
                    currentInput.Clear();
                    return false;
                }

                return barCount == 1 && dotCount == 4;

            case PuzzleStage.Input13:
                if (currentInput.Count > 5)
                {
                    currentInput.Clear();
                    return false;
                }

                return barCount == 2 && dotCount == 3;

            case PuzzleStage.Input20:
                if (currentInput.Count > 2)
                {
                    currentInput.Clear();
                    return false;
                }

                return circleCount == 1 && dotCount == 1;
        }

        return false;
    }

    void OnStageSuccess()
    {
        Debug.Log($"阶段成功：{currentStage}");

        switch (currentStage)
        {
            case PuzzleStage.Input9:
                StartCoroutine(FlashZoneSuccess(InputZone.MainZero));
                StartCoroutine(ShakeXRWithAudio(PuzzleStage.Input9));

                // 新版文案取消数字13，玩家画对9后直接进入20阶段。
                currentStage = PuzzleStage.Input20;
                currentInput.Clear();
                break;

            case PuzzleStage.Input13:
                StartCoroutine(FlashZoneSuccess(InputZone.MainZero));

                if (pyramidGlowPulse != null)
                {
                    pyramidGlowPulse.PlayGlow();
                }

                StartCoroutine(ShakeXRWithAudio(PuzzleStage.Input13));

                currentStage = PuzzleStage.Input20;
                currentInput.Clear();
                break;

            case PuzzleStage.Input20:
                currentStage = PuzzleStage.Completed;
                currentInput.Clear();
                StartCoroutine(Input20FinalRoutine());
                break;
        }
    }

    IEnumerator Input20FinalRoutine()
    {
        isStageTransitioning = true;

        StartCoroutine(FlashZoneSuccess(InputZone.MainZero));
        StartCoroutine(FlashZoneSuccess(InputZone.Carry));

        if (pyramidGlowPulse != null)
        {
            pyramidGlowPulse.StopGlow();
        }

        ClearAllStrokes();

        yield return StartCoroutine(ShakeXRWithAudio(PuzzleStage.Input20));

        if (finalSequenceController != null)
        {
            finalSequenceController.StartFinalSequence();
        }

        isStageTransitioning = false;
    }

    IEnumerator FlashZoneSuccess(InputZone zone)
    {
        isShowingSuccessBorder = true;

        SetZoneBorderColor(zone, successBorderColor, successEmissionIntensity);

        yield return new WaitForSeconds(successBorderShowTime);

        isShowingSuccessBorder = false;
        ResetBorderColors();
    }

    IEnumerator ShakeXRWithAudio(PuzzleStage stage)
    {
        PlayStageAudio(stage);
        yield return StartCoroutine(ShakeXR());
    }

    void PlayStageAudio(PuzzleStage stage)
    {
        if (stageAudioSource == null)
            return;

        AudioClip clip = null;

        switch (stage)
        {
            case PuzzleStage.Input9:
                clip = input9SuccessClip;
                break;

            case PuzzleStage.Input13:
                clip = input13SuccessClip;
                break;

            case PuzzleStage.Input20:
                clip = input20SuccessClip;
                break;
        }

        if (clip != null)
        {
            stageAudioSource.PlayOneShot(clip);
        }
    }

    IEnumerator ShakeXR()
    {
        if (xrOriginTransform == null || isShaking)
            yield break;

        isShaking = true;

        Vector3 originalLocalPosition = xrOriginTransform.localPosition;
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            float offsetX = Mathf.Sin(timer * shakeFrequency) * shakeStrength;
            float offsetZ = Mathf.Cos(timer * shakeFrequency * 1.3f) * shakeStrength;

            xrOriginTransform.localPosition = originalLocalPosition + new Vector3(offsetX, 0f, offsetZ);

            yield return null;
        }

        xrOriginTransform.localPosition = originalLocalPosition;
        isShaking = false;
    }

    InputZone GetInputZone(Collider col)
    {
        if (col == mainZeroZoneCollider)
            return InputZone.MainZero;

        if (col == carryZoneCollider)
            return InputZone.Carry;

        return InputZone.Invalid;
    }

    void CacheBorderMaterials()
    {
        if (mainZeroZoneBorder != null)
        {
            mainZeroBorderMaterials = mainZeroZoneBorder.materials;
            EnableEmission(mainZeroBorderMaterials);
        }

        if (carryZoneBorder != null)
        {
            carryBorderMaterials = carryZoneBorder.materials;
            EnableEmission(carryBorderMaterials);
        }
    }

    void EnableEmission(Material[] materials)
    {
        if (materials == null) return;

        foreach (Material mat in materials)
        {
            if (mat == null) continue;
            mat.EnableKeyword("_EMISSION");
        }
    }

    void ResetBorderColors()
    {
        SetRendererMaterialsColor(mainZeroBorderMaterials, normalBorderColor, normalEmissionIntensity);
        SetRendererMaterialsColor(carryBorderMaterials, normalBorderColor, normalEmissionIntensity);
    }

    void SetZoneBorderColor(InputZone zone, Color color, float emissionIntensity)
    {
        if (zone == InputZone.MainZero)
        {
            SetRendererMaterialsColor(mainZeroBorderMaterials, color, emissionIntensity);
        }
        else if (zone == InputZone.Carry)
        {
            SetRendererMaterialsColor(carryBorderMaterials, color, emissionIntensity);
        }
    }

    void SetRendererMaterialsColor(Material[] materials, Color color, float emissionIntensity)
    {
        if (materials == null)
            return;

        foreach (Material mat in materials)
        {
            if (mat == null) continue;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            mat.color = color;

            if (useEmissionForZoneBorder && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emissionIntensity);
            }
        }
    }


    // =========================================================
    // 给 DialogueSystemAct2 调用的“诺亚帮写正确答案”接口
    // 作用：不需要玩家真的画完，也能播放对应阶段成功反馈，并推进阶段。
    // =========================================================
    public void ForceCompleteInput9WithFeedback()
    {
        if (isStageTransitioning)
            return;

        Debug.Log("[MayaPainter] 外部触发：数字9按成功处理，并进入 Input20。 ");

        suppressDrawingUntilDrawReleased = false;

        ClearAllStrokes();
        currentInput.Clear();
        isDrawing = false;
        currentLine = null;
        currentDrawingZone = InputZone.Invalid;

        StartCoroutine(FlashZoneSuccess(InputZone.MainZero));
        StartCoroutine(ShakeXRWithAudio(PuzzleStage.Input9));

        // 新文案取消13，所以这里直接进入20阶段，而不是进入Input13
        currentStage = PuzzleStage.Input20;
    }

    public void ForceCompleteInput20WithFeedback()
    {
        if (isStageTransitioning)
            return;

        Debug.Log("[MayaPainter] 外部触发：数字20按成功处理，并进入 Completed。 ");

        suppressDrawingUntilDrawReleased = false;

        currentStage = PuzzleStage.Completed;
        currentInput.Clear();
        isDrawing = false;
        currentLine = null;
        currentDrawingZone = InputZone.Invalid;

        StartCoroutine(Input20FinalRoutine());
    }

    public void CancelCurrentDrawingWithoutFeedback()
    {
        // 只取消当前正在画的笔画和残留输入，不播放任何成功动画/音效/震动。
        // 这个方法专门用于“限时失败/重试”场景：
        // 如果玩家在超时瞬间还按着扳机，旧版会在松手 EndDrawing 时继续识别残留笔画，
        // 从而误触发数字9成功动效。这里要把当前笔画彻底作废。
        StopAllCoroutines();

        // StopAllCoroutines 只能停协程，不能停已经 PlayOneShot 出去的成功音效。
        // 超时重试时如果刚好误触发过成功音效，这里必须手动停止 AudioSource。
        if (stageAudioSource != null)
            stageAudioSource.Stop();

        isDrawing = false;
        currentLine = null;
        currentDrawingZone = InputZone.Invalid;
        points.Clear();

        currentInput.Clear();
        ClearAllStrokes();

        isShaking = false;
        isShowingSuccessBorder = false;
        isStageTransitioning = false;

        suppressDrawingUntilDrawReleased = true;

        ResetBorderColors();
    }

    public void ForceStageInput9Only()
    {
        CancelCurrentDrawingWithoutFeedback();
        currentStage = PuzzleStage.Input9;
        Debug.Log("[MayaPainter] 外部强制阶段：Input9（无成功反馈）。 ");
    }

    public void ForceStageInput20Only()
    {
        CancelCurrentDrawingWithoutFeedback();
        currentStage = PuzzleStage.Input20;
        Debug.Log("[MayaPainter] 外部强制阶段：Input20（无成功反馈）。 ");
    }

    public void ForceStageCompletedOnly()
    {
        CancelCurrentDrawingWithoutFeedback();
        currentStage = PuzzleStage.Completed;
        Debug.Log("[MayaPainter] 外部强制阶段：Completed（无成功反馈）。 ");
    }

    public void ClearAllStrokes()
    {
        foreach (GameObject s in allStrokes)
        {
            if (s != null)
                Destroy(s);
        }

        allStrokes.Clear();
    }

    private struct StrokeStats
    {
        public float widthX;
        public float heightY;
        public float depthZ;
        public float maxSpan;
        public float pathLength;
        public float closeDistance;
        public float startEndDistanceXZ;
    }
}