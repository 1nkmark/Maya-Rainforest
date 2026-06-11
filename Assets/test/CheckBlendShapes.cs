using UnityEngine;

public class CheckBlendShapes : MonoBehaviour
{
    public SkinnedMeshRenderer smr;

    void Start()
    {
        if (smr == null)
        {
            Debug.Log("没有指定 SkinnedMeshRenderer");
            return;
        }

        Mesh mesh = smr.sharedMesh;
        if (mesh == null)
        {
            Debug.Log("没有找到 Mesh");
            return;
        }

        Debug.Log("当前检查对象: " + smr.name);
        Debug.Log("Mesh 名称: " + mesh.name);
        Debug.Log("BlendShape 数量: " + mesh.blendShapeCount);

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            Debug.Log($"BlendShape {i}: {mesh.GetBlendShapeName(i)}");
        }
    }
}