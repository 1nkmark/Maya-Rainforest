using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotateSpeed = 200f; // 鼠标旋转速度

    private Vector2 lastMousePosition;
    private bool isDragging = false;

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null || keyboard == null) return;

        // ALT 是否按下
        bool altPressed = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;

        // ALT + 左键按下开始拖拽
        if (altPressed && mouse.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePosition = mouse.position.ReadValue();
        }

        // 左键抬起停止拖拽
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        // 拖拽旋转视角
        if (isDragging)
        {
            Vector2 mouseDelta = mouse.position.ReadValue() - lastMousePosition;
            float yaw = mouseDelta.x * rotateSpeed * Time.deltaTime;
            float pitch = -mouseDelta.y * rotateSpeed * Time.deltaTime;

            transform.Rotate(Vector3.up, yaw, Space.World); // 绕世界 Y 轴转
            transform.Rotate(Vector3.right, pitch, Space.Self); // 绕自身 X 轴转

            lastMousePosition = mouse.position.ReadValue();
        }

        // 可选：使用 WASD 移动摄像机
        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.qKey.isPressed) move += Vector3.down;
        if (keyboard.eKey.isPressed) move += Vector3.up;

        transform.position += move * moveSpeed * Time.deltaTime;
    }
}