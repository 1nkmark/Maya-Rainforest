using UnityEngine;
using UnityEngine.Events;

public class GlowGuideZone : MonoBehaviour
{
    [Header("Zone Link")]
    public GlowGuideZone nextZone;

    [Header("Visual")]
    public Renderer[] borderRenderers;
    public Color normalEmissionColor = new Color(0.1f, 1.0f, 1.0f);
    public Color activeEmissionColor = new Color(1.0f, 0.85f, 0.2f);
    public Color completedEmissionColor = new Color(0.2f, 1.0f, 0.2f);

    [Header("Flash")]
    public float flashSpeed = 3.0f;
    public float minEmission = 0.6f;
    public float maxEmission = 2.5f;

    [Header("State")]
    public bool isStartZone = false;

    [Header("Events")]
    public UnityEvent onPlayerEnter;

    private Material[] runtimeMaterials;
    private Collider zoneCollider;
    private bool isActiveZone = false;
    private bool completed = false;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        runtimeMaterials = new Material[borderRenderers.Length];

        for (int i = 0; i < borderRenderers.Length; i++)
        {
            if (borderRenderers[i] == null) continue;

            runtimeMaterials[i] = borderRenderers[i].material;
            runtimeMaterials[i].EnableKeyword("_EMISSION");
        }
    }

    private void Start()
    {
        if (isStartZone)
        {
            ActivateZone();
        }
        else
        {
            HideZone();
        }
    }

    private void Update()
    {
        if (!isActiveZone || completed) return;

        float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
        float intensity = Mathf.Lerp(minEmission, maxEmission, t);

        SetEmission(activeEmissionColor, intensity);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCompleteByPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCompleteByPlayer(other);
    }

    private void TryCompleteByPlayer(Collider other)
    {
        if (!isActiveZone || completed) return;
        if (!other.CompareTag("Player")) return;

        CompleteZone();
    }

    public void ActivateZone()
    {
        completed = false;
        isActiveZone = true;

        SetVisualVisible(true);

        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
        }

        SetEmission(activeEmissionColor, maxEmission);
    }

    public void CompleteZone()
    {
        completed = true;
        isActiveZone = false;

        onPlayerEnter?.Invoke();

        // 当前区域立刻消失
        HideZone();

        // 如果有下一个区域：显示下一个区域，并显示引导线
        if (nextZone != null)
        {
            nextZone.ActivateZone();
            GuideLineManager.Instance?.ShowLineTo(nextZone);
        }
        // 如果没有下一个区域：说明已经到最后一个，隐藏引导线
        else
        {
            GuideLineManager.Instance?.HideLine();
        }
    }

    public void HideZone()
    {
        isActiveZone = false;

        SetVisualVisible(false);

        if (zoneCollider != null)
        {
            zoneCollider.enabled = false;
        }
    }

    private void SetVisualVisible(bool visible)
    {
        if (borderRenderers == null) return;

        foreach (Renderer renderer in borderRenderers)
        {
            if (renderer == null) continue;
            renderer.enabled = visible;
        }
    }

    private void SetEmission(Color color, float intensity)
    {
        if (runtimeMaterials == null) return;

        foreach (Material mat in runtimeMaterials)
        {
            if (mat == null) continue;
            mat.SetColor("_EmissionColor", color * intensity);
        }
    }

    public Vector3 GetGuideTargetPosition()
    {
        return transform.position + Vector3.up * 0.05f;
    }
}