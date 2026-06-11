using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotateSpeed = 60f;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // WASD ÒÆ¶¯
        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.dKey.isPressed) move += transform.right;

        transform.position += move * moveSpeed * Time.deltaTime;

        // QE ×óÓÒ×ªÏò
        if (keyboard.qKey.isPressed)
            transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0);
        if (keyboard.eKey.isPressed)
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
    }
}