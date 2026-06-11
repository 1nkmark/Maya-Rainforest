using UnityEngine;

public class AutoDisableSimulator : MonoBehaviour
{
    void Awake()
    {
        // 如果不是在编辑器里运行（即在真机上），直接关掉自己
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }
}