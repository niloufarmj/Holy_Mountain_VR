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
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick); // آنالوگ سمت چپ

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * input.y + right * input.x;

        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
    }
}
