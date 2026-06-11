using UnityEngine;

public class PyramidGlowPulse : MonoBehaviour
{
    [Header("Target")]
    public Renderer targetRenderer;

    [Header("Emission")]
    public Color emissionColor = new Color(1.0f, 0.75f, 0.15f);
    public float minEmission = 0.3f;
    public float maxEmission = 4.0f;
    public float pulseSpeed = 2.0f;

    [Header("Initial State")]
    public bool playOnStart = false;

    [Header("Stop Behavior")]
    public bool hideRendererWhenStopped = true;
    public bool disableEmissionWhenStopped = true;

    private Material[] runtimeMaterials;
    private bool isPlaying = false;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogWarning("[PyramidGlowPulse] Target Renderer is missing.");
            return;
        }

        runtimeMaterials = targetRenderer.materials;

        foreach (Material mat in runtimeMaterials)
        {
            if (mat == null) continue;
            mat.EnableKeyword("_EMISSION");
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayGlow();
        }
        else
        {
            StopGlow();
        }
    }

    private void Update()
    {
        if (!isPlaying || runtimeMaterials == null) return;

        float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        float intensity = Mathf.Lerp(minEmission, maxEmission, t);

        SetEmission(emissionColor, intensity);
    }

    public void PlayGlow()
    {
        if (targetRenderer == null) return;

        isPlaying = true;
        targetRenderer.enabled = true;
    }

    public void StopGlow()
    {
        isPlaying = false;

        if (disableEmissionWhenStopped)
        {
            SetEmission(Color.black, 0f);
        }

        if (hideRendererWhenStopped && targetRenderer != null)
        {
            targetRenderer.enabled = false;
        }
    }

    private void SetEmission(Color color, float intensity)
    {
        if (runtimeMaterials == null) return;

        foreach (Material mat in runtimeMaterials)
        {
            if (mat == null) continue;

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
        }
    }
}