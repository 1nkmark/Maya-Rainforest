using System.Collections;
using UnityEngine;

public class PortalSceneLoader : MonoBehaviour
{
    [Header("Scene")]
    public string targetSceneName = "ThirdScene";
    public float loadDelay = 0f;

    [Tooltip("进入下一个 Scene 后，相对目标 Scene 锚点方向额外旋转的 Y 轴角度")]
    public float sceneYawOffset = 0f;

    [Header("Trigger")]
    public Collider portalTriggerCollider;
    public string playerTag = "Player";

    private bool portalActive = false;
    private bool isLoading = false;

    private void Awake()
    {
        if (portalTriggerCollider == null)
        {
            portalTriggerCollider = GetComponent<Collider>();
        }

        if (portalTriggerCollider != null)
        {
            portalTriggerCollider.isTrigger = true;
            portalTriggerCollider.enabled = false;
        }

        portalActive = false;
    }

    public void ActivatePortal()
    {
        portalActive = true;
        isLoading = false;

        if (portalTriggerCollider != null)
        {
            portalTriggerCollider.enabled = true;
            portalTriggerCollider.isTrigger = true;
        }
    }

    public void DeactivatePortal()
    {
        portalActive = false;
        isLoading = false;

        if (portalTriggerCollider != null)
        {
            portalTriggerCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLoadScene(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryLoadScene(other);
    }

    private void TryLoadScene(Collider other)
    {
        if (!portalActive || isLoading)
            return;

        if (!other.CompareTag(playerTag))
            return;

        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        isLoading = true;

        if (loadDelay > 0f)
        {
            yield return new WaitForSeconds(loadDelay);
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("PortalSceneLoader：没有设置 targetSceneName。");
            yield break;
        }

        if (XRSceneTransferManager.Instance == null)
        {
            Debug.LogError("PortalSceneLoader：场景中没有 XRSceneTransferManager，无法执行 XR 对齐跳转。");
            yield break;
        }

        XRSceneTransferManager.Instance.LoadSceneWithPhysicalAlignment(
            targetSceneName,
            sceneYawOffset,
            XRSceneTransferManager.Instance.defaultPositionAlignMode,
            XRSceneTransferManager.Instance.defaultHeightMode
        );
    }
}