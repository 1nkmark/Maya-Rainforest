using UnityEngine;

public class ProximitySound : MonoBehaviour
{
    [Header("目标物体")]
    public GameObject targetObject;          // 需要靠近的物体

    [Header("检测设置")]
    public float triggerDistance = 5f;       // 触发声音的距离
    public bool onlyPlayOncePerApproach = true;  // 每次靠近只播放一次，离开后重置

    [Header("声音设置")]
    public AudioClip soundClip;              // 要播放的音效
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;                // 是否循环播放（一般不需要）
    public bool playOneShot = false;         // 是否用 PlayOneShot（允许重叠播放）

    private AudioSource audioSource;
    private bool hasPlayed = false;           // 记录本次靠近是否已播放过

    void Start()
    {
        // 获取或添加 AudioSource 组件（声音从角色身上发出）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置 AudioSource
        audioSource.clip = soundClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // 安全检查：目标物体未设置时给出警告
        if (targetObject == null)
        {
            Debug.LogWarning("ProximitySound：尚未指定目标物体！");
            return;
        }

        // 计算角色与目标物体的距离
        float distance = Vector3.Distance(transform.position, targetObject.transform.position);

        // 判断是否进入触发范围
        if (distance <= triggerDistance)
        {
            // 在范围内，且尚未播放过本次靠近
            if (!hasPlayed)
            {
                PlaySound();
                hasPlayed = true;
            }
        }
        else
        {
            // 离开范围后重置标志，以便下次靠近再次播放
            if (onlyPlayOncePerApproach)
            {
                hasPlayed = false;
            }
        }
    }

    private void PlaySound()
    {
        if (soundClip == null)
        {
            Debug.LogError("ProximitySound：未指定声音片段！");
            return;
        }

        if (playOneShot)
            audioSource.PlayOneShot(soundClip, volume);
        else
            audioSource.Play();
    }

    // 在编辑器中可视化触发范围（方便调试）
    void OnDrawGizmosSelected()
    {
        if (targetObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetObject.transform.position, triggerDistance);
        }
    }
}