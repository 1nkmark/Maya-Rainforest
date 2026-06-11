using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Rendering.Universal; // 必须引用

public class SimpleThermalVision : MonoBehaviour
{
    [Header("红外效果控制")]
    // 拖入刚才在 Universal Renderer 里创建的那个 SilhouetteEffect
    public ScriptableRendererFeature silhouetteFeature;

    [Header("按键绑定")]
    public InputActionProperty toggleAction; // 绑定右手 A 键

    private bool isOn = false;
    private XRGrabInteractable grab;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        // 初始确保效果是关着的
        if (silhouetteFeature != null) silhouetteFeature.SetActive(false);
    }

    private void OnEnable() => toggleAction.action.Enable();

    void Update()
    {
        // 逻辑：只有当【抓着设备】且【按下按键】时才切换
        if (grab != null && grab.isSelected && toggleAction.action.WasPressedThisFrame())
        {
            isOn = !isOn;
            ToggleThermal(isOn);
        }

        // 可选：如果希望【放下设备】自动关闭红外，启用下面这段
        /*
        if (grab != null && !grab.isSelected && isOn) {
            isOn = false;
            ToggleThermal(false);
        }
        */
    }

    void ToggleThermal(bool state)
    {
        if (silhouetteFeature != null)
        {
            silhouetteFeature.SetActive(state);
        }
        Debug.Log("红外轮廓系统: " + (state ? "激活" : "关闭"));
    }
}