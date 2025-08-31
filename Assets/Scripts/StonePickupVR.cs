using UnityEngine;

/// <summary>
/// StonePickupVR
///
/// Handles picking up and throwing stones in VR.
/// 
/// ✅ Features:
/// - Pickup with button (A / Key E) → holds a stone at a fixed anchor in front of player
/// - Drop with the same button
/// - Throw with right trigger (charge mode or gesture mode)
/// - Supports optional arc preview with LineRenderer
/// - Integrates with <see cref="ControllerHandCurl"/> for hand pose feedback
/// - Provides short haptic buzz feedback on pickup and throw
///
/// Usage:
/// - Attach this to the right-hand controller (aimOrigin).
/// - Assign holdAnchor (where the stone will be held, e.g. in front of the camera).
/// - Assign trackingSpace (OVRCameraRig/TrackingSpace) if using gesture throws.
/// - Stones must have a <see cref="ThrowableStone"/> component.
/// </summary>
public class StonePickupVR : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Controller origin (e.g., RightControllerAnchor).")]
    public Transform aimOrigin;
    [Tooltip("Anchor where held stones are placed (e.g., ViewHoldAnchor).")]
    public Transform holdAnchor;
    [Tooltip("Tracking space (OVRCameraRig/TrackingSpace) used to convert local to world velocity for gesture throws).")]
    public Transform trackingSpace;
    [Tooltip("Layer mask for stones.")]
    public LayerMask stoneLayer;

    [Header("Pickup")]
    [Tooltip("Max pickup distance for raycast.")]
    public float pickDistance = 3f;
    [Tooltip("Sphere radius for near pickup test.")]
    public float sphereRadius = 0.2f;
    [Tooltip("Button for pickup/drop (default: OVRInput.Button.One → A).")]
    public OVRInput.Button pickupButton = OVRInput.Button.One;
    [Tooltip("Key for pickup/drop testing in editor.")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Throw (charge mode)")]
    [Tooltip("Button to hold for charging throw (default: Right Trigger).")]
    public OVRInput.Button throwHoldButton = OVRInput.Button.SecondaryIndexTrigger;
    [Tooltip("Min throw speed when barely charged.")]
    public float minThrowSpeed = 6f;
    [Tooltip("Max throw speed when fully charged.")]
    public float maxThrowSpeed = 18f;
    [Tooltip("Seconds to fully charge from min to max speed.")]
    public float chargeTime = 1.0f;
    [Tooltip("If true, ignore charge and use controller gesture velocity for throw.")]
    public bool useGestureThrow = false;

    [Header("Gesture tuning (only if useGestureThrow)")]
    [Tooltip("Minimum speed when throwing via gesture.")]
    public float gestureMinSpeed = 4f;
    [Tooltip("Maximum speed when throwing via gesture.")]
    public float gestureMaxSpeed = 16f;
    [Tooltip("Extra boost added forward to stabilize gesture throws.")]
    public float forwardBoost = 2f;

    private ThrowableStone held;
    private float chargeT = 0f;
    private bool charging = false;

    [Header("Arc Preview")]
    [Tooltip("If true, shows an arc trajectory preview while charging.")]
    public bool showArc = true;
    [Tooltip("LineRenderer used for arc preview.")]
    public LineRenderer arcLine;
    [Tooltip("Number of segments used for arc preview.")]
    public int arcResolution = 24;
    [Tooltip("Preview speed when using gesture mode.")]
    public float previewSpeed = 14f;

    [Header("Hand Pose Link")]
    [Tooltip("Optional: hand curl driver for animating grip/trigger.")]
    public ControllerHandCurl handCurl;
    [Tooltip("Grip target when picking up a stone.")]
    public float gripOnPickup = 1f;
    [Tooltip("Grip target when releasing a stone.")]
    public float gripOnRelease = 0f;
    [Tooltip("If true, trigger pressure drives index finger curl.")]
    public bool driveTriggerToIndex = true;

    void Reset()
    {
        // Auto-assign stone layer if available
        if (stoneLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Stone");
            if (idx >= 0) stoneLayer = 1 << idx;
        }
    }

    void Update()
    {
        // --- Pickup / Drop ---
        bool pickPressed = OVRInput.GetDown(pickupButton) || Input.GetKeyDown(pickupKey);
        if (pickPressed)
        {
            if (held == null) TryPickup();
            else Drop();
        }

        // --- Charging & Throw ---
        bool holdTrig = OVRInput.Get(throwHoldButton);
        bool releaseTrig = OVRInput.GetUp(throwHoldButton);

        // Drive index finger if linked
        if (driveTriggerToIndex && handCurl != null)
            handCurl.SetIndexAdd(holdTrig ? 1f : 0f);

        if (held != null && holdTrig)
        {
            // Charging
            charging = true;
            if (!useGestureThrow)
                chargeT = Mathf.Clamp01(chargeT + Time.deltaTime / chargeTime);

            // Arc preview
            if (showArc && arcLine)
            {
                float speedPreview = useGestureThrow
                    ? previewSpeed
                    : Mathf.Lerp(minThrowSpeed, maxThrowSpeed, chargeT);
                Vector3 v0 = aimOrigin.forward * speedPreview;
                DrawArc(aimOrigin.position, v0);
            }
        }
        else
        {
            if (arcLine && arcLine.enabled) arcLine.enabled = false;
        }

        if (held != null && charging && releaseTrig)
        {
            charging = false;
            if (arcLine) arcLine.enabled = false;
            Throw();
        }
    }

    /// <summary>
    /// Attempts to pick up a nearby stone by raycast or overlap sphere.
    /// </summary>
    void TryPickup()
    {
        // 1) Raycast directly forward
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickDistance, stoneLayer, QueryTriggerInteraction.Ignore))
        {
            var stone = hit.collider.GetComponentInParent<ThrowableStone>();
            if (stone != null && !stone.IsHeld) { Pickup(stone); return; }
        }

        // 2) Overlap sphere slightly in front
        Collider[] hits = Physics.OverlapSphere(
            aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f),
            sphereRadius, stoneLayer, QueryTriggerInteraction.Ignore);

        ThrowableStone best = null;
        float bestDot = 0.75f;
        for (int i = 0; i < hits.Length; i++)
        {
            var s = hits[i].GetComponentInParent<ThrowableStone>();
            if (!s || s.IsHeld) continue;
            Vector3 to = (s.transform.position - aimOrigin.position).normalized;
            float d = Vector3.Dot(aimOrigin.forward, to);
            if (d > bestDot) { bestDot = d; best = s; }
        }
        if (best != null) Pickup(best);
    }

    /// <summary>
    /// Executes pickup: parents the stone to hold anchor and triggers feedback.
    /// </summary>
    void Pickup(ThrowableStone stone)
    {
        held = stone;
        stone.PickUp(holdAnchor);
        chargeT = 0f;
        charging = false;

        if (handCurl) handCurl.SetGripTarget(gripOnPickup);

        TryBuzz(0.1f, 0.25f, 0.08f);
    }

    /// <summary>
    /// Drops the currently held stone without throwing.
    /// </summary>
    void Drop()
    {
        if (held == null) return;
        held.Drop();
        held = null;
        chargeT = 0f;
        charging = false;

        if (handCurl) { handCurl.SetGripTarget(gripOnRelease); handCurl.SetIndexAdd(0f); }
    }

    /// <summary>
    /// Throws the held stone using either charged speed or controller velocity.
    /// </summary>
    void Throw()
    {
        charging = false;

        Vector3 velocity;
        if (useGestureThrow)
        {
            // Local controller velocity → world
            Vector3 vLocal = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            Transform ts = trackingSpace ? trackingSpace : aimOrigin;
            Vector3 vWorld = ts.TransformVector(vLocal);

            // Add slight forward boost
            vWorld += aimOrigin.forward * forwardBoost;

            float mag = Mathf.Clamp(vWorld.magnitude, gestureMinSpeed, gestureMaxSpeed);
            velocity = vWorld.normalized * mag;
        }
        else
        {
            float speed = Mathf.Lerp(minThrowSpeed, maxThrowSpeed, chargeT);
            velocity = aimOrigin.forward * speed;
        }

        held.Throw(velocity, aimOrigin);
        held = null;
        chargeT = 0f;

        if (handCurl) { handCurl.SetGripTarget(gripOnRelease); handCurl.SetIndexAdd(0f); }

        TryBuzz(0.05f, 0.4f, 0.06f);
    }

    void TryBuzz(float f, float a, float dur)
    {
        try { OVRInput.SetControllerVibration(f, a, OVRInput.Controller.RTouch); } catch { }
        Invoke(nameof(StopBuzz), dur);
    }

    void StopBuzz()
    {
        try { OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch); } catch { }
    }

    void OnDrawGizmosSelected()
    {
        if (!aimOrigin) return;
        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.DrawLine(aimOrigin.position, aimOrigin.position + aimOrigin.forward * pickDistance);
        Gizmos.DrawWireSphere(aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f), sphereRadius);
    }

    /// <summary>
    /// Draws an arc trajectory preview using physics gravity.
    /// </summary>
    void DrawArc(Vector3 startPos, Vector3 startVel)
    {
        if (!arcLine) return;
        arcLine.positionCount = arcResolution;

        Vector3 p = startPos;
        Vector3 v = startVel;
        float dt = Time.fixedDeltaTime;

        for (int i = 0; i < arcResolution; i++)
        {
            arcLine.SetPosition(i, p);
            v += Physics.gravity * dt;
            p += v * dt;
        }
        arcLine.enabled = true;
    }
}
