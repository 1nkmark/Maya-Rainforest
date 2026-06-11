using UnityEngine;

public class LightZoneController : MonoBehaviour
{
    [Header("需要控制的灯光")]
    public Light[] targetLights;

    [Header("进入区域时是否打开灯光")]
    public bool turnOnWhenEnter = true;

    [Header("离开区域时是否恢复相反状态")]
    public bool reverseWhenExit = true;

    [Header("是否渐变灯光")]
    public bool useFade = true;

    [Header("渐变时间")]
    public float fadeDuration = 1.0f;

    [Header("目标亮度")]
    public float targetIntensity = 3.0f;

    private Coroutine fadeCoroutine;

    private void Start()
    {
        // 初始状态：如果进入时打开，则初始关闭；如果进入时关闭，则初始打开
        bool initialState = !turnOnWhenEnter;

        foreach (Light l in targetLights)
        {
            if (l == null) continue;

            l.enabled = initialState;

            if (initialState)
                l.intensity = targetIntensity;
            else
                l.intensity = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        SetLights(turnOnWhenEnter);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (reverseWhenExit)
        {
            SetLights(!turnOnWhenEnter);
        }
    }

    private void SetLights(bool turnOn)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (useFade)
            fadeCoroutine = StartCoroutine(FadeLights(turnOn));
        else
            SetLightsImmediate(turnOn);
    }

    private void SetLightsImmediate(bool turnOn)
    {
        foreach (Light l in targetLights)
        {
            if (l == null) continue;

            l.enabled = turnOn;
            l.intensity = turnOn ? targetIntensity : 0f;
        }
    }

    private System.Collections.IEnumerator FadeLights(bool turnOn)
    {
        float time = 0f;

        float[] startIntensities = new float[targetLights.Length];

        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] == null) continue;

            startIntensities[i] = targetLights[i].intensity;

            if (turnOn)
                targetLights[i].enabled = true;
        }

        float endIntensity = turnOn ? targetIntensity : 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            for (int i = 0; i < targetLights.Length; i++)
            {
                if (targetLights[i] == null) continue;

                targetLights[i].intensity = Mathf.Lerp(startIntensities[i], endIntensity, t);
            }

            yield return null;
        }

        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] == null) continue;

            targetLights[i].intensity = endIntensity;

            if (!turnOn)
                targetLights[i].enabled = false;
        }
    }
}