using UnityEngine;

/// <summary>
/// UIStickToHMD
///
/// Places a world-space UI element so it follows the player's HMD (VR headset) / camera,
/// at a fixed distance and vertical offset. Useful for inventory menus or HUDs in VR.
///
/// ✅ Features:
/// - Automatically finds the camera (MainCamera or CenterEyeAnchor) if not assigned
/// - Positions UI at configurable distance and offset in front of the player
/// - Option to only follow yaw rotation (VR-friendly, prevents UI tilting with head roll)
/// - Smooth interpolation for position and rotation
/// - Ensures positive local scale to avoid mirrored rendering
///
/// ⚠️ Known Issues:
/// - The result is currently **bugged**: the UI sometimes appears mirrored in VR.
/// - This bug may be fixed in future versions; current workaround is only acceptable for testing.
/// </summary>
public class UIStickToHMD : MonoBehaviour
{
    [Header("HMD / Camera")]
    [Tooltip("Assign the VR camera transform (CenterEyeAnchor or Camera.main).")]
    public Transform cameraTransform;
    public bool autoFindCamera = true;

    [Header("Placement")]
    [Tooltip("Distance in meters in front of the camera.")]
    public float distance = 0.9f;
    [Tooltip("Vertical offset relative to eye level (negative = lower).")]
    public float verticalOffset = -0.05f;
    [Tooltip("If true, only yaw (horizontal) rotation is applied (prevents roll/pitch wobble).")]
    public bool yawOnly = true;
    [Tooltip("Smoothing factor for position and rotation updates.")]
    public float smooth = 14f;

    void Awake()
    {
        // Auto-find camera if none assigned
        if (autoFindCamera && !cameraTransform)
        {
            var cam = Camera.main;
            if (cam) cameraTransform = cam.transform;

            if (!cameraTransform)
            {
                var t = GameObject.Find("CenterEyeAnchor");
                if (t) cameraTransform = t.transform;
            }
        }

        // Ensure positive local scale (prevents mirror inversion issues)
        var ls = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
    }

    void LateUpdate()
    {
        if (!cameraTransform) return;

        // Forward and up vectors
        Vector3 fwd = cameraTransform.forward;
        Vector3 up = cameraTransform.up;

        if (yawOnly)
        {
            // Keep only horizontal component of forward
            fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            up = Vector3.up;
        }

        // Target position in front of the camera
        Vector3 targetPos = cameraTransform.position + fwd * distance + up * verticalOffset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            1f - Mathf.Exp(-smooth * Time.deltaTime));

        // Look towards the camera to ensure UI faces player
        Vector3 toCam = cameraTransform.position - transform.position;
        if (yawOnly)
        {
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-6f)
                toCam = fwd;
        }

        Quaternion want = Quaternion.LookRotation(toCam.normalized, up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            want,
            1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
