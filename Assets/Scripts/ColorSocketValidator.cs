using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ColorSocketValidator : XRSocketInteractor
{
    private ColorID _socketColorID;
    private Vector3 _originalScale = Vector3.one;
    private bool _hasStoredScale = false;

    protected override void Awake()
    {
        base.Awake();
        _socketColorID = GetComponent<ColorID>();
    }

    // 核心验证逻辑：判断一个物体是否符合选中的条件（颜色匹配）
    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        // 1. 调用基础判断
        if (!base.CanSelect(interactable)) return false;

        // 2. 检查颜色是否匹配
        return IsColorMatch(interactable.transform);
    }

    // 私有辅助方法，用于统一检查颜色
    private bool IsColorMatch(Transform target)
    {
        if (target == null) return false;

        ColorID ballColorID = target.GetComponent<ColorID>();
        if (ballColorID != null && _socketColorID != null)
        {
            return ballColorID.objectColor == _socketColorID.objectColor;
        }
        return false;
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);

        // --- 修复错误的关键点 ---
        // 将 IXRHoverInteractable 转换为 IXRSelectInteractable 以便调用 CanSelect
        if (args.interactableObject is IXRSelectInteractable selectInteractable)
        {
            if (CanSelect(selectInteractable))
            {
                if (!_hasStoredScale)
                {
                    _originalScale = args.interactableObject.transform.localScale;
                    _hasStoredScale = true;
                }
                args.interactableObject.transform.localScale = _originalScale * 0.9f;
            }
        }
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);

        if (_hasStoredScale)
        {
            args.interactableObject.transform.localScale = _originalScale;
            _hasStoredScale = false;
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (_hasStoredScale)
        {
            args.interactableObject.transform.localScale = _originalScale;
            _hasStoredScale = false;
        }

        Debug.Log($"<color=cyan>{args.interactableObject.transform.name} 匹配成功！</color>");
    }
}