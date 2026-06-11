using UnityEngine;

public class ThermalTarget : MonoBehaviour
{
    [Header("材质设置")]
    public Material normalMaterial;
    public Material thermalMaterial;

    [Header("是否启动时自动收集子物体Renderer")]
    public bool autoFindRenderers = true;

    [Header("调试")]
    public bool logDebug = true;

    private Renderer[] cachedRenderers;
    private bool isThermalOn = false;

    private void Awake()
    {
        if (autoFindRenderers)
        {
            CacheRenderers();
        }
    }

    /// <summary>
    /// 自动缓存当前物体以及所有子物体上的 Renderer
    /// </summary>
    public void CacheRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if (logDebug)
        {
            Debug.Log($"[ThermalTarget] {gameObject.name} 找到 Renderer 数量: {cachedRenderers.Length}", this);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    Debug.Log($"[ThermalTarget] Renderer #{i}: {cachedRenderers[i].name}", cachedRenderers[i]);
                }
            }
        }
    }

    /// <summary>
    /// 开启/关闭红外材质
    /// </summary>
    public void SetThermal(bool enabled)
    {
        isThermalOn = enabled;

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (normalMaterial == null || thermalMaterial == null)
        {
            Debug.LogWarning($"[ThermalTarget] {gameObject.name} 没有设置 normalMaterial 或 thermalMaterial", this);
            return;
        }

        foreach (Renderer r in cachedRenderers)
        {
            if (r == null) continue;

            int materialCount = r.sharedMaterials.Length;
            Material targetMat = enabled ? thermalMaterial : normalMaterial;

            Material[] newMats = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                newMats[i] = targetMat;
            }

            r.materials = newMats;
        }

        if (logDebug)
        {
            Debug.Log($"[ThermalTarget] {gameObject.name} 红外状态切换为: {enabled}", this);
        }
    }

    /// <summary>
    /// 切换当前状态
    /// </summary>
    public void ToggleThermal()
    {
        SetThermal(!isThermalOn);
    }

    /// <summary>
    /// 编辑器里右键菜单测试：开启红外
    /// </summary>
    [ContextMenu("Test Thermal ON")]
    private void TestThermalOn()
    {
        SetThermal(true);
    }

    /// <summary>
    /// 编辑器里右键菜单测试：关闭红外
    /// </summary>
    [ContextMenu("Test Thermal OFF")]
    private void TestThermalOff()
    {
        SetThermal(false);
    }
}