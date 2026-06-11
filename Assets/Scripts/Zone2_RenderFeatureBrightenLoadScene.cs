using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class Zone2_RenderFeatureBrightenLoadScene : MonoBehaviour
{
    [Header("玩家识别")]
    public string playerTag = "Player";

    [Header("边缘发光 Renderer Feature")]
    public ScriptableRendererFeature edgeGlowFeature;

    [Header("Renderer Feature 使用的发光材质")]
    [Tooltip("把用于边缘发光的 SG_Thermal Material 拖进来。可以有多个。")]
    public Material[] edgeGlowMaterials;

    [Header("Shader 属性名")]
    public string thermalColorProperty = "_ThermalColor";
    public string glowStrengthProperty = "_GlowStrength";

    [Header("边缘发光颜色")]
    public Color glowColor = new Color(1f, 0.25f, 0.05f, 1f);

    [Header("边缘发光强度")]
    public float glowStartStrength = 0f;
    public float glowTargetStrength = 8f;

    [Header("Spot Light 变亮")]
    public bool controlSpotLights = true;
    public Light[] targetSpotLights;
    public float lightStartIntensity = 0f;
    public float lightTargetIntensity = 8f;

    [Header("时间控制")]
    public float delayBeforeBrighten = 1.0f;
    public float brightenDuration = 3.0f;
    public float delayBeforeLoadScene = 0.5f;

    [Header("切换场景")]
    public string targetSceneName;

    [Tooltip("进入下一个 Scene 后，相对目标 Scene 锚点方向额外旋转的 Y 轴角度")]
    public float sceneYawOffset = 0f;

    [Header("切换场景音效")]
    public AudioClip sceneLoadAudioClip;

    [Range(0f, 1f)]
    public float sceneLoadAudioVolume = 1f;

    [Tooltip("勾选后：音效播完再切场景。不勾选：音效开始播放后立刻切场景。")]
    public bool waitForAudioBeforeLoad = false;

    [Tooltip("建议勾选。切场景后音效继续播放。")]
    public bool keepAudioAfterSceneLoad = true;

    private bool hasTriggered = false;

    private int thermalColorID;
    private int glowStrengthID;

    private void Awake()
    {
        thermalColorID = Shader.PropertyToID(thermalColorProperty);
        glowStrengthID = Shader.PropertyToID(glowStrengthProperty);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        if (edgeGlowFeature != null)
        {
            edgeGlowFeature.SetActive(false);
        }

        SetGlowMaterials(glowStartStrength);
        SetSpotLights(lightStartIntensity, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;
        StartCoroutine(BrightenThenLoadScene());
    }

    private IEnumerator BrightenThenLoadScene()
    {
        yield return new WaitForSeconds(delayBeforeBrighten);

        if (edgeGlowFeature != null)
        {
            edgeGlowFeature.SetActive(true);
        }

        float timer = 0f;

        while (timer < brightenDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / brightenDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float currentGlowStrength = Mathf.Lerp(glowStartStrength, glowTargetStrength, t);
            float currentLightIntensity = Mathf.Lerp(lightStartIntensity, lightTargetIntensity, t);

            SetGlowMaterials(currentGlowStrength);
            SetSpotLights(currentLightIntensity, true);

            yield return null;
        }

        SetGlowMaterials(glowTargetStrength);
        SetSpotLights(lightTargetIntensity, true);

        yield return new WaitForSeconds(delayBeforeLoadScene);

        yield return PlaySceneLoadAudio();

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Zone2_RenderFeatureBrightenLoadScene：没有设置 targetSceneName。");
            yield break;
        }

        if (XRSceneTransferManager.Instance == null)
        {
            Debug.LogError("Zone2_RenderFeatureBrightenLoadScene：场景中没有 XRSceneTransferManager，无法执行 XR 对齐跳转。");
            yield break;
        }

        XRSceneTransferManager.Instance.LoadSceneWithPhysicalAlignment(
            targetSceneName,
            sceneYawOffset,
            XRSceneTransferManager.Instance.defaultPositionAlignMode,
            XRSceneTransferManager.Instance.defaultHeightMode
        );
    }

    private IEnumerator PlaySceneLoadAudio()
    {
        if (sceneLoadAudioClip == null)
        {
            yield break;
        }

        if (keepAudioAfterSceneLoad)
        {
            GameObject audioObject = new GameObject("SceneLoadAudio_OneShot");
            DontDestroyOnLoad(audioObject);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = sceneLoadAudioClip;
            source.volume = sceneLoadAudioVolume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();

            Destroy(audioObject, sceneLoadAudioClip.length + 0.5f);

            if (waitForAudioBeforeLoad)
            {
                yield return new WaitForSeconds(sceneLoadAudioClip.length);
            }
        }
        else
        {
            AudioSource source = GetComponent<AudioSource>();

            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.volume = sceneLoadAudioVolume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.PlayOneShot(sceneLoadAudioClip);

            if (waitForAudioBeforeLoad)
            {
                yield return new WaitForSeconds(sceneLoadAudioClip.length);
            }
        }
    }

    private void SetGlowMaterials(float strength)
    {
        if (edgeGlowMaterials == null) return;

        Color finalColor = glowColor * strength;
        finalColor.a = glowColor.a;

        foreach (Material mat in edgeGlowMaterials)
        {
            if (mat == null) continue;

            if (mat.HasProperty(thermalColorID))
            {
                mat.SetColor(thermalColorID, finalColor);
            }

            if (mat.HasProperty(glowStrengthID))
            {
                mat.SetFloat(glowStrengthID, strength);
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", finalColor);
            }
        }
    }

    private void SetSpotLights(float intensity, bool enabled)
    {
        if (!controlSpotLights || targetSpotLights == null) return;

        foreach (Light l in targetSpotLights)
        {
            if (l == null) continue;

            l.enabled = enabled;
            l.intensity = intensity;
        }
    }
}