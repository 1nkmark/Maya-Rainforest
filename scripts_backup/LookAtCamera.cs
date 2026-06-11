using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform cameraTransform;
    public bool onlyWhenGreeting = true; // 只在打招呼时朝向相机
    bool hasLogged = false; // 加在类里

    private Animator animator;

    void Start()
    {
        // VR里直接找主相机
        cameraTransform = Camera.main.transform;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator.runtimeAnimatorController == null) return;

        if (onlyWhenGreeting)
        {
            // 只在打招呼动画时才转向
            if (!animator.GetBool("isGreeting")) return;
            if (!hasLogged)
            {
                hasLogged = true;
                Debug.Log("打招呼：面朝玩家");
            }
        }

        // 计算朝向（只转Y轴，不会让角色仰头低头）
        Vector3 direction = cameraTransform.position - transform.position;
        direction.y = 0; // 忽略高度差

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // 平滑转向，5f是速度，可以调大调小
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }
}