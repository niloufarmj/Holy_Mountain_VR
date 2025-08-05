using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class AnimalWander : MonoBehaviour
{
    // Enum to manage the animal's current state
    private enum AnimalState
    {
        Idling,
        Walking,
        Eating // NEW
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

    private NavMeshAgent agent;
    private Animator animator;
    private AnimalState currentState;
    private float stateTimer;

    private const int totalVariants = 6; // Number of idle/walk animation variants
    private float walkAnimChangeTimer;
    private float currentAnimVariant = -1f;

    public Transform targetTree = null;
    public float targetPriorityWeight = 0.7f; // بین ۰ تا ۱، چقدر به سمت درخت جذب شه
    public float targetReachedThreshold = 1f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        StartIdling();
    }

    void Update()
    {
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat(speedParameter, currentSpeed > 0.05f ? 1f : 0f);

        stateTimer -= Time.deltaTime;

        if (targetTree != null && Vector3.Distance(transform.position, targetTree.position) < targetReachedThreshold)
        {
            if (currentState != AnimalState.Eating)
            {
                StartEating();
            }
            return;
        }

        if (currentState == AnimalState.Walking)
        {
            walkAnimChangeTimer -= Time.deltaTime;

            if (walkAnimChangeTimer <= 0f)
            {
                SetAnimVariant(differentFromCurrent: true); // pick a different walk animation
                walkAnimChangeTimer = walkAnimChangeInterval;
            }
        }

        if (stateTimer <= 0f)
        {
            if (currentState == AnimalState.Idling)
                StartWalking();
            else
                StartIdling();
        }
    }

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

        // حالت انیمیشن یا پارامتر خاص برای خوردن، در صورت وجود
        animator.SetBool("IsEating", true); // فرض بر اینه که همچین تریگری وجود داره
    }

    private void StopEating()
    {
        animator.SetBool("IsEating", false);

        if (targetTree != null)
        {
            TreeGrower grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null)
                grower.StopEating(this);
        }

        targetTree = null; // دیگه هدفی نداره
        StartIdling();     // دوباره وارد حالت عادی بشه
    }


    private void StartIdling()
    {
        currentState = AnimalState.Idling;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);

        agent.ResetPath();

        SetAnimVariant();
    }

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
    /// Randomly sets the animation variant between 0 and 1 (in steps based on variant count).
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
    /// Finds a random point on the NavMesh within a given radius.
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

    public void SetTargetTree(Transform tree)
    {
        targetTree = tree;
    }

    public void ForceStopEating()
    {
        StopEating(); // همون تابع خودت که متوقف می‌کنه همه‌چیزو
    }
}
