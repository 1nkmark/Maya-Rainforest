using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GuideLineManager : MonoBehaviour
{
    public static GuideLineManager Instance { get; private set; }

    [Header("References")]
    public Transform playerTransform;

    [Header("Line Settings")]
    public float lineHeight = 14f;
    public float startForwardOffset = 0.35f;
    public float updateSpeed = 12f;

    [Header("Line Shape")]
    public int pointCount = 16;
    public float waveAmplitude = 0.08f;
    public float waveFrequency = 2.0f;

    private LineRenderer lineRenderer;
    private GlowGuideZone targetZone;
    private bool hasTarget = false;

    private void Awake()
    {
        Instance = this;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (!hasTarget || playerTransform == null || targetZone == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        UpdateGuideLine();
    }

    public void ShowLineTo(GlowGuideZone zone)
    {
        targetZone = zone;
        hasTarget = zone != null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = hasTarget;
        }
    }

    public void HideLine()
    {
        hasTarget = false;
        targetZone = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void UpdateGuideLine()
    {
        Vector3 start = playerTransform.position + playerTransform.forward * startForwardOffset;
        Vector3 end = targetZone.GetGuideTargetPosition();

        start.y = lineHeight;
        end.y = lineHeight;

        lineRenderer.positionCount = pointCount;

        Vector3 direction = end - start;
        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            float wave = Mathf.Sin((t * waveFrequency + Time.time) * Mathf.PI * 2f) * waveAmplitude;
            pos += right * wave;

            lineRenderer.SetPosition(i, pos);
        }
    }
}