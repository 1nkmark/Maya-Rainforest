using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LeftHandOnlyGrab : MonoBehaviour
{
    [Header("红外成像仪上的 XR Grab Interactable")]
    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("只允许这个左手 Interactor 抓取")]
    [SerializeField] private Transform leftHandInteractor;

    [Header("调试")]
    [SerializeField] private bool showDebugLog = true;

    void Awake()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }
    }

    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (leftHandInteractor == null)
        {
            Debug.LogWarning("LeftHandOnlyGrab：还没有指定 Left Hand Interactor！");
            return;
        }

        Transform currentInteractor = args.interactorObject.transform;

        bool isLeftHand =
            currentInteractor == leftHandInteractor ||
            currentInteractor.IsChildOf(leftHandInteractor) ||
            leftHandInteractor.IsChildOf(currentInteractor);

        if (!isLeftHand)
        {
            if (showDebugLog)
            {
                Debug.Log("右手或其他 Interactor 尝试抓取红外成像仪，强制释放。");
            }

            StartCoroutine(ForceReleaseNextFrame(args.interactorObject));
        }
        else
        {
            if (showDebugLog)
            {
                Debug.Log("左手成功抓取红外成像仪。");
            }
        }
    }

    private IEnumerator ForceReleaseNextFrame(IXRSelectInteractor wrongInteractor)
    {
        yield return null;

        if (grabInteractable != null &&
            grabInteractable.interactionManager != null &&
            wrongInteractor != null)
        {
            grabInteractable.interactionManager.SelectExit(wrongInteractor, grabInteractable);
        }
    }
}