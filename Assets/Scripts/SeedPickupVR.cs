// SeedPickupVR.cs
// Attach one instance to each VR controller (e.g., RightControllerAnchor, LeftControllerAnchor).
// Allows the player to pick up seeds by pressing A (right) or X (left).
// Collected seeds are removed from the scene and added to GameStats.
// Optional: integrates with ControllerHandCurl to briefly animate a gripping hand pose.

using UnityEngine;

/// <summary>
/// Handles seed pickup interaction for VR controllers.
/// 
/// ✅ Features:
/// - Detects nearby seeds using overlap sphere or a short forward ray.
/// - Collects seeds on button press (A on right / X on left) or 'E' key for testing.
/// - Updates <see cref="GameStats"/> when a seed is collected.
/// - Plays haptic feedback on the active controller.
/// - Optionally pulses a hand pose using <see cref="ControllerHandCurl"/> for visual feedback.
/// 
/// Usage:
/// - Attach this to LeftControllerAnchor and RightControllerAnchor.
/// - Assign <see cref="aimOrigin"/> to the controller transform (usually itself).
/// - Ensure seed prefabs are on the correct <see cref="seedLayer"/>.
/// </summary>
public class SeedPickupVR : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform used as the center for detection (usually the controller).")]
    public Transform aimOrigin;

    [Tooltip("Which controller this script belongs to (LTouch/RTouch).")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Detection")]
    [Tooltip("Layer mask that seeds belong to.")]
    public LayerMask seedLayer;
    [Tooltip("Radius around the controller considered as 'touch distance'.")]
    public float touchRadius = 0.18f;
    [Tooltip("If no seed is within touch radius, cast a short ray forward up to this distance.")]
    public float reachDistance = 0.6f;

    [Header("Input")]
    [Tooltip("Collect button (default: OVRInput.Button.One → A on right / X on left).")]
    public OVRInput.Button collectButton = OVRInput.Button.One;
    [Tooltip("Fallback input for desktop testing.")]
    public KeyCode collectKey = KeyCode.E;

    [Header("Debug")]
    [Tooltip("Draw gizmos for overlap sphere and ray in the scene view.")]
    public bool debugDraw = false;

    private GameStats _stats;

    [Header("Hand Pose Link")]
    [Tooltip("Optional: link to ControllerHandCurl for hand closing animation.")]
    public ControllerHandCurl handCurl;
    [Tooltip("Grip target applied on pickup (0 = open, 1 = fist).")]
    public float collectPulseValue = 0.95f;
    [Tooltip("Time to hold the grip before resetting.")]
    public float collectPulseHold = 0.2f;
    [Tooltip("Time to return to open hand after hold.")]
    public float collectPulseBack = 0.3f;

    void Reset()
    {
        aimOrigin = transform;

        // Default to "Seed" layer if it exists, otherwise allow all layers
        if (seedLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Seed");
            if (idx >= 0) seedLayer = 1 << idx;
            else seedLayer = ~0;
        }

        // Auto-detect controller type by object name
        if (name.ToLower().Contains("left")) controller = OVRInput.Controller.LTouch;
        if (name.ToLower().Contains("right")) controller = OVRInput.Controller.RTouch;
    }

    void Awake()
    {
        if (!aimOrigin) aimOrigin = transform;
        _stats = FindObjectOfType<GameStats>();
    }

    void Update()
    {
        bool pressed = OVRInput.GetDown(collectButton) || Input.GetKeyDown(collectKey);
        if (!pressed) return;

        // Try to find a seed nearby
        var seed = FindBestSeed();
        if (!seed) return;

        // Attempt collection
        if (seed.TryCollect(_stats))
        {
            Buzz(0.1f, 0.35f, 0.07f);

            // Animate grip using handCurl if available
            if (handCurl)
            {
                handCurl.SetGripTarget(collectPulseValue);
                Invoke(nameof(ResetHand), collectPulseHold + collectPulseBack);
            }

            if (debugDraw)
                Debug.Log($"[SeedPickupVR] Collected seed proto={seed.prototypeIndex} by {controller}");
        }
    }

    /// <summary>
    /// Resets the hand grip animation to open hand.
    /// </summary>
    private void ResetHand()
    {
        if (handCurl) handCurl.SetGripTarget(0f);
    }

    /// <summary>
    /// Finds the closest valid seed, first checking within a radius, then with a short ray.
    /// </summary>
    private SeedCollectible FindBestSeed()
    {
        // 1) Overlap sphere around controller
        Collider[] hits = Physics.OverlapSphere(
            aimOrigin.position,
            touchRadius,
            seedLayer,
            QueryTriggerInteraction.Collide);

        SeedCollectible best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            var s = hits[i].GetComponentInParent<SeedCollectible>();
            if (!s) continue;

            float d = Vector3.SqrMagnitude(hits[i].transform.position - aimOrigin.position);
            if (d < bestDist) { bestDist = d; best = s; }
        }

        if (best != null) return best;

        // 2) Forward ray if no seed is touching
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, seedLayer, QueryTriggerInteraction.Collide))
        {
            return hit.collider.GetComponentInParent<SeedCollectible>();
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (!debugDraw || !aimOrigin) return;

        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(aimOrigin.position, touchRadius);

        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.DrawLine(aimOrigin.position, aimOrigin.position + aimOrigin.forward * reachDistance);
    }

    /// <summary>
    /// Trigger controller vibration (haptic feedback).
    /// </summary>
    private void Buzz(float frequency, float amplitude, float duration)
    {
        try { OVRInput.SetControllerVibration(frequency, amplitude, controller); } catch { }
        Invoke(nameof(StopBuzz), duration);
    }

    private void StopBuzz()
    {
        try { OVRInput.SetControllerVibration(0, 0, controller); } catch { }
    }
}
