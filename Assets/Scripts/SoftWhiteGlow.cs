using UnityEngine;

[DisallowMultipleComponent]
public class SoftWhiteGlow : MonoBehaviour
{
    [Header("Glow Activation")]
    public bool useDistanceTrigger = false;
    public JaguarAI monitoredJaguar;
    public Transform distanceTarget;
    public Transform distanceReference;

    [Min(0.1f)]
    public float triggerDistance = 8f;

    public bool requireJaguarMovement = true;
    public bool autoFindMainCamera = true;
    public bool autoFindJaguar = true;

    [Min(0f)]
    public float fadeSpeed = 6f;

    [Header("Glow Color")]
    [ColorUsage(false, true)]
    public Color glowColor = Color.white;

    [Header("Surface Emission")]
    [Min(0f)]
    public float emissionIntensity = 0.6f;

    [Header("Point Light")]
    [Min(0f)]
    public float lightIntensity = 1.2f;

    [Min(0.1f)]
    public float lightRange = 4f;

    [Range(0f, 1f)]
    public float shadowStrength = 0f;

    [Header("Optional Pulse")]
    public bool pulseGlow = false;

    [Min(0.1f)]
    public float pulseSpeed = 1.2f;

    [Range(0f, 1f)]
    public float pulseAmount = 0.15f;

    private Renderer[] targetRenderers;
    private Light glowLight;
    private Material[] runtimeMaterials;
    private MaterialPropertyBlock propertyBlock;
    private float currentActivationMultiplier = 1f;

    private void Reset()
    {
        InitializeGlow(true);
    }

    private void Awake()
    {
        InitializeGlow(true);
    }

    private void OnEnable()
    {
        InitializeGlow(true);
    }

    private void OnValidate()
    {
        triggerDistance = Mathf.Max(0.1f, triggerDistance);
        fadeSpeed = Mathf.Max(0f, fadeSpeed);

        InitializeGlow(!Application.isPlaying);
    }

    private void Update()
    {
        if (!useDistanceTrigger && !pulseGlow)
        {
            return;
        }

        ResolveSceneReferences();

        float targetActivation = GetTargetActivationMultiplier();
        currentActivationMultiplier = fadeSpeed > 0f
            ? Mathf.MoveTowards(currentActivationMultiplier, targetActivation, fadeSpeed * Time.deltaTime)
            : targetActivation;

        ApplyGlow(GetCombinedGlowMultiplier(currentActivationMultiplier));
    }

    [ContextMenu("Refresh Glow")]
    public void RefreshGlow()
    {
        InitializeGlow(true);
    }

    private void InitializeGlow(bool snapToTargetState)
    {
        CacheRenderers();
        EnsureLight();
        CacheMaterials();
        ResolveSceneReferences();

        if (snapToTargetState)
        {
            currentActivationMultiplier = GetTargetActivationMultiplier();
        }

        ApplyGlow(GetCombinedGlowMultiplier(currentActivationMultiplier));
    }

    private void CacheMaterials()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            runtimeMaterials = null;
            return;
        }

        if (Application.isPlaying)
        {
            System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();

            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] rendererMaterials = renderer.materials;
                foreach (Material material in rendererMaterials)
                {
                    if (material != null && !materials.Contains(material))
                    {
                        materials.Add(material);
                    }
                }
            }

            runtimeMaterials = materials.ToArray();
            return;
        }

        runtimeMaterials = null;
    }

    private void CacheRenderers()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void EnsureLight()
    {
        glowLight = GetComponent<Light>();

        if (glowLight == null)
        {
            glowLight = gameObject.AddComponent<Light>();
        }

        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = lightRange;
        glowLight.intensity = lightIntensity;
        glowLight.renderMode = LightRenderMode.Auto;
        glowLight.shadows = shadowStrength > 0.01f ? LightShadows.Soft : LightShadows.None;
        glowLight.shadowStrength = shadowStrength;
    }

    private void ResolveSceneReferences()
    {
        if (autoFindJaguar && monitoredJaguar == null)
        {
            monitoredJaguar = FindObjectOfType<JaguarAI>();
        }

        if (distanceTarget == null && monitoredJaguar != null)
        {
            distanceTarget = monitoredJaguar.transform;
        }

        if (autoFindMainCamera && distanceReference == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                distanceReference = mainCamera.transform;
            }
        }
    }

    private float GetTargetActivationMultiplier()
    {
        if (!useDistanceTrigger)
        {
            return 1f;
        }

        if (distanceTarget == null || distanceReference == null)
        {
            return 0f;
        }

        if (requireJaguarMovement && (monitoredJaguar == null || !monitoredJaguar.IsMoving))
        {
            return 0f;
        }

        float distance = Vector3.Distance(distanceTarget.position, distanceReference.position);
        if (distance >= triggerDistance)
        {
            return 0f;
        }

        return Mathf.Clamp01(1f - (distance / triggerDistance));
    }

    private float GetCombinedGlowMultiplier(float activationMultiplier)
    {
        if (activationMultiplier <= 0f)
        {
            return 0f;
        }

        if (!pulseGlow)
        {
            return activationMultiplier;
        }

        return activationMultiplier * GetPulseMultiplier();
    }

    private void ApplyGlow(float multiplier)
    {
        if (glowLight != null)
        {
            glowLight.enabled = multiplier > 0.001f;
            glowLight.color = glowColor;
            glowLight.range = lightRange;
            glowLight.intensity = lightIntensity * multiplier;
            glowLight.shadows = shadowStrength > 0.01f ? LightShadows.Soft : LightShadows.None;
            glowLight.shadowStrength = shadowStrength;
        }

        Color emissionColor = glowColor * emissionIntensity * multiplier;

        if (!Application.isPlaying)
        {
            ApplyEditorEmission(emissionColor);
            return;
        }

        if (runtimeMaterials == null)
        {
            return;
        }

        foreach (Material material in runtimeMaterials)
        {
            if (material == null || !material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }
    }

    private void ApplyEditorEmission(Color emissionColor)
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            propertyBlock.Clear();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private float GetPulseMultiplier()
    {
        float wave = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
        return 1f + wave * pulseAmount;
    }
}
