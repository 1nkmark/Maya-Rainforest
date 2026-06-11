using UnityEngine;

public class DistanceTriggerSound : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("要检测的目标对象（例如玩家）。如果为空，会自动查找标签为 'Player' 的对象")]
    public Transform target;

    [Header("距离阈值")]
    [Tooltip("触发音效的最大距离")]
    public float triggerDistance = 5f;

    [Header("音效设置")]
    [Tooltip("需要播放的音效片段")]
    public AudioClip soundClip;
    [Tooltip("音量（0~1）")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("触发行为")]
    [Tooltip("是否只触发一次（true=只播放一次，之后不再触发；false=每次进入范围都会播放）")]
    public bool playOnceOnly = true;

    // 内部状态
    private bool hasPlayed = false;          // 是否已经播放过（当 playOnceOnly = true 时有效）
    private bool isInsideRange = false;      // 当前是否在触发范围内（用于离开后重置状态）

    private AudioSource audioSource;          // 用于播放音效的 AudioSource

    void Start()
    {
        // 如果没有手动指定目标，尝试通过标签找到玩家
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
            else
                Debug.LogWarning("未设置目标，且场景中没有 Tag 为 'Player' 的对象！");
        }

        // 如果没有 AudioSource 组件，则添加一个
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            // 可选：设置一些默认属性适合距离触发
            audioSource.spatialBlend = 1f;     // 完全3D音效，声音会随距离衰减
            audioSource.dopplerLevel = 0f;     // 简化，不建议此处使用多普勒
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = triggerDistance;
        }

        // 确保不自发自动播放
        audioSource.playOnAwake = false;
        audioSource.clip = soundClip;
    }

    void Update()
    {
        // 如果目标不存在，无法检测距离
        if (target == null) return;

        // 如果已设置为“只播放一次”并且已经播放过，则不再检测
        if (playOnceOnly && hasPlayed) return;

        // 计算距离
        float currentDistance = Vector3.Distance(transform.position, target.position);
        bool currentlyInside = currentDistance <= triggerDistance;

        // 检测是否刚刚进入范围
        if (currentlyInside && !isInsideRange)
        {
            OnEnterRange();
        }
        // 检测是否刚刚离开范围（用于重置一次性触发标志，允许多次触发模式下的重置）
        else if (!currentlyInside && isInsideRange)
        {
            OnExitRange();
        }

        isInsideRange = currentlyInside;
    }

    /// <summary>
    /// 当目标进入距离范围内时调用
    /// </summary>
    private void OnEnterRange()
    {
        // 播放音效
        PlaySound();

        // 如果设置只触发一次，标记已播放
        if (playOnceOnly)
        {
            hasPlayed = true;
        }
    }

    /// <summary>
    /// 当目标离开距离范围时调用（用于重复触发模式下，离开后可以再次触发）
    /// </summary>
    private void OnExitRange()
    {
        // 目前不需要额外动作，但可以在这里重置一些重复触发逻辑
        // 对于 playOnceOnly = false 的情况，离开范围后下次再进入仍会播放
        // hasPlayed 不变，只有 playOnceOnly 为 true 时才永久禁止
        if (!playOnceOnly)
        {
            // 如果需要重置某些内部状态，这里留空即可，因为 isInsideRange 控制进入判定
        }
    }

    /// <summary>
    /// 播放音效的实际方法
    /// </summary>
    private void PlaySound()
    {
        if (soundClip == null)
        {
            Debug.LogWarning("没有指定音效片段 AudioClip！");
            return;
        }

        // 使用 AudioSource 播放一次
        audioSource.PlayOneShot(soundClip, volume);
    }

    // 可选：在编辑器中绘制触发范围（方便调试）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}