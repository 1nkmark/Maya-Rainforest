using UnityEngine;
using Unity.XR.CoreUtils;

public class XRSceneSpawnAnchor : MonoBehaviour
{
    [Header("当前 Scene 的 XR Origin")]
    public XROrigin xrOrigin;

    [Header("玩家真实站位锚点")]
    public Transform playerPhysicalAnchor;

    private void Reset()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        playerPhysicalAnchor = transform;
    }

    private void Awake()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        if (playerPhysicalAnchor == null)
        {
            playerPhysicalAnchor = transform;
        }
    }
}