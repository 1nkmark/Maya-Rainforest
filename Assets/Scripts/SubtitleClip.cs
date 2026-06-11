using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class SubtitleClip : PlayableAsset, ITimelineClipAsset {
    [TextArea(3, 10)] public string text;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
        var playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);
        SubtitleBehaviour behaviour = playable.GetBehaviour();
        behaviour.subtitleText = text;
        return playable;
    }
}