using System.Collections;
using UnityEngine;

public class MayaFinalSequenceController : MonoBehaviour
{
    [Header("XR 视角震动")]
    public Transform xrOriginTransform;
    public float shakeStrength = 0.035f;
    public float shakeFrequency = 35f;

    [Header("石碑下沉")]
    public Transform steleTransform;
    public bool useLocalPosition = true;
    public Vector3 steleTargetPosition;
    public float steleMoveDuration = 2.0f;

    [Header("石门上升")]
    public Transform stoneDoorTransform;
    public Vector3 stoneDoorTargetPosition;
    public float stoneDoorMoveDuration = 2.5f;

    [Header("传送门幕布")]
    public GameObject portalCurtain;
    public AudioSource portalAudioSource;

    [Header("传送检测")]
    public PortalSceneLoader portalSceneLoader;

    private bool isPlaying = false;

    private void Start()
    {
        if (portalCurtain != null)
        {
            portalCurtain.SetActive(false);
        }

        if (portalSceneLoader != null)
        {
            portalSceneLoader.DeactivatePortal();
        }
    }

    public void StartFinalSequence()
    {
        if (isPlaying)
            return;

        StartCoroutine(FinalSequenceRoutine());
    }

    private IEnumerator FinalSequenceRoutine()
    {
        isPlaying = true;

        if (portalSceneLoader != null)
        {
            portalSceneLoader.DeactivatePortal();
        }

        if (portalCurtain != null)
        {
            portalCurtain.SetActive(false);
        }

        yield return StartCoroutine(MoveTransformWithShake(
            steleTransform,
            steleTargetPosition,
            steleMoveDuration
        ));

        yield return StartCoroutine(MoveTransformWithShake(
            stoneDoorTransform,
            stoneDoorTargetPosition,
            stoneDoorMoveDuration
        ));

        if (portalCurtain != null)
        {
            portalCurtain.SetActive(true);
        }

        if (portalAudioSource != null)
        {
            portalAudioSource.Play();
        }

        if (portalSceneLoader != null)
        {
            portalSceneLoader.ActivatePortal();
        }

        isPlaying = false;
    }

    private IEnumerator MoveTransformWithShake(Transform target, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = Vector3.zero;

        if (target != null)
        {
            startPosition = useLocalPosition ? target.localPosition : target.position;
        }

        Vector3 originalXROriginLocalPosition = Vector3.zero;
        bool canShake = xrOriginTransform != null;

        if (canShake)
        {
            originalXROriginLocalPosition = xrOriginTransform.localPosition;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (target != null)
            {
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, t);

                if (useLocalPosition)
                    target.localPosition = currentPosition;
                else
                    target.position = currentPosition;
            }

            if (canShake)
            {
                float offsetX = Mathf.Sin(timer * shakeFrequency) * shakeStrength;
                float offsetZ = Mathf.Cos(timer * shakeFrequency * 1.3f) * shakeStrength;

                xrOriginTransform.localPosition =
                    originalXROriginLocalPosition + new Vector3(offsetX, 0f, offsetZ);
            }

            yield return null;
        }

        if (target != null)
        {
            if (useLocalPosition)
                target.localPosition = targetPosition;
            else
                target.position = targetPosition;
        }

        if (canShake)
        {
            xrOriginTransform.localPosition = originalXROriginLocalPosition;
        }
    }
}