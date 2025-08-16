using UnityEngine;

/// <summary>
/// Handles VR (and keyboard) pickup and drop of stones.
/// - Primary input: Oculus A button (configurable).
/// - PC test input: keyboard key (default: E).
/// - Picks up by raycasting from a controller; if nothing is hit,
///   falls back to choosing the most forward stone inside a small sphere
///   ahead of the controller.
/// - On pickup, parents the stone to a hold anchor and triggers brief haptics.
/// </summary>
public class StonePickupVR : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Ray origin, typically the right controller transform (e.g., RightControllerAnchor).")]
    public Transform aimOrigin;

    [Tooltip("Where the held stone should be attached (e.g., a point in front of the view).")]
    public Transform holdAnchor;

    [Tooltip("Physics layer used to identify stones (should be set to 'Stone').")]
    public LayerMask stoneLayer;

    [Header("Pickup Settings")]
    [Tooltip("Maximum distance for direct raycast pickup.")]
    public float pickDistance = 3f;

    [Tooltip("Radius for the fallback sphere check (a small cone-like assist in front).")]
    public float sphereRadius = 0.2f;

    [Tooltip("VR pickup button (default: A on Oculus Touch).")]
    public OVRInput.Button pickupButton = OVRInput.Button.One;

    [Tooltip("Keyboard fallback for PC testing (default: E).")]
    public KeyCode pickupKey = KeyCode.E;

    // Currently held stone (if any)
    private ThrowableStone held;

    private void Reset()
    {
        // If layer mask wasn't set in the Inspector, attempt to auto-assign the 'Stone' layer.
        if (stoneLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Stone");
            if (idx >= 0) stoneLayer = 1 << idx;
        }
    }

    private void Update()
    {
        // Toggle behavior on press: pick up if free, otherwise drop.
        bool pressed = OVRInput.GetDown(pickupButton) || Input.GetKeyDown(pickupKey);
        if (!pressed) return;

        if (held == null) TryPickup();
        else Drop();
    }

    /// <summary>
    /// Attempts to pick up a stone by:
    /// 1) Raycasting straight ahead from the controller.
    /// 2) If nothing is hit, selecting the most forward stone inside a small sphere ahead.
    /// </summary>
    private void TryPickup()
    {
        // 1) Direct raycast from controller
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickDistance, stoneLayer, QueryTriggerInteraction.Ignore))
        {
            var stone = hit.collider.GetComponentInParent<ThrowableStone>();
            if (stone != null && !stone.IsHeld)
            {
                Pickup(stone);
                return;
            }
        }

        // 2) Fallback: find the most forward stone in a small sphere ahead
        Collider[] hits = Physics.OverlapSphere(
            aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f),
            sphereRadius,
            stoneLayer,
            QueryTriggerInteraction.Ignore
        );

        ThrowableStone best = null;
        float bestDot = 0.75f; // Only consider objects mostly in front

        for (int i = 0; i < hits.Length; i++)
        {
            var s = hits[i].GetComponentInParent<ThrowableStone>();
            if (s == null || s.IsHeld) continue;

            Vector3 to = (s.transform.position - aimOrigin.position).normalized;
            float d = Vector3.Dot(aimOrigin.forward, to);
            if (d > bestDot)
            {
                bestDot = d;
                best = s;
            }
        }

        if (best != null) Pickup(best);
        // If none found, do nothing.
    }

    /// <summary>
    /// Parents the stone to the hold anchor, resets local rotation, and fires a brief haptic pulse.
    /// </summary>
    private void Pickup(ThrowableStone stone)
    {
        held = stone;
        stone.PickUp(holdAnchor);

        // Optional: set a neutral local rotation for nicer presentation
        held.transform.localRotation = Quaternion.identity;

        // Optional short haptic feedback on right controller
        try { OVRInput.SetControllerVibration(0.1f, 0.2f, OVRInput.Controller.RTouch); } catch {}
        Invoke(nameof(StopHaptics), 0.08f);
    }

    /// <summary>
    /// Drops the currently held stone (if any).
    /// </summary>
    private void Drop()
    {
        if (held == null) return;
        held.Drop();
        held = null;
    }

    /// <summary>
    /// Stops any ongoing haptics on the right controller.
    /// </summary>
    private void StopHaptics()
    {
        try { OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch); } catch {}
    }

    private void OnDrawGizmosSelected()
    {
        if (!aimOrigin) return;
        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.DrawLine(aimOrigin.position, aimOrigin.position + aimOrigin.forward * pickDistance);
        Gizmos.DrawWireSphere(aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f), sphereRadius);
    }
}
