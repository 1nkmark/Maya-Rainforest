using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform cameraTransform;
    public bool onlyWhenGreeting = true; 
    bool hasLogged = false; 

    private Animator animator;

    void Start()
    {
        // Get the main camera's transform and the Animator component
        cameraTransform = Camera.main.transform;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Exit if there is no Animator Controller assigned
        if (animator.runtimeAnimatorController == null) return;

        if (onlyWhenGreeting)
        {
            // Only rotate if the "isGreeting" parameter is true
            if (!animator.GetBool("isGreeting")) return;
            if (!hasLogged)
            {
                hasLogged = true;
                // Debug.Log("���к����泯���?");
            }
        }

        // Rotate only around the Y axis, keeping the character upright
        Vector3 direction = cameraTransform.position - transform.position;
        direction.y = 0; // Ignore height difference

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Smoothly rotate towards the target with a speed of 5
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }
}