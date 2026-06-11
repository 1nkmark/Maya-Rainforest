using UnityEngine;
using System.Collections.Generic;

public class MayaOrbitBillboarding : MonoBehaviour
{
    [Header("中心追踪 (将 Main Camera 拖入)")]
    public Transform playerHead;
    public float heightOffset = -0.2f;

    [Header("轨道控制")]
    public float orbitRadius = 1.0f;    // 转动半径
    public float rotationSpeed = 30f;   // 转动速度 (度/秒)

    private float currentAngle = 0f;

    void Update()
    {
        if (playerHead == null) return;

        // 1. 圆心随动：Manager 坐标永远等于头的位置
        transform.position = playerHead.position + new Vector3(0, heightOffset, 0);

        // 2. 更新角度
        currentAngle += rotationSpeed * Time.deltaTime;

        // 3. 计算并设置每个球的位置和旋转
        int childCount = transform.childCount;
        if (childCount == 0) return;

        // 均匀分布每个球的角度间隔
        float angleStep = 360f / childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // 如果球被手柄抓走了，它会脱离父子关系，就不再受下面代码控制
            // 注意：确保 XR Grab Interactable 的 Attach 逻辑正常

            // 计算该球当前的弧度
            float radians = (currentAngle + (i * angleStep)) * Mathf.Deg2Rad;

            // 使用三角函数计算圆形轨迹上的局部坐标 (X, Z 平面)
            float x = Mathf.Cos(radians) * orbitRadius;
            float z = Mathf.Sin(radians) * orbitRadius;

            // 设置球的位置
            child.localPosition = new Vector3(x, 0, z);

            // 4. 【核心需求】始终朝向玩家
            // 让球的“前方”始终指向玩家头部
            child.LookAt(playerHead.position);

            // 如果你的玛雅文字是贴在球上的，而 LookAt 让它背面朝你了
            // 请取消下面这一行的注释来修正 180 度翻转
            // child.Rotate(0, 180, 0); 
        }
    }
}