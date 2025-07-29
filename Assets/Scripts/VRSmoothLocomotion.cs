using UnityEngine;

public class VRSmoothLocomotion : MonoBehaviour
{
    public float speed = 3.0f;
    public Transform cameraTransform;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Read input from left thumbstick
        Vector2 input = Vector2.zero;
        if (OVRInput.IsControllerConnected(OVRInput.Controller.LTouch))
        {
            input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        }
        else
        {
            // If no VR controller connected, use keyboard input (WASD or Arrow Keys)
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
        }

        // Calculate forward and right directions relative to camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        Vector3 moveDirection = forward * input.y + right * input.x;

        // Apply movement to rigidbody
        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
    }
}
