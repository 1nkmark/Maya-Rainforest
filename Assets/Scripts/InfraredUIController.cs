using UnityEngine;
using UnityEngine.InputSystem;

public class InfraredUIController : MonoBehaviour
{
    [Header("UI护目镜设置")]
    public GameObject infraredHUD; // 你的护目镜图片UI

    [Header("场景热力图设置")]
    public GameObject heatmapObject; // 你的热力图立方体/预制体

    [Header("按键绑定")]
    public InputActionProperty toggleAction;

    private bool isActive = false;

    private void OnEnable()
    {
        if (toggleAction.action != null) toggleAction.action.Enable();
    }

    private void OnDisable()
    {
        if (toggleAction.action != null) toggleAction.action.Disable();
    }

    void Update()
    {
        // 核心：按下按键，同时切换两个物体的状态
        if (toggleAction.action.WasPressedThisFrame())
        {
            ToggleInfraredSystem();
        }
    }

    void ToggleInfraredSystem()
    {
        isActive = !isActive;

        // 1. 控制护目镜UI
        if (infraredHUD != null)
        {
            infraredHUD.SetActive(isActive);
        }

        // 2. 控制热力图立方体
        if (heatmapObject != null)
        {
            heatmapObject.SetActive(isActive);
        }

        Debug.Log("红外系统开启状态: " + isActive);
    }
}