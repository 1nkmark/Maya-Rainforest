using UnityEngine;

public class SmoothFollow : MonoBehaviour {
    public Transform targetCamera; // 拖入你的主相机
    public float distance = 2.0f;
    public float smoothSpeed = 5.0f;

    void Update() {
        // 计算目标位置：相机前方
        Vector3 targetPosition = targetCamera.position + targetCamera.forward * distance;
        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        // 始终面向玩家
        transform.LookAt(transform.position + targetCamera.forward);
    }
}