using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class UniversalRayDial : MonoBehaviour
{
    private XRSimpleInteractable interactable;
    private XRRayInteractor currentRayInteractor;
    private bool isLocked;

    [Header("旋转设置")]
    [Tooltip("圆盘绕着哪根轴转？通常圆柱体选 Up (Y轴) 或 Forward (Z轴)")]
    public Vector3 localRotationAxis = Vector3.up;

    private Vector3 initialHitVector;
    private Quaternion initialObjectRotation;
    private Vector3 cachedWorldAxis; // 【新增】缓存初始时的世界空间旋转轴

    public bool IsLocked => isLocked;
    public bool IsBeingManipulated => currentRayInteractor != null;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrabStarted);
        interactable.selectExited.AddListener(OnGrabEnded);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrabStarted);
        interactable.selectExited.RemoveListener(OnGrabEnded);
    }

    private void OnGrabStarted(SelectEnterEventArgs args)
    {
        if (isLocked)
            return;

        // 确保交互者是射线检测
        if (args.interactorObject is XRRayInteractor ray)
        {
            currentRayInteractor = ray;
            if (currentRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                // 1. 记录初始物体旋转角度
                initialObjectRotation = transform.rotation;

                // 2. 【核心修改】在抓取的一瞬间，锁定旋转轴到世界空间
                // 这样在 Update 过程中，旋转轴不会随着物体的转动而偏移
                cachedWorldAxis = transform.TransformDirection(localRotationAxis).normalized;

                // 3. 计算初始撞击向量：从中心指向撞击点的向量
                Vector3 hitVec = hit.point - transform.position;

                // 4. 将向量投影到旋转平面上（基于固定的缓存轴）
                initialHitVector = Vector3.ProjectOnPlane(hitVec, cachedWorldAxis).normalized;
            }
        }
    }

    private void OnGrabEnded(SelectExitEventArgs args)
    {
        currentRayInteractor = null;
        cachedWorldAxis = Vector3.zero;
    }

    void Update()
    {
        if (isLocked)
            return;

        // 只有在按下按键并移动射线时计算旋转
        if (currentRayInteractor != null && currentRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // 1. 获取当前的撞击向量
            Vector3 currentHitVec = hit.point - transform.position;

            // 2. 将当前的撞击向量投影到预先缓存好的旋转平面上
            Vector3 currentHitVectorOnPlane = Vector3.ProjectOnPlane(currentHitVec, cachedWorldAxis).normalized;

            // 3. 计算从【初始位置】到【当前位置】的夹角 (带正负号)
            // 使用 cachedWorldAxis 确保夹角的正负逻辑在整个旋转过程中保持一致
            float angleDelta = Vector3.SignedAngle(initialHitVector, currentHitVectorOnPlane, cachedWorldAxis);

            // 4. 应用旋转：在初始旋转的基础上，绕着固定的轴进行旋转
            // 这避免了物体转动导致轴向偏移的“反馈环路”错误
            transform.rotation = Quaternion.AngleAxis(angleDelta, cachedWorldAxis) * initialObjectRotation;
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (locked)
        {
            currentRayInteractor = null;
            cachedWorldAxis = Vector3.zero;
        }

        if (interactable != null)
        {
            interactable.enabled = !locked;
        }
    }
}
