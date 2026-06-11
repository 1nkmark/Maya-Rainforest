using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MayaSocketVerifier : MonoBehaviour
{
    public string requiredTag = "Red"; // 目标标签
    public MeshRenderer platformRenderer; // 拖入RedPlatform

    private XRSocketInteractor socket;

    void Awake() => socket = GetComponent<XRSocketInteractor>();

    void OnEnable()
    {
        // 监听：当有东西被放进插槽
        socket.selectEntered.AddListener(CheckMatch);
        // 监听：当东西被拿走
        socket.selectExited.AddListener(ResetPlatform);
    }

    void CheckMatch(SelectEnterEventArgs args)
    {
        // 获取放进去的那个物体的标签
        if (args.interactableObject.transform.CompareTag(requiredTag))
        {
            Debug.Log("匹配正确！");
            platformRenderer.material.color = Color.green; // 成功变绿
        }
        else
        {
            Debug.Log("匹配错误！");
            platformRenderer.material.color = Color.black; // 失败变黑
        }
    }

    void ResetPlatform(SelectExitEventArgs args)
    {
        platformRenderer.material.color = Color.red; // 拿走后变回红色
    }
}