using UnityEngine;
using UnityEngine.Timeline;
using TMPro;

[TrackColor(0.1f, 0.8f, 1f)]
[TrackBindingType(typeof(TextMeshProUGUI))]
[TrackClipType(typeof(SubtitleClip))]
public class SubtitleTrack : TrackAsset {}