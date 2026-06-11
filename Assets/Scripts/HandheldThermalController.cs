using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Rendering.Universal; // 必须引用，用于控制 Renderer Feature

public class HandheldThermalController : MonoBehaviour
{
    [Header("红外效果控制")]
    // 1. 拖入你之前在 Universal Renderer 资产里创建的那个 ThermalSilhouette 特性
    public ScriptableRendererFeature thermalFeature;

    // 2. 拖入热成像仪下的那个【cone】(带 Stencil 材质的隐形光束)
    public GameObject coneMask;

    [Header("外部同步物体")]
    // 3. 拖入场景中的【Heatmap】物体
    public GameObject heatmapObject;

    [Header("按键绑定")]
    public InputActionProperty toggleAction; // 绑定左手 A 键

    private bool isOn = false;
    private XRGrabInteractable grabInteractable;

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // 初始确保全部关闭
        UpdateSystem(false);
    }

    private void OnEnable()
    {
        if (toggleAction.action != null) toggleAction.action.Enable();
    }

    void Update()
    {
        // 逻辑：只有抓着设备按 A 键才触发
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            if (toggleAction.action.WasPressedThisFrame())
            {
                isOn = !isOn;
                UpdateSystem(isOn);
            }
        }
    }

    void UpdateSystem(bool state)
    {
        // 1. 开关动物轮廓高亮 (URP Renderer Feature)
        if (thermalFeature != null)
        {
            thermalFeature.SetActive(state);
        }

        // 2. 开关隐形投影光束 (Stencil 遮罩)
        // 只有这个开了，主相机才能通过模板测试看到 Heatmap
        if (coneMask != null)
        {
            coneMask.SetActive(state);
        }

        // 3. 同步开关热力图物体本身
        if (heatmapObject != null)
        {
            heatmapObject.SetActive(state);
        }

        Debug.Log("红外系统状态: " + (state ? "开启" : "关闭"));
    }
}