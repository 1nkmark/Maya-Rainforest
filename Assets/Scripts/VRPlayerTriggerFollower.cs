using UnityEngine;

public class VRPlayerTriggerFollower : MonoBehaviour
{
    [Header("References")]
    public Transform headCamera;

    [Header("Follow Settings")]
    public bool followY = false;
    public float fixedY = 1.0f;

    private void LateUpdate()
    {
        if (headCamera == null) return;

        Vector3 pos = headCamera.position;

        if (!followY)
        {
            pos.y = fixedY;
        }

        transform.position = pos;
    }
}