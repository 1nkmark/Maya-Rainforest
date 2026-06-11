using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;

public class CheckXR : MonoBehaviour
{
    void Update()
    {
        var origin = FindObjectOfType<XROrigin>();
        if (origin != null)
        {
            // 在控制台打印：
            // 1. 我们想要的模式
            // 2. 硬件当前实际运行的模式
            Debug.Log($"[追踪诊断] 请求模式: {origin.RequestedTrackingOriginMode}, 实际运行模式: {origin.CurrentTrackingOriginMode}");
        }
    }
}