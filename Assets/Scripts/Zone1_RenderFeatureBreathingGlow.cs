using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Zone1_RenderFeatureBreathingGlow : MonoBehaviour
{
    [Header("玩家识别")]
    public string playerTag = "Player";

    [Header("要控制的 Renderer Feature")]
    public ScriptableRendererFeature edgeGlowFeature;

    [Header("该 Feature 使用的独立发光材质")]
    public Material edgeGlowMaterial;

    [Header("Shader 属性名")]
    public string thermalColorProperty = "_ThermalColor";
    public string glowStrengthProperty = "_GlowStrength";

    [Header("边缘发光颜色")]
    public Color glowColor = new Color(1f, 0.25f, 0.05f, 1f);

    [Header("边缘发光呼吸强度")]
    public float glowMinStrength = 0.1f;
    public float glowMaxStrength = 5f;

    [Header("Spot Light 呼吸，可不用")]
    public bool controlSpotLights = false;
    public Light[] targetSpotLights;
    public float lightMinIntensity = 0.2f;
    public float lightMaxIntensity = 3f;

    [Header("呼吸周期")]
    public float breathPeriod = 2.5f;

    [Header("进入区域后的行为")]
    public bool stopWhenEnter = true;
    public bool disableGlowWhenEnter = true;
    public bool disableSpotLightsWhenEnter = true;

    [Header("防止开局误触发")]
    [Tooltip("场景开始后这段时间内忽略 TriggerEnter，防止玩家初始位置就在触发区内导致立刻关闭")]
    public float ignoreTriggerSecondsAfterStart = 1.0f;

    [Header("稳定性")]
    [Tooltip("呼吸期间每帧强制保持 Renderer Feature 开启，防止被其他脚本 Start() 关闭")]
    public bool keepFeatureActiveWhileBreathing = true;

    public bool debugLog = true;

    private bool isBreathing = true;
    private float startTime;

    private int thermalColorID;
    private int glowStrengthID;
    private int emissionColorID;

    private void Awake()
    {
        thermalColorID = Shader.PropertyToID(thermalColorProperty);
        glowStrengthID = Shader.PropertyToID(glowStrengthProperty);
        emissionColorID = Shader.PropertyToID("_EmissionColor");

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        startTime = Time.time;

        if (edgeGlowFeature != null)
        {
            edgeGlowFeature.SetActive(true);
        }

        SetGlow(glowMaxStrength);

        if (debugLog)
        {
            Debug.Log("[Zone1] 呼吸发光启动。Feature = "
                      + (edgeGlowFeature != null ? edgeGlowFeature.name : "NULL")
                      + "，Material = "
                      + (edgeGlowMaterial != null ? edgeGlowMaterial.name : "NULL"));
        }
    }

    private void Update()
    {
        if (!isBreathing) return;

        if (keepFeatureActiveWhileBreathing && edgeGlowFeature != null)
        {
            edgeGlowFeature.SetActive(true);
        }

        float phase = Mathf.Sin(Time.time * Mathf.PI * 2f / breathPeriod) * 0.5f + 0.5f;

        float glowStrength = Mathf.Lerp(glowMinStrength, glowMaxStrength, phase);
        float lightIntensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, phase);

        SetGlow(glowStrength);
        SetSpotLights(lightIntensity, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (Time.time - startTime < ignoreTriggerSecondsAfterStart)
        {
            if (debugLog)
            {
                Debug.Log("[Zone1] 开局忽略一次 TriggerEnter，避免初始重叠导致直接关闭。");
            }

            return;
        }

        if (!stopWhenEnter) return;

        if (debugLog)
        {
            Debug.Log("[Zone1] 玩家进入区域，停止呼吸发光。碰撞对象 = " + other.name);
        }

        isBreathing = false;

        if (disableGlowWhenEnter)
        {
            SetGlow(0f);

            if (edgeGlowFeature != null)
            {
                edgeGlowFeature.SetActive(false);
            }
        }

        if (disableSpotLightsWhenEnter)
        {
            SetSpotLights(0f, false);
        }
    }

    private void SetGlow(float strength)
    {
        if (edgeGlowMaterial == null) return;

        Color finalColor = glowColor * strength;
        finalColor.a = glowColor.a;

        if (edgeGlowMaterial.HasProperty(thermalColorID))
        {
            edgeGlowMaterial.SetColor(thermalColorID, finalColor);
        }
        else if (debugLog)
        {
            Debug.LogWarning("[Zone1] 材质没有属性：" + thermalColorProperty);
        }

        if (edgeGlowMaterial.HasProperty(glowStrengthID))
        {
            edgeGlowMaterial.SetFloat(glowStrengthID, strength);
        }
        else if (debugLog)
        {
            Debug.LogWarning("[Zone1] 材质没有属性：" + glowStrengthProperty);
        }

        if (edgeGlowMaterial.HasProperty(emissionColorID))
        {
            edgeGlowMaterial.SetColor(emissionColorID, finalColor);
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