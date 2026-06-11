using UnityEngine;
using UnityEngine.SceneManagement; // 用于以后实现“重启”功能

public class DeathUIManager : MonoBehaviour
{
    public static DeathUIManager Instance;
    public GameObject deathUIPanel; // 在 Inspector 中拖入你的 UI 面板

    void Awake()
    {
        Instance = this;
        if (deathUIPanel != null) deathUIPanel.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(true);
            
            // 停止物理模拟（可选）
            // Time.timeScale = 0; 
            
            // 显示鼠标以便点击 UI 按钮
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}