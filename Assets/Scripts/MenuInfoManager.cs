using UnityEngine;
using TMPro;

public class MenuInfoManager : MonoBehaviour
{
    public TextMeshProUGUI pingText; // 拖入显示延迟的文字
    public TextMeshProUGUI versionText; // 拖入显示版本的文字

    void Start()
    {
        versionText.text = "v" + Application.version + " (Beta)";
    }

    void Update()
    {
        // 让毫秒数在 20-30 之间随机跳动
        if (Time.frameCount % 100 == 0)
        { // 每30帧更新一次，防止闪得太快
            pingText.text = Random.Range(20, 30).ToString() + "ms";
        }
    }
}