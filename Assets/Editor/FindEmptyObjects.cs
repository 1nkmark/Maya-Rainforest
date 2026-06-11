using UnityEngine;
using UnityEditor;

public class FindEmptyObjects : MonoBehaviour
{
    [MenuItem("Tools/Find Empty Objects")]
    static void FindAllEmpty()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int count = 0;
        foreach (GameObject obj in allObjects)
        {
            // 排除 Prefab 根对象或者隐藏物体，可根据需求修改
            if (obj.transform.childCount == 0 && obj.GetComponents<Component>().Length == 1)
            {
                Debug.Log("Empty Object: " + obj.name, obj);
                count++;
            }
        }
        Debug.Log("Total empty objects: " + count);
    }
}