using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple wandering/foraging brain for an animal using a NavMeshAgent and Animator.
/// Cycles between Idling, Walking, and Eating states with randomized timers,
/// optional attraction toward a target tree, and animation variant switching.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class AnimalWander : MonoBehaviour
{
    /// <summary>
    /// High-level locomotion/behavior states for the animal.
    /// </summary>
    private enum AnimalState
    {
        Idling,
        Walking,
        Eating
    }

    [Header("Movement Settings")]
    [Tooltip("The radius within which the animal will wander.")]
    public float wanderRadius = 15f;

    [Tooltip("Minimum time the animal will stay idle.")]
    public float minIdleTime = 3f;

    [Tooltip("Maximum time the animal will stay idle.")]
    public float maxIdleTime = 6f;

    [Header("Animation Settings")]
    [Tooltip("The name of the float parameter in the Animator that controls speed.")]
    public string speedParameter = "Speed";

    [Tooltip("The name of the float parameter in the Animator that selects animation variant.")]
    public string animVariantParameter = "AnimVariant";

    [Tooltip("Minimum time the animal will walk.")]
    public float minWalkTime = 6f;

    [Tooltip("Maximum time the animal will walk.")]
    public float maxWalkTime = 12f;

    [Tooltip("Minimum time before changing walk animation variant again.")]
    public float walkAnimChangeInterval = 5f;

    // Required components
    private NavMeshAgent agent;
    private Animator animator;

    // State machine bookkeeping
    private AnimalState currentState;
    private float stateTimer;

    // Animation variant bookkeeping
    private const int totalVariants = 6; // Number of idle/walk animation variants
    private float walkAnimChangeTimer;
    private float currentAnimVariant = -1f;

    // Optional attraction toward a target tree while wandering
    public Transform targetTree = null;

    [Tooltip("Weight (0..1) toward the target tree when picking a walk destination.")]
    public float targetPriorityWeight = 0.7f;

    [Tooltip("Distance within which the animal considers the tree reached for eating.")]
    public float targetReachedThreshold = 1f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        StartIdling();
    }

    private void Update()
    {
        // Drive a binary Speed parameter for blend trees that expect 0/1
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat(speedParameter, currentSpeed > 0.05f ? 1f : 0f);

        stateTimer -= Time.deltaTime;

        // Switch to Eating when close enough to the target tree
        if (targetTree != null && Vector3.Distance(transform.position, targetTree.position) < targetReachedThreshold)
        {
            if (currentState != AnimalState.Eating)
            {
                StartEating();
            }
            return;
        }

        // While walking, periodically change the walk animation variant
        if (currentState == AnimalState.Walking)
        {
            walkAnimChangeTimer -= Time.deltaTime;

            if (walkAnimChangeTimer <= 0f)
            {
                SetAnimVariant(differentFromCurrent: true); // pick a different walk animation
                walkAnimChangeTimer = walkAnimChangeInterval;
            }
        }

        // Flip between idle and walk when the state's timer elapses
        if (stateTimer <= 0f)
        {
            if (currentState == AnimalState.Idling)
                StartWalking();
            else
                StartIdling();
        }
    }

    /// <summary>
    /// Enter the Eating state: stop moving, notify the tree (if it supports it), and set eating animation flag.
    /// </summary>
    private void StartEating()
    {
        currentState = AnimalState.Eating;
        agent.ResetPath();
        animator.SetFloat(speedParameter, 0f);

        if (targetTree != null)
        {
            TreeGrower grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null)
                grower.StartEating(this);
        }

        // Assumes the Animator has a boolean parameter named "IsEating"
        animator.SetBool("IsEating", true);
    }

    /// <summary>
    /// Exit the Eating state: clear animation flag, notify the tree, clear target, and return to idle.
    /// </summary>
    private void StopEating()
    {
        animator.SetBool("IsEating", false);

        if (targetTree != null)
        {
            TreeGrower grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null)
                grower.StopEating(this);
        }

        targetTree = null;
        StartIdling();
    }

    /// <summary>
    /// Enter the Idling state and pick a new idle duration. Stops movement and randomizes idle animation variant.
    /// </summary>
    private void StartIdling()
    {
        currentState = AnimalState.Idling;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);

        agent.ResetPath();

        SetAnimVariant();
    }

    /// <summary>
    /// Enter the Walking state, choose a destination (optionally biased toward target tree),
    /// compute a reasonable state duration based on distance and a random window,
    /// and select a walk animation variant.
    /// </summary>
    private void StartWalking()
    {
        currentState = AnimalState.Walking;

        Vector3 basePoint = transform.position;

        if (targetTree != null)
        {
            Vector3 toTarget = (targetTree.position - transform.position).normalized;
            Vector3 randomOffset = Random.insideUnitSphere * wanderRadius * (1 - targetPriorityWeight);
            Vector3 towardTarget = toTarget * wanderRadius * targetPriorityWeight;

            basePoint = transform.position + randomOffset + towardTarget;
        }

        Vector3 randomDestination = GetRandomNavMeshPoint(basePoint, wanderRadius);

        float randomWalkTime = Random.Range(minWalkTime, maxWalkTime);

        if (agent.SetDestination(randomDestination))
        {
            float travelTime = Vector3.Distance(transform.position, randomDestination) / agent.speed;
            stateTimer = Mathf.Max(travelTime + 2f, randomWalkTime);
        }
        else
        {
            StartIdling();
            return;
        }

        SetAnimVariant(); // Initial walk animation
        walkAnimChangeTimer = walkAnimChangeInterval;
    }

    /// <summary>
    /// Randomly sets the animation variant (0..1 normalized across totalVariants).
    /// Optionally enforces that the new value differs from the current one.
    /// </summary>
    private void SetAnimVariant(bool differentFromCurrent = false)
    {
        int index;
        do
        {
            index = Random.Range(0, totalVariants);
        } while (differentFromCurrent && Mathf.Approximately((float)index / (totalVariants - 1), currentAnimVariant));

        float normalizedValue = totalVariants > 1 ? (float)index / (totalVariants - 1) : 0f;

        currentAnimVariant = normalizedValue;
        animator.SetFloat(animVariantParameter, normalizedValue);
    }

    /// <summary>
    /// Finds a random valid position on the NavMesh within a given radius of an origin.
    /// Returns the origin if sampling fails.
    /// </summary>
    public static Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += origin;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, radius, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        return origin;
    }

    /// <summary>
    /// Assign or clear the current target tree transform that influences walking and triggers eating.
    /// </summary>
    public void SetTargetTree(Transform tree)
    {
        targetTree = tree;
    }

    /// <summary>
    /// Public wrapper to forcibly stop the current eating behavior (e.g., if the tree is destroyed).
    /// </summary>
    public void ForceStopEating()
    {
        StopEating();
    }
}
