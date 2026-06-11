using UnityEngine;
using UnityEngine.Playables;
using TMPro;

public class SubtitleBehaviour : PlayableBehaviour {
    public string subtitleText;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
        TextMeshProUGUI textNode = playerData as TextMeshProUGUI;
        if (textNode != null) {
            // 只有当当前 Clip 权重较大时才更新，防止多个 Clip 混叠
            if (info.weight > 0.5f) {
                textNode.text = subtitleText;
            }
        }
    }

    // 当播放指针离开 Clip 时，清空字幕（可选）
    public override void OnBehaviourPause(Playable playable, FrameData info) {
        // 如果你需要播完消失，可以在这里写逻辑，但更好的做法是加一个 Mixer
    }
}