using UnityEngine;
using UnityEngine.Playables; // 必须引用 Timeline 相关
using UnityEngine.InputSystem; // 必须引用新输入系统

public class PrologueSkipper : MonoBehaviour
{
    [Header("核心引用")]
    public PlayableDirector director;  // 拖入你的 NarrativeDirector (Timeline)
    public double lastLineStartTime;   // 最后一句话的时间（秒），比如 35.0

    [Header("绑定中指按键 (Grip)")]
    public InputActionProperty skipAction;

    void Update()
    {
        // 检查按键是否在这一帧被按下
        if (skipAction.action != null && skipAction.action.WasPressedThisFrame())
        {
            PerformSkip();
        }
    }

    // 将此函数设为 Public，这样如果你想保留那个 Skip 按钮，也能手动绑定它
    public void PerformSkip()
    {
        if (director == null) return;

        Debug.Log(">>> 检测到中指按键：跳过序幕对话 <<<");

        // 1. 强制将 Timeline 时间拨到最后一段
        director.time = lastLineStartTime;

        // 2. 关键：强制让 Timeline 刷新画面状态
        director.Evaluate();

        // 3. 继续播放（直到触发 Timeline 末尾的场景跳转逻辑）
        director.Play();
    }
}