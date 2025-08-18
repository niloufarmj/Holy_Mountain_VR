using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class AnimalWander : MonoBehaviour
{
    private enum AnimalState { Idling, Walking, Eating }

    [Header("Movement Settings")]
    public float wanderRadius = 15f;
    public float minIdleTime = 3f;
    public float maxIdleTime = 6f;

    [Header("Animation Settings")]
    public string speedParameter = "Speed";
    public string animVariantParameter = "AnimVariant";
    public float minWalkTime = 6f;
    public float maxWalkTime = 12f;
    public float walkAnimChangeInterval = 5f;

    // NEW: واکنش به سنگ
    [Header("Scare Reaction")]
    [Tooltip("مدت زمانی که بعد از خوردن سنگ، حیوان جذب درخت‌ها نشود.")]
    public float scaredCooldown = 6f;
    [Tooltip("فاصله‌ی هدف فرار از منبع برخورد.")]
    public float fleeDistance = 12f;
    [Tooltip("بازه‌ی زمانی که حیوان بعد از برخورد، در حالت دور شدن بماند.")]
    public Vector2 fleeTimeRange = new Vector2(3f, 5f);

    private NavMeshAgent agent;
    private Animator animator;
    private AnimalState currentState;
    private float stateTimer;

    private const int totalVariants = 6;
    private float walkAnimChangeTimer;
    private float currentAnimVariant = -1f;

    public Transform targetTree = null;
    public float targetPriorityWeight = 0.7f;
    public float targetReachedThreshold = 1f;

    // NEW:
    private float scaredUntil = -1f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        StartIdling();
    }

    private void Update()
    {
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat(speedParameter, currentSpeed > 0.05f ? 1f : 0f);

        stateTimer -= Time.deltaTime;

        // NEW: اگر در کول‌داون ترس هستیم، هیچ هدف درختی را دنبال نکنیم
        if (Time.time < scaredUntil && targetTree != null)
            targetTree = null;

        // سوئیچ به Eating وقتی به درخت رسیدیم (اگر کول‌داون نیستیم)
        if (targetTree != null &&
            Time.time >= scaredUntil &&
            Vector3.Distance(transform.position, targetTree.position) < targetReachedThreshold)
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
                SetAnimVariant(differentFromCurrent: true);
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
            if (grower != null) grower.StartEating(this);
        }
        animator.SetBool("IsEating", true);
    }

    private void StopEating()
    {
        animator.SetBool("IsEating", false);

        if (targetTree != null)
        {
            TreeGrower grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null) grower.StopEating(this);
        }

        targetTree = null;
        StartIdling();
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

        SetAnimVariant();
        walkAnimChangeTimer = walkAnimChangeInterval;
    }

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

    public static Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += origin;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            return navHit.position;

        return origin;
    }

    // NEW: وقتی سنگ می‌خورد
    public void OnHitByStone(Vector3 fromPosition)
    {
        // هرچه می‌خورد قطع کن و از درخت جدا شو
        ForceStopEating(); // همین الان در کدت موجوده و StopEating را صدا می‌زند. :contentReference[oaicite:1]{index=1}

        // مدتی جذب درخت‌ها نشود
        scaredUntil = Time.time + scaredCooldown;

        // جهت فرار از منبع برخورد
        Vector3 away = (transform.position - fromPosition);
        if (away.sqrMagnitude < 0.001f) away = -transform.forward;
        away.Normalize();

        Vector3 basePoint = transform.position + away * fleeDistance;
        Vector3 fleeTarget = GetRandomNavMeshPoint(basePoint, fleeDistance * 0.75f);

        currentState = AnimalState.Walking;
        agent.ResetPath();
        agent.SetDestination(fleeTarget);

        stateTimer = Random.Range(fleeTimeRange.x, fleeTimeRange.y);
        SetAnimVariant(differentFromCurrent: true);
    }

    // اگر در کول‌داون ترس است، درخواست هدف جدید را نادیده بگیر
    public void SetTargetTree(Transform tree)
    {
        if (Time.time < scaredUntil) return; // NEW
        targetTree = tree;
    }

    public void ForceStopEating()
    {
        StopEating();
    }
}
