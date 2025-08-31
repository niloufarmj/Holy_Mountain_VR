using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple AI for ambient animals that alternate between idling and wandering,
/// optionally seeking a target tree to eat. Supports:
/// - Wander/idle cycle within a radius
/// - Approaching and eating a target tree (via TreeGrower.StartEating/StopEating)
/// - Being scared away when hit by a thrown stone (temporary cooldown)
/// - Walk animation variant cycling and speed parameter driving
/// - Optional looping eating SFX (3D spatialized)
///
/// Requirements:
/// - <see cref="NavMeshAgent"/> for navigation
/// - <see cref="Animator"/> with parameters:
///     * float Speed (or configured via <see cref="speedParameter"/>)
///     * float AnimVariant (or configured via <see cref="animVariantParameter"/>)
///     * bool  IsEating
/// - Optional: <see cref="TreeGrower"/> on the target tree transform
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class AnimalWander : MonoBehaviour
{
    #region Types

    private enum AnimalState { Idling, Walking, Eating }

    #endregion


    #region Inspector: Movement

    [Header("Movement Settings")]
    [Tooltip("Maximum radius used when picking random wander positions.")]
    public float wanderRadius = 15f;

    [Tooltip("Minimum time (seconds) to remain idle between walks.")]
    public float minIdleTime = 3f;

    [Tooltip("Maximum time (seconds) to remain idle between walks.")]
    public float maxIdleTime = 6f;

    #endregion


    #region Inspector: Animation

    [Header("Animation Settings")]
    [Tooltip("Animator float parameter that maps to locomotion speed (0 or 1 is sufficient for blend trees).")]
    public string speedParameter = "Speed";

    [Tooltip("Animator float parameter [0..1] selecting one of multiple walk variants.")]
    public string animVariantParameter = "AnimVariant";

    [Tooltip("Minimum time (seconds) to keep walking before switching state.")]
    public float minWalkTime = 6f;

    [Tooltip("Maximum time (seconds) to keep walking before switching state.")]
    public float maxWalkTime = 12f;

    [Tooltip("How often (seconds) to pick a new walk animation variant while walking.")]
    public float walkAnimChangeInterval = 5f;

    #endregion


    #region Inspector: Approach / Eating

    [Header("Approach/Eating")]
    [Tooltip("Distance at which the animal considers itself close enough to start eating.")]
    public float eatDistance = 0.7f;

    [Tooltip("NavMesh sampling radius around the tree when searching for a reachable point.")]
    public float approachRadius = 1.5f;

    [Tooltip("How frequently (seconds) to refresh the path while approaching the tree.")]
    public float repathInterval = 0.6f;

    #endregion


    #region Inspector: Scare / Flee

    [Header("Scare Reaction")]
    [Tooltip("Duration (seconds) after being hit by a stone during which the animal ignores trees.")]
    public float scaredCooldown = 6f;

    [Tooltip("Desired distance to flee away from the impact source.")]
    public float fleeDistance = 12f;

    [Tooltip("Randomized duration (seconds) to remain in a fleeing/walking state after impact.")]
    public Vector2 fleeTimeRange = new Vector2(3f, 5f);

    #endregion


    #region Inspector: Audio (Eating Loop)

    [Header("Audio (Eating Loop)")]
    [SerializeField, Tooltip("Looped chewing/eating SFX played while the animal is eating.")]
    private AudioClip eatingLoopSfx;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Volume of the eating loop SFX.")]
    private float eatingLoopVolume = 0.7f;

    [SerializeField, Tooltip("Min 3D distance for spatial rolloff.")]
    private float eatingMinDistance = 1.5f;

    [SerializeField, Tooltip("Max 3D distance for spatial rolloff.")]
    private float eatingMaxDistance = 20f;

    [SerializeField, Tooltip("Randomize pitch slightly each time eating starts to avoid monotony.")]
    private bool eatingRandomizePitch = true;

    [SerializeField, Range(0f, 0.2f)]
    [Tooltip("Pitch jitter magnitude used when randomizing pitch.")]
    private float eatingPitchJitter = 0.04f;

    #endregion


    #region Runtime fields

    private NavMeshAgent agent;
    private Animator animator;

    private AnimalState currentState;
    private float stateTimer;             // Counts down time left in the current state
    private float repathTimer;            // Counts down until we refresh path when approaching a tree
    private float defaultStoppingDistance;

    private const int totalVariants = 6;  // Number of walk variants available in the animator [0..1]
    private float walkAnimChangeTimer;
    private float currentAnimVariant = -1f;

    [Tooltip("Tree target the animal will try to approach and eat, if assigned at runtime.")]
    public Transform targetTree = null;

    [Tooltip("Bias toward the target when picking wander positions (0 = random, 1 = only toward target).")]
    public float targetPriorityWeight = 0.7f;

    [Tooltip("Not currently used; reserved for proximity checks if needed.")]
    public float targetReachedThreshold = 1f;

    // Scare cooldown end-time (Time.time based). Negative value means not scared.
    private float scaredUntil = -1f;

    private AudioSource eatSource;

    #endregion


    #region Unity lifecycle

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Prepare eating loop SFX if provided
        if (eatingLoopSfx != null)
        {
            eatSource = gameObject.AddComponent<AudioSource>();
            eatSource.playOnAwake = false;
            eatSource.loop = true;                         // Plays continuously while eating
            eatSource.clip = eatingLoopSfx;
            eatSource.volume = eatingLoopVolume;
            eatSource.spatialBlend = 1f;                   // 3D spatial audio (useful in VR)
            eatSource.rolloffMode = AudioRolloffMode.Logarithmic;
            eatSource.minDistance = eatingMinDistance;
            eatSource.maxDistance = eatingMaxDistance;
            eatSource.spatialize = true;                   // If your XR audio plugin supports it
        }

        defaultStoppingDistance = agent.stoppingDistance;
        StartIdling();  // Begin in idle state
    }

    private void Update()
    {
        // Drive a simple 0/1 speed parameter (works fine with blend trees expecting >0 for locomotion)
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat(speedParameter, currentSpeed > 0.05f ? 1f : 0f);

        stateTimer  -= Time.deltaTime;
        repathTimer -= Time.deltaTime;

        // If scared, drop any existing attraction to trees
        if (Time.time < scaredUntil && targetTree != null)
            targetTree = null;

        // --- Tree approach / eating ---
        if (targetTree != null && Time.time >= scaredUntil)
        {
            float dist = Vector3.Distance(transform.position, targetTree.position);

            if (dist > eatDistance)
            {
                // Continue approaching the tree at a throttled repath cadence
                if (repathTimer <= 0f)
                {
                    ApproachTree();
                    repathTimer = repathInterval;
                }
            }
            else
            {
                // Close enough → switch to eating state (suppresses wander/idle)
                if (currentState != AnimalState.Eating)
                    StartEating();
                return; // While eating, ignore other state logic
            }
        }

        // --- Walk variant cycling while walking ---
        if (currentState == AnimalState.Walking)
        {
            walkAnimChangeTimer -= Time.deltaTime;
            if (walkAnimChangeTimer <= 0f)
            {
                SetAnimVariant(differentFromCurrent: true);
                walkAnimChangeTimer = walkAnimChangeInterval;
            }
        }

        // --- State machine: toggle between idle and walking when timers elapse ---
        if (stateTimer <= 0f)
        {
            if (currentState == AnimalState.Idling)
                StartWalking();
            else
                StartIdling();
        }
    }

    private void OnDisable()
    {
        if (eatSource != null && eatSource.isPlaying) eatSource.Stop();
    }

    private void OnDestroy()
    {
        if (eatSource != null && eatSource.isPlaying) eatSource.Stop();
    }

    #endregion


    #region State helpers

    /// <summary>
    /// Choose a reachable point near the target tree and path toward it.
    /// </summary>
    private void ApproachTree()
    {
        if (!targetTree) return;

        // Use a slightly smaller stopping distance than the eat trigger distance for stability
        agent.stoppingDistance = eatDistance * 0.9f;

        Vector3 around = targetTree.position;
        if (NavMesh.SamplePosition(around, out NavMeshHit hit, approachRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            currentState = AnimalState.Walking;
        }
        else
        {
            // Fallback: path directly to the tree position if sampling failed
            agent.SetDestination(around);
            currentState = AnimalState.Walking;
        }
    }

    /// <summary>
    /// Enter the eating state, halt movement, notify the tree, and start looped SFX.
    /// </summary>
    private void StartEating()
    {
        currentState = AnimalState.Eating;

        agent.ResetPath();
        agent.stoppingDistance = defaultStoppingDistance; // Restore user-defined stopping distance
        animator.SetFloat(speedParameter, 0f);

        if (targetTree != null)
        {
            // Notify the tree that this animal started eating
            var grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null) grower.StartEating(this); // Original behavior. :contentReference[oaicite:0]{index=0}
        }

        animator.SetBool("IsEating", true);

        // Start looped chewing SFX
        if (eatSource != null)
        {
            if (eatingRandomizePitch)
                eatSource.pitch = 1f + Random.Range(-eatingPitchJitter, eatingPitchJitter);

            if (!eatSource.isPlaying) eatSource.Play();
            else eatSource.UnPause(); // If someone paused it externally
        }
    }

    /// <summary>
    /// Exit the eating state, notify the tree, stop SFX, clear target, and return to idle.
    /// </summary>
    private void StopEating()
    {
        animator.SetBool("IsEating", false);

        if (targetTree != null)
        {
            // Notify the tree that this animal stopped eating
            var grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null) grower.StopEating(this); // Original behavior. :contentReference[oaicite:1]{index=1}
        }

        if (eatSource != null && eatSource.isPlaying)
            eatSource.Stop();

        agent.stoppingDistance = defaultStoppingDistance;
        targetTree = null;

        StartIdling();
    }

    /// <summary>
    /// Enter idle state and pick a random duration.
    /// </summary>
    private void StartIdling()
    {
        currentState = AnimalState.Idling;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);

        agent.ResetPath();
        SetAnimVariant(); // Pick a (potentially new) walk variant for later
    }

    /// <summary>
    /// Enter walking state, choose a destination biased toward the target (if any), and set a timer.
    /// </summary>
    private void StartWalking()
    {
        currentState = AnimalState.Walking;

        // Base destination: current position, optionally biased toward the target
        Vector3 basePoint = transform.position;

        if (targetTree != null)
        {
            Vector3 toTarget = (targetTree.position - transform.position).normalized;
            Vector3 randomOffset = Random.insideUnitSphere * wanderRadius * (1f - targetPriorityWeight);
            Vector3 towardTarget = toTarget * wanderRadius * targetPriorityWeight;
            basePoint = transform.position + randomOffset + towardTarget;
        }

        // Pick a reachable random point on the NavMesh
        Vector3 randomDestination = GetRandomNavMeshPoint(basePoint, wanderRadius);

        // Attempt to path; compute a conservative timer to avoid early switching
        float randomWalkTime = Random.Range(minWalkTime, maxWalkTime);
        if (agent.SetDestination(randomDestination))
        {
            float travelTime = Vector3.Distance(transform.position, randomDestination) / agent.speed;
            stateTimer = Mathf.Max(travelTime + 2f, randomWalkTime);
        }
        else
        {
            // If we failed to get a path, fall back to idle this frame
            StartIdling();
            return;
        }

        SetAnimVariant();
        walkAnimChangeTimer = walkAnimChangeInterval;
    }

    /// <summary>
    /// Randomly selects a normalized [0..1] animation variant index and applies it.
    /// Optionally guarantees the value differs from the current one.
    /// </summary>
    private void SetAnimVariant(bool differentFromCurrent = false)
    {
        int index;
        do
        {
            index = Random.Range(0, totalVariants);
        } while (differentFromCurrent &&
                 Mathf.Approximately((float)index / (totalVariants - 1), currentAnimVariant));

        float normalizedValue = totalVariants > 1 ? (float)index / (totalVariants - 1) : 0f;
        currentAnimVariant = normalizedValue;
        animator.SetFloat(animVariantParameter, normalizedValue);
    }

    #endregion


    #region Public API

    /// <summary>
    /// Utility: Sample a random reachable point on the NavMesh near an origin.
    /// </summary>
    /// <param name="origin">Center of the sampling area.</param>
    /// <param name="radius">Sampling radius around the origin.</param>
    public static Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += origin;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            return navHit.position;

        return origin; // Fallback: no valid position found
    }

    /// <summary>
    /// Called by external systems (e.g., your stone projectile) when the animal is hit.
    /// The animal stops eating, ignores trees for a cooldown, and flees away from impact.
    /// </summary>
    /// <param name="fromPosition">World position of the impact source.</param>
    public void OnHitByStone(Vector3 fromPosition)
    {
        // Immediately stop any ongoing eating and detach from tree
        ForceStopEating(); // Calls StopEating under the hood in your original script. :contentReference[oaicite:2]{index=2}

        // Ignore trees for a short duration
        scaredUntil = Time.time + scaredCooldown;

        // Compute a flee direction away from the impact
        Vector3 away = (transform.position - fromPosition);
        if (away.sqrMagnitude < 0.001f) away = -transform.forward; // Edge case: overlapping
        away.Normalize();

        // Pick a reachable flee target in that general direction
        Vector3 basePoint = transform.position + away * fleeDistance;
        Vector3 fleeTarget = GetRandomNavMeshPoint(basePoint, fleeDistance * 0.75f);

        currentState = AnimalState.Walking;
        agent.ResetPath();
        agent.SetDestination(fleeTarget);

        stateTimer = Random.Range(fleeTimeRange.x, fleeTimeRange.y);
        SetAnimVariant(differentFromCurrent: true);
    }

    /// <summary>
    /// Assigns a new target tree to approach and eat. Ignored while in scare cooldown.
    /// </summary>
    public void SetTargetTree(Transform tree)
    {
        if (Time.time < scaredUntil) return; // Respect scare cooldown
        targetTree = tree;
        repathTimer = 0f; // Force an immediate path update next frame
    }

    /// <summary>
    /// External hard stop for eating (e.g., if the tree dies or is scared away).
    /// </summary>
    public void ForceStopEating()
    {
        StopEating();
    }

    #endregion
}
