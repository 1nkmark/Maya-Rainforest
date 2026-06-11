using UnityEngine;

public class CalendarPuzzleManager : MonoBehaviour
{
    [Header("四个转盘的引用")]
    public Transform leftOuterDial;  // 神历外盘 (20日符)
    public Transform leftInnerDial;  // 神历内盘 (13数字)
    public Transform rightOuterDial; // 太阳历外盘 (19月符)
    public Transform rightInnerDial; // 太阳历内盘 (20数字)

    [Header("目标角度 (0-360度)")]
    public float targetLeftOuter = 90f;  // 假设 Ahau 符号在 90 度
    public float targetLeftInner = 180f; // 假设数字 4 在 180 度
    public float targetRightOuter = 45f; // 假设 Kumk'u 在 45 度
    public float targetRightInner = 0f;  // 假设数字 8 在 0 度

    [Header("容错范围 (度)")]
    public float tolerance = 10f; // 转到目标角度正负 10 度内都算对

    [Header("反馈指示器")]
    public MeshRenderer indicatorSphere;
    private bool isSolved = false;

    void Update()
    {
        // 如果已经解开了，就不再重复检测
        if (isSolved) return;

        // 检测四个盘子是否都到位了
        if (CheckDial(leftOuterDial, targetLeftOuter) &&
            CheckDial(leftInnerDial, targetLeftInner) &&
            CheckDial(rightOuterDial, targetRightOuter) &&
            CheckDial(rightInnerDial, targetRightInner))
        {
            PuzzleSolved();
        }
        else
        {
            // 如果转错了，保持红色
            if (indicatorSphere != null) indicatorSphere.material.color = Color.red;
        }
    }

    // 检查单个盘子角度的函数
    bool CheckDial(Transform dial, float targetAngle)
    {
        if (dial == null) return false;

        // 获取局部欧拉角
        Vector3 euler = dial.localEulerAngles;


        float currentAngle = euler.z;

        // 计算最短夹角差
        float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        return difference <= tolerance;
    }

    void PuzzleSolved()
    {
        isSolved = true;
        Debug.Log("<color=green>历法坐标对齐！创世之眼开启！</color>");

        if (indicatorSphere != null)
        {
            indicatorSphere.material.color = Color.green;
        }

        // 这里后续可以加上大门开启的动画、音效等
    }
}