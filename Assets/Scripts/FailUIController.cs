using UnityEngine;
using UnityEngine.SceneManagement;

public class FailUIController : MonoBehaviour
{
    // 再试一次：重新加载当前场景
    public void RetryLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    // 返回主页：加载主菜单场景
    public void BackToHome()
    {
        SceneManager.LoadScene("MainMenu");
    }
}