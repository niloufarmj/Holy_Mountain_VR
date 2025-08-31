using UnityEngine;

/// <summary>
/// VR-friendly throwable stone with pickup/drop/throw logic and simple settling behavior.
/// - Starts optionally as a static object resting on the ground (kinematic).
/// - When dropped/thrown, uses physics and gradually settles if motion is below thresholds.
/// - Temporarily disables colliders while held to avoid unwanted collisions.
/// - Optionally coordinates with <see cref="StoneHighlightable"/> to disable highlight when held.
/// </summary>
[RequireComponent(typeof(Rigidbody)), DisallowMultipleComponent]
public class ThrowableStone : MonoBehaviour
{
    [Header("Landing / Settle")]
    [Tooltip("Linear damping applied after drop/throw (custom field used by your physics code).")]
    public float dropDrag = 0.25f;

    [Tooltip("Angular damping applied after drop/throw (custom field used by your physics code).")]
    public float dropAngularDrag = 3.0f;

    [Tooltip("If linear speed falls below this value, settling begins.")]
    public float settleSpeedThreshold = 0.15f;

    [Tooltip("If angular speed falls below this value, settling begins.")]
    public float settleAngularThreshold = 1.0f;

    [Tooltip("How long speeds must stay below thresholds before the stone is put to sleep (kinematic).")]
    public float settleDelay = 0.6f;

    [Tooltip("Raycast distance used to confirm the stone is resting on the ground.")]
    public float groundCheckDist = 0.35f;

    [Tooltip("Layers considered as ground for the settle-raycast.")]
    public LayerMask groundMask = ~0;

    // Accumulates time spent under settle thresholds
    private float _stillTimer;

    [Header("Setup")]
    [Tooltip("Optional reference to the highlight component. Auto-fetched if not assigned.")]
    public StoneHighlightable highlightable;

    [Tooltip("If true, all colliders under this object are disabled while held.")]
    public bool disableCollidersWhileHeld = true;

    [Tooltip("If true, the stone starts kinematic without gravity (as if already resting on the ground).")]
    public bool startAsStaticOnGround = true;

    [Header("Throw")]
    [Tooltip("Random spin magnitude applied on throw.")]
    public float randomSpin = 8f;

    /// <summary>Indicates whether the stone is currently held.</summary>
    public bool IsHeld { get; private set; }

    // Cached state
    private Rigidbody rb;
    private Collider[] colls;
    private Transform lastThrower;
    private Vector3 initialWorldScale;

    // ----- at the top of ThrowableStone -----
    [Header("Audio (Animal Hit)")]
    [SerializeField] private AudioClip animalHitClip;   // single clip
    [Range(0f, 1f)] public float animalHitVolume = 0.9f;
    public float minSpeedForSfx = 1.2f;
    public float maxSpeedForFullVolume = 8f;
    [Range(0f, 0.25f)] public float pitchJitter = 0.08f;
    public float sfxCooldown = 0.10f;
    public float sfxMinDistance = 1.5f;
    public float sfxMaxDistance = 25f;

    private AudioSource _sfx;
    private float _nextSfxOkTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colls = GetComponentsInChildren<Collider>(true);
        if (!highlightable) highlightable = GetComponent<StoneHighlightable>();

        // Use continuous collision for fast-moving throws
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Initial physics mode (static vs dynamic)
        if (startAsStaticOnGround)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.spatialBlend = 1f; // 3D
        _sfx.rolloffMode = AudioRolloffMode.Logarithmic;
        _sfx.minDistance = sfxMinDistance;
        _sfx.maxDistance = sfxMaxDistance;
        _sfx.spatialize = true; // if your XR audio plugin supports it
    }

    private void Start()
    {
        // Record world scale at spawn so we can restore it when picked up
        initialWorldScale = transform.lossyScale;
    }

    private void Update()
    {
        // Only manage settling when not held and physics is active
        if (!IsHeld && !rb.isKinematic)
        {
            // Check for ground beneath the stone
            bool grounded = Physics.Raycast(
                transform.position + Vector3.up * 0.05f,
                Vector3.down, out RaycastHit hit, groundCheckDist, groundMask,
                QueryTriggerInteraction.Ignore);

            // If grounded and both linear & angular speeds are below thresholds, advance settle timer
            if (grounded &&
                rb.linearVelocity.sqrMagnitude < (settleSpeedThreshold * settleSpeedThreshold) &&
                rb.angularVelocity.magnitude < settleAngularThreshold)
            {
                _stillTimer += Time.deltaTime;

                if (_stillTimer >= settleDelay)
                {
                    // Optional: align slightly to ground normal for stability
                    // transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

                    // Put to sleep (static)
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
            else
            {
                _stillTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Utility to set world scale precisely, compensating for any parent scaling.
    /// </summary>
    private void SetWorldScale(Vector3 targetWorldScale)
    {
        Vector3 p = (transform.parent ? transform.parent.lossyScale : Vector3.one);
        transform.localScale = new Vector3(
            targetWorldScale.x / Mathf.Max(p.x, 1e-6f),
            targetWorldScale.y / Mathf.Max(p.y, 1e-6f),
            targetWorldScale.z / Mathf.Max(p.z, 1e-6f)
        );
    }

    /// <summary>
    /// Parents the stone to the given hand transform, resets local pose, restores its original world scale,
    /// marks it as held, and zeros motion. Also turns off highlight if present.
    /// </summary>
    public void PickUp(Transform hand)
    {
        transform.SetParent(hand, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Restore the recorded spawn scale (prevents cumulative scaling when re-picked)
        SetWorldScale(initialWorldScale);

        SetHeld(true);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (highlightable) highlightable.SetHighlighted(false);
    }

    /// <summary>
    /// Releases the stone at its current position and switches to dynamic physics,
    /// applying the configured drop damping.
    /// </summary>
    public void Drop()
    {
        if (!IsHeld) return;

        // Detach while keeping world transform unchanged
        transform.SetParent(null, true);
        SetHeld(false);

        // Apply drop damping and reset settle timer
        rb.linearDamping = dropDrag;
        rb.angularDamping = dropAngularDrag;
        _stillTimer = 0f;
    }

    /// <summary>
    /// Throws the stone with a given initial velocity from a specified thrower transform.
    /// Adds a random angular spin and enables dynamic physics with drop damping.
    /// </summary>
    public void Throw(Vector3 velocity, Transform thrower)
    {
        // Detach while keeping world transform unchanged
        transform.SetParent(null, true);
        SetHeld(false);

        // Apply drop damping and reset settle timer
        rb.linearDamping = dropDrag;
        rb.angularDamping = dropAngularDrag;
        _stillTimer = 0f;

        lastThrower = thrower;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.insideUnitSphere * randomSpin;
    }

    void OnCollisionEnter(Collision c)
    {
        if (IsHeld) return;

        var animal = c.collider.GetComponentInParent<AnimalWander>();
        if (animal != null)
        {
            Vector3 from = lastThrower ? lastThrower.position
                                    : (transform.position - c.GetContact(0).normal * 0.2f);
            animal.OnHitByStone(from); // ← متد جدید بالا

            PlayAnimalHitSfx(c); // play the single clip here
        }

    }

    /// <summary>
    /// Internal helper to toggle held state, physics mode, colliders, and highlight.
    /// </summary>
    private void SetHeld(bool held)
    {
        IsHeld = held;

        // While held: disable physics; when released: enable physics
        if (held)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Optionally disable colliders while held to prevent clipping/collisions
        if (disableCollidersWhileHeld)
        {
            foreach (var c in colls) c.enabled = !held;
        }

        // Ensure highlight is off while held
        if (highlightable && held)
            highlightable.SetHighlighted(false);
    }
    
    // ----- helper inside class -----
    private void PlayAnimalHitSfx(Collision c)
    {
        if (Time.time < _nextSfxOkTime) return;
        if (animalHitClip == null) return;

        float speed = c.relativeVelocity.magnitude;
        if (speed < minSpeedForSfx) return;

        float loudness = Mathf.InverseLerp(minSpeedForSfx, maxSpeedForFullVolume, speed);
        _sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);

        // play at the contact point for proper spatialization
        _sfx.transform.position = c.GetContact(0).point;
        _sfx.PlayOneShot(animalHitClip, animalHitVolume * loudness);

        _nextSfxOkTime = Time.time + sfxCooldown;
    }
}
