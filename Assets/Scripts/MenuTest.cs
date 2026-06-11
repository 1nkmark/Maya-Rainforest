using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuTest : MonoBehaviour
{
    public void OnClickStart()
    {
        SceneManager.LoadScene("Prologue");
        //SceneManager.LoadScene("SampleScene");
    }

    public void OnClickContinue()
    {
        SceneManager.LoadScene("FirstSceneNew");
    }

    public void OnClickSettings()
    {
        //Debug.Log("settings");
        SceneManager.LoadScene("SecondScene");
    }

    public void OnClickProfile()
    {
        SceneManager.LoadScene("ThirdScene");
    }
}