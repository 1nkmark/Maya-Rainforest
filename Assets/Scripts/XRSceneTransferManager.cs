using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

public class XRSceneTransferManager : MonoBehaviour
{
    public static XRSceneTransferManager Instance;

    [Header("当前 Scene 的 XR Origin")]
    public XROrigin currentXROrigin;

    public enum PositionAlignMode
    {
        KeepPlayerWorldPosition,     // 模式 A：保留玩家跳转前世界位置
        AlignPlayerToTargetAnchor    // 模式 B：玩家跳转后对齐到目标 Scene 锚点
    }

    public enum HeightMode
    {
        KeepAnchorFloorY,     // 推荐：只对齐水平 XZ，不处理头显高度
        AlignCameraExactly    // 精确让 Camera XYZ 对齐目标点
    }

    [Header("默认跳转位置模式")]
    public PositionAlignMode defaultPositionAlignMode = PositionAlignMode.KeepPlayerWorldPosition;

    [Header("默认高度处理模式")]
    public HeightMode defaultHeightMode = HeightMode.KeepAnchorFloorY;

    [Header("等待目标 Scene 初始化帧数")]
    public int waitFramesAfterSceneLoaded = 3;

    [Header("调试日志")]
    public bool enableDebugLog = true;

    private Vector3 savedCameraWorldPosition;
    private bool hasSavedCameraWorldPosition = false;

    private float pendingYawOffset;
    private bool pendingApply = false;

    private PositionAlignMode pendingPositionAlignMode;
    private HeightMode pendingHeightMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (currentXROrigin == null)
        {
            currentXROrigin = FindObjectOfType<XROrigin>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void LoadSceneWithPhysicalAlignment(string sceneName, float customYawOffset = 0f)
    {
        LoadSceneWithPhysicalAlignment(
            sceneName,
            customYawOffset,
            defaultPositionAlignMode,
            defaultHeightMode
        );
    }

    public void LoadSceneWithPhysicalAlignment(
        string sceneName,
        float customYawOffset,
        PositionAlignMode positionAlignMode,
        HeightMode heightMode)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("XRSceneTransferManager：目标 Scene 名称为空。");
            return;
        }

        SaveCameraWorldPositionBeforeLoad();

        pendingYawOffset = customYawOffset;
        pendingPositionAlignMode = positionAlignMode;
        pendingHeightMode = heightMode;
        pendingApply = true;

        SceneManager.LoadScene(sceneName);
    }

    private void SaveCameraWorldPositionBeforeLoad()
    {
        if (currentXROrigin == null)
        {
            currentXROrigin = FindObjectOfType<XROrigin>();
        }

        if (currentXROrigin == null)
        {
            hasSavedCameraWorldPosition = false;
            Debug.LogWarning("XRSceneTransferManager：跳转前没有找到 currentXROrigin。");
            return;
        }

        Camera xrCamera = GetXRCamera(currentXROrigin);

        if (xrCamera == null)
        {
            hasSavedCameraWorldPosition = false;
            Debug.LogWarning("XRSceneTransferManager：跳转前没有找到 XR Camera。");
            return;
        }

        savedCameraWorldPosition = xrCamera.transform.position;
        hasSavedCameraWorldPosition = true;

        if (enableDebugLog)
        {
            Debug.Log(
                $"跳转前保存 Camera 世界位置：{savedCameraWorldPosition}"
            );
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pendingApply)
            return;

        StartCoroutine(ApplyAfterXRReady());
    }

    private IEnumerator ApplyAfterXRReady()
    {
        int waitFrames = Mathf.Max(1, waitFramesAfterSceneLoaded);

        for (int i = 0; i < waitFrames; i++)
        {
            yield return null;
        }

        XRSceneSpawnAnchor spawnAnchor = FindObjectOfType<XRSceneSpawnAnchor>();

        if (spawnAnchor == null)
        {
            Debug.LogError("XRSceneTransferManager：目标 Scene 中没有找到 XRSceneSpawnAnchor。");
            yield break;
        }

        XROrigin xrOrigin = spawnAnchor.xrOrigin;

        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        if (xrOrigin == null)
        {
            Debug.LogError("XRSceneTransferManager：目标 Scene 中没有找到 XROrigin。");
            yield break;
        }

        Camera xrCamera = GetXRCamera(xrOrigin);

        if (xrCamera == null)
        {
            Debug.LogError("XRSceneTransferManager：XROrigin 下没有找到 Main Camera。");
            yield break;
        }

        Transform anchor = spawnAnchor.playerPhysicalAnchor;

        if (anchor == null)
        {
            anchor = spawnAnchor.transform;
        }

        Vector3 beforeCameraWorld = xrCamera.transform.position;
        Vector3 beforeOriginWorld = xrOrigin.transform.position;
        Quaternion beforeOriginRotation = xrOrigin.transform.rotation;

        /*
         * 第一步：处理旋转。
         * 注意：不能直接 SetPositionAndRotation。
         * 要围绕 Main Camera 当前世界位置旋转 XR Origin。
         * 这样玩家视角不会因为旋转而绕 XR Origin 画圆移动。
         */
        if (Mathf.Abs(pendingYawOffset) > 0.001f)
        {
            RotateOriginAroundCamera(
                xrOrigin,
                xrCamera,
                pendingYawOffset
            );
        }

        /*
         * 第二步：处理位置。
         * 不再用 Camera localPosition 反算 Origin。
         * 只看当前 Camera 世界位置和目标 Camera 世界位置之间的 delta。
         * 然后把这个 delta 加到 XR Origin 上。
         */
        ApplyPositionAlignment(
            xrOrigin,
            xrCamera,
            anchor
        );

        currentXROrigin = xrOrigin;
        pendingApply = false;

        yield return null;

        if (enableDebugLog)
        {
            Debug.Log(
                $"XR 对齐完成 | " +
                $"Mode={pendingPositionAlignMode}, " +
                $"HeightMode={pendingHeightMode}, " +
                $"YawOffset={pendingYawOffset}, " +
                $"OriginBefore={beforeOriginWorld}, " +
                $"OriginAfter={xrOrigin.transform.position}, " +
                $"OriginRotBefore={beforeOriginRotation.eulerAngles}, " +
                $"OriginRotAfter={xrOrigin.transform.rotation.eulerAngles}, " +
                $"CameraBefore={beforeCameraWorld}, " +
                $"CameraAfter={xrCamera.transform.position}, " +
                $"Anchor={anchor.position}"
            );
        }
    }

    private void RotateOriginAroundCamera(
        XROrigin xrOrigin,
        Camera xrCamera,
        float yawOffset)
    {
        Vector3 cameraWorldBefore = xrCamera.transform.position;

        Quaternion deltaRotation = Quaternion.Euler(0f, yawOffset, 0f);

        /*
         * 围绕玩家头显位置旋转整个 XR Origin。
         * 这一步会保持玩家视角位置不变，只改变场景相对玩家的朝向。
         */
        xrOrigin.transform.RotateAround(
            cameraWorldBefore,
            Vector3.up,
            yawOffset
        );

        Vector3 cameraWorldAfter = xrCamera.transform.position;

        /*
         * 理论上 RotateAround 已经围绕 cameraWorldBefore 转，
         * Camera 位置应保持不变。
         * 这里做一次微调，避免 XR 子层级导致微小偏移。
         */
        Vector3 correction = cameraWorldBefore - cameraWorldAfter;
        xrOrigin.transform.position += correction;
    }

    private void ApplyPositionAlignment(
        XROrigin xrOrigin,
        Camera xrCamera,
        Transform anchor)
    {
        Vector3 currentCameraWorld = xrCamera.transform.position;
        Vector3 targetCameraWorld;

        switch (pendingPositionAlignMode)
        {
            case PositionAlignMode.KeepPlayerWorldPosition:
                if (hasSavedCameraWorldPosition)
                {
                    targetCameraWorld = savedCameraWorldPosition;
                }
                else
                {
                    targetCameraWorld = currentCameraWorld;
                }
                break;

            case PositionAlignMode.AlignPlayerToTargetAnchor:
                targetCameraWorld = anchor.position;
                break;

            default:
                targetCameraWorld = currentCameraWorld;
                break;
        }

        Vector3 delta = targetCameraWorld - currentCameraWorld;

        if (pendingHeightMode == HeightMode.KeepAnchorFloorY)
        {
            /*
             * 只移动 XZ，不改 Y。
             * 这样不会因为头显高度、设备地面校准、玩家身高导致场景上下漂移。
             */
            delta.y = 0f;
        }

        xrOrigin.transform.position += delta;

        if (enableDebugLog)
        {
            Debug.Log(
                $"位置增量对齐 | " +
                $"CurrentCamera={currentCameraWorld}, " +
                $"TargetCamera={targetCameraWorld}, " +
                $"Delta={delta}, " +
                $"NewOrigin={xrOrigin.transform.position}"
            );
        }
    }

    private Camera GetXRCamera(XROrigin xrOrigin)
    {
        if (xrOrigin == null)
            return null;

        Camera xrCamera = xrOrigin.Camera;

        if (xrCamera == null)
        {
            xrCamera = xrOrigin.GetComponentInChildren<Camera>();
        }

        return xrCamera;
    }
}