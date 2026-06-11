using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))] // 自动挂载轻量级交互组件
public class MayaKnob : MonoBehaviour
{
    private XRSimpleInteractable interactable;
    private IXRSelectInteractor currentInteractor;

    private float initialControllerAngle;
    private float initialDialRotationZ;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrab);
        interactable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;

        // 使用父物体的静止坐标系计算手柄角度，防止盘子自身旋转干扰参考系
        Transform refTransform = transform.parent != null ? transform.parent : transform;
        Vector3 localPos = refTransform.InverseTransformPoint(currentInteractor.transform.position);

        // Atan2 计算抓取瞬间手柄在 XY 平面上的初始角度
        initialControllerAngle = Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg;
        initialDialRotationZ = transform.localEulerAngles.z;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
    }

    void Update()
    {
        if (currentInteractor != null)
        {
            Transform refTransform = transform.parent != null ? transform.parent : transform;
            Vector3 localPos = refTransform.InverseTransformPoint(currentInteractor.transform.position);
            float currentAngle = Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg;

            // 计算手柄转动的角度差值
            float angleDelta = currentAngle - initialControllerAngle;

            // 实时驱动盘子的 Z 轴旋转
            Vector3 newEuler = transform.localEulerAngles;
            newEuler.z = initialDialRotationZ + angleDelta;
            transform.localEulerAngles = newEuler;
        }
    }
}