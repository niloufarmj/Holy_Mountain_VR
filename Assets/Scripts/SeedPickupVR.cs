// SeedPickupVR.cs
// Attach one instance to each controller (RightControllerAnchor, LeftControllerAnchor).
// Press OVRInput.Button.One (A on right / X on left) to collect a nearby seed.
// Nothing is held; the seed is removed and GameStats is updated.

using UnityEngine;

public class SeedPickupVR : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Usually set this to the controller transform itself.")]
    public Transform aimOrigin;

    [Tooltip("Optional. Used for choosing which controller to buzz.")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Detection")]
    [Tooltip("Layer that your seed meshes/colliders are on.")]
    public LayerMask seedLayer;
    [Tooltip("Radius around controller to consider as 'touch'.")]
    public float touchRadius = 0.18f;
    [Tooltip("If no seed in touch radius, try a short forward ray to this distance.")]
    public float reachDistance = 0.6f;

    [Header("Input")]
    [Tooltip("Use the same button as stones: OVRInput.Button.One (A on R / X on L).")]
    public OVRInput.Button collectButton = OVRInput.Button.One;
    [Tooltip("Desktop testing key.")]
    public KeyCode collectKey = KeyCode.E;

    [Header("Debug")]
    public bool debugDraw = false;

    private GameStats _stats;

    [Header("Hand Pose Link")]
    public ControllerHandCurl handCurl;
    public float collectPulseValue = 0.95f;    // Almost full fist (was 0.6f)
    public float collectPulseHold = 0.2f;      // Longer hold to see the animation
    public float collectPulseBack = 0.3f;      // Slower return

    void Reset()
    {
        aimOrigin = transform;

        // Default to a 'Seed' layer if present; otherwise everything.
        if (seedLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Seed");
            if (idx >= 0) seedLayer = 1 << idx;
            else seedLayer = ~0; // fallback: all layers
        }

        // Heuristic: auto-pick left/right by name
        if (name.ToLower().Contains("left"))  controller = OVRInput.Controller.LTouch;
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

        var seed = FindBestSeed();
        if (!seed) return;

        if (seed.TryCollect(_stats))
        {
            Buzz(0.1f, 0.35f, 0.07f);

            if (handCurl)
            {
                // Set grip target directly instead of pulsing
                handCurl.SetGripTarget(collectPulseValue);
                // Reset after delay
                Invoke(nameof(ResetHand), collectPulseHold + collectPulseBack);
            }

            
            if (debugDraw) Debug.Log($"[SeedPickupVR] Collected seed proto={seed.prototypeIndex} by {controller}");
        }
    }

    // Add this method to SeedPickupVR:
    private void ResetHand()
    {
        if (handCurl) handCurl.SetGripTarget(0f);
    }


    SeedCollectible FindBestSeed()
    {
        // 1) Touch: overlap sphere at controller
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

        // 2) Short forward ray as a convenience
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
        Gizmos.color = new Color(0,1,0,0.25f);
        Gizmos.DrawWireSphere(aimOrigin.position, touchRadius);

        Gizmos.color = new Color(0,1,1,0.25f);
        Gizmos.DrawLine(aimOrigin.position, aimOrigin.position + aimOrigin.forward * reachDistance);
    }

    void Buzz(float f, float a, float dur)
    {
        try { OVRInput.SetControllerVibration(f, a, controller); } catch { }
        Invoke(nameof(StopBuzz), dur);
    }
    void StopBuzz()
    {
        try { OVRInput.SetControllerVibration(0, 0, controller); } catch { }
    }
}
