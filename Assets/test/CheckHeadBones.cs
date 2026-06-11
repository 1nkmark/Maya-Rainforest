using UnityEngine;

public class FindFaceBones : MonoBehaviour
{
    public SkinnedMeshRenderer headRenderer;

    void Start()
    {
        if (headRenderer == null)
        {
            Debug.Log("没有指定 headRenderer");
            return;
        }

        Debug.Log("===== 开始筛选头部/嘴部相关骨骼 =====");

        string[] keywords = new string[]
        {
            "head", "Head",
            "jaw", "Jaw",
            "mouth", "Mouth",
            "face", "Face",
            "neck", "Neck",
            "tongue", "Tongue",
            "lip", "Lip",
            "teeth", "Teeth",
            "eye", "Eye",
            "舌", "牙", "嘴", "头", "脸", "颈"
        };

        for (int i = 0; i < headRenderer.bones.Length; i++)
        {
            Transform b = headRenderer.bones[i];
            if (b == null) continue;

            string boneName = b.name;
            bool matched = false;

            foreach (string key in keywords)
            {
                if (boneName.Contains(key))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                Debug.Log($"Bone {i}: {boneName} | 路径: {GetPath(b)}");
            }
        }

        Debug.Log("===== 筛选结束 =====");
    }

    string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}