using UnityEngine;
using UnityEngine.InputSystem; // 必须引入新输入系统

public class InfraredController : MonoBehaviour
{
    [Header("要把哪个物体变出来？(拖入Heatmap)")]
    public GameObject heatmapObject; [Header("绑定哪个按键？(推荐右手侧键 Grip)")]
    public InputActionProperty activateButton;

    void Start()
    {
        // 游戏刚开始时，确保红外视觉是关闭的
        if (heatmapObject != null)
        {
            heatmapObject.SetActive(false);
        }
    }

    void Update()
    {
        // 如果没有绑定按键或热力图，就不执行
        if (activateButton.action == null || heatmapObject == null) return;

        // 读取按键按下的深度 (0 到 1)
        // 侧键/扳机键通常带有键程，大于 0.1 就算按下了
        bool isPressed = activateButton.action.ReadValue<float>() > 0.1f;

        // 核心逻辑：按住就显示，松开就隐藏
        heatmapObject.SetActive(isPressed);
    }
}