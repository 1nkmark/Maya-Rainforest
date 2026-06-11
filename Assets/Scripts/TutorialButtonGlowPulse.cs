using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TutorialButtonGlowPulse : MonoBehaviour
{
    [Header("Targets")]
    public Renderer[] highlightedRenderers;
    public bool autoFindChildRenderers = false;

    [Header("Glow")]
    [ColorUsage(false, true)]
    public Color glowColor = new Color(1f, 0.72f, 0.22f);
    public float minEmission = 0.6f;
    public float maxEmission = 2.0f;
    public float pulseSpeed = 0.8f;
    public float phaseOffset = 0f;

    [Header("Surface Color")]
    public bool alsoPulseBaseColor = true;
    public Color dimBaseColor = new Color(0.45f, 0.32f, 0.08f, 1f);
    public Color brightBaseColor = new Color(1f, 0.82f, 0.28f, 1f);

    [Header("State")]
    public bool playOnStart = true;
    public bool restoreWhenDisabled = true;

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<MaterialState> originalStates = new List<MaterialState>();
    private bool isPlaying;
    private bool initialized;

    private void Awake()
    {
        InitializeRuntimeMaterials();
    }

    private void Start()
    {
        SetPulseActive(playOnStart);
    }

    private void OnDisable()
    {
        if (restoreWhenDisabled)
        {
            RestoreOriginalState();
        }
    }

    private void Update()
    {
        if (!isPlaying || runtimeMaterials.Count == 0)
        {
            return;
        }

        float wave = Mathf.Sin((Time.time * pulseSpeed + phaseOffset) * Mathf.PI * 2f);
        float t = 0.5f + wave * 0.5f;
        float emissionIntensity = Mathf.Lerp(minEmission, maxEmission, t);
        Color baseColor = Color.Lerp(dimBaseColor, brightBaseColor, t);

        ApplyGlow(glowColor * emissionIntensity, baseColor);
    }

    [ContextMenu("Refresh Targets")]
    public void RefreshTargets()
    {
        initialized = false;
        InitializeRuntimeMaterials();
    }

    public void SetPulseActive(bool active)
    {
        isPlaying = active;

        if (!initialized)
        {
            InitializeRuntimeMaterials();
        }

        if (!isPlaying)
        {
            RestoreOriginalState();
        }
    }

    private void InitializeRuntimeMaterials()
    {
        if (initialized)
        {
            return;
        }

        runtimeMaterials.Clear();
        originalStates.Clear();

        Renderer[] targets = highlightedRenderers;
        if ((targets == null || targets.Length == 0) && autoFindChildRenderers)
        {
            targets = GetComponentsInChildren<Renderer>(true);
        }

        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("[TutorialButtonGlowPulse] No highlighted renderers assigned.", this);
            initialized = true;
            return;
        }

        foreach (Renderer target in targets)
        {
            if (target == null)
            {
                continue;
            }

            Material[] materials = target.materials;
            foreach (Material material in materials)
            {
                if (material == null || runtimeMaterials.Contains(material))
                {
                    continue;
                }

                runtimeMaterials.Add(material);
                originalStates.Add(new MaterialState(material));

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                }
            }
        }

        initialized = true;
    }

    private void ApplyGlow(Color emissionColor, Color baseColor)
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            if (alsoPulseBaseColor)
            {
                SetBaseColor(material, baseColor);
            }
        }
    }

    private static void SetBaseColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void RestoreOriginalState()
    {
        for (int i = 0; i < originalStates.Count; i++)
        {
            originalStates[i].Restore();
        }
    }

    private struct MaterialState
    {
        private readonly Material material;
        private readonly bool hadEmissionProperty;
        private readonly bool emissionEnabled;
        private readonly Color emissionColor;
        private readonly bool hadBaseColorProperty;
        private readonly Color baseColor;
        private readonly bool hadColorProperty;
        private readonly Color color;

        public MaterialState(Material material)
        {
            this.material = material;
            hadEmissionProperty = material != null && material.HasProperty("_EmissionColor");
            emissionEnabled = material != null && material.IsKeywordEnabled("_EMISSION");
            emissionColor = hadEmissionProperty ? material.GetColor("_EmissionColor") : Color.black;
            hadBaseColorProperty = material != null && material.HasProperty("_BaseColor");
            baseColor = hadBaseColorProperty ? material.GetColor("_BaseColor") : Color.white;
            hadColorProperty = material != null && material.HasProperty("_Color");
            color = hadColorProperty ? material.GetColor("_Color") : Color.white;
        }

        public void Restore()
        {
            if (material == null)
            {
                return;
            }

            if (hadEmissionProperty)
            {
                material.SetColor("_EmissionColor", emissionColor);

                if (emissionEnabled)
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }

            if (hadBaseColorProperty)
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (hadColorProperty)
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
