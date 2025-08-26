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


    [Header("Approach/Eating")]
    public float eatDistance = 0.7f;         // فاصله‌ای که کافی است تا شروع به خوردن کند
    public float approachRadius = 1.5f;      // شعاعی که اطراف درخت در NavMesh دنبال نقطه می‌گردیم
    public float repathInterval = 0.6f;      // هر چند وقت یک بار مقصد را به‌روز کنیم

    float repathTimer = 0f;
    float defaultStoppingDistance;


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

    [Header("Audio (Eating Loop)")]
    [SerializeField] private AudioClip eatingLoopSfx;
    [SerializeField, Range(0f, 1f)] private float eatingLoopVolume = 0.7f;
    [SerializeField] private float eatingMinDistance = 1.5f;
    [SerializeField] private float eatingMaxDistance = 20f;
    [SerializeField] private bool eatingRandomizePitch = true;
    [SerializeField, Range(0f, 0.2f)] private float eatingPitchJitter = 0.04f;

    private AudioSource _eatSrc;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (eatingLoopSfx != null)
        {
            _eatSrc = gameObject.AddComponent<AudioSource>();
            _eatSrc.playOnAwake = false;
            _eatSrc.loop = true;                    // keep going while eating
            _eatSrc.clip = eatingLoopSfx;
            _eatSrc.volume = eatingLoopVolume;
            _eatSrc.spatialBlend = 1f;              // 3D for VR
            _eatSrc.rolloffMode = AudioRolloffMode.Logarithmic;
            _eatSrc.minDistance = eatingMinDistance;
            _eatSrc.maxDistance = eatingMaxDistance;
            _eatSrc.spatialize = true;              // if your XR audio plugin supports it
        }

        defaultStoppingDistance = agent.stoppingDistance;
        StartIdling();
    }

    void Update()
    {
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat(speedParameter, currentSpeed > 0.05f ? 1f : 0f);

        stateTimer -= Time.deltaTime;
        repathTimer -= Time.deltaTime;

        // اگر ترسیده‌ایم، جذب درخت را پاک کن
        if (Time.time < scaredUntil && targetTree != null)
            targetTree = null;

        // --- نزدیک شدن/خوردن ---
        if (targetTree != null && Time.time >= scaredUntil)
        {
            float dist = Vector3.Distance(transform.position, targetTree.position);

            if (dist > eatDistance)
            {
                // نزدیک شو
                if (repathTimer <= 0f)
                {
                    ApproachTree();
                    repathTimer = repathInterval;
                }
            }
            else
            {
                if (currentState != AnimalState.Eating)
                    StartEating();
                return; // وقتی می‌خوریم، بقیه‌ی رفتارها بی‌اثر باشند
            }
        }

        // --- چرخه‌ی راه‌رفتن/ایستادن قبلی ---
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

    void ApproachTree()
    {
        if (!targetTree) return;

        agent.stoppingDistance = eatDistance * 0.9f; // کمی کمتر از آستانه
        Vector3 around = targetTree.position;
        if (NavMesh.SamplePosition(around, out NavMeshHit hit, approachRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            currentState = AnimalState.Walking;
        }
        else
        {
            // اگر NavMesh نبود، حداقل سعی کن سمت خود درخت بروی
            agent.SetDestination(around);
            currentState = AnimalState.Walking;
        }
    }


    private void StartEating()
    {
        currentState = AnimalState.Eating;
        agent.ResetPath();
        agent.stoppingDistance = defaultStoppingDistance; // برگردان
        animator.SetFloat(speedParameter, 0f);

        if (targetTree != null)
        {
            TreeGrower grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null) grower.StartEating(this); // موجود است. :contentReference[oaicite:2]{index=2}
        }
        animator.SetBool("IsEating", true);

        // --- NEW: eating loop SFX ---
        if (_eatSrc != null)
        {
            if (eatingRandomizePitch)
                _eatSrc.pitch = 1f + Random.Range(-eatingPitchJitter, eatingPitchJitter);

            if (!_eatSrc.isPlaying) _eatSrc.Play();
            else _eatSrc.UnPause(); // in case it was paused elsewhere
        }

    }

    private void StopEating()
    {
        animator.SetBool("IsEating", false);

        if (targetTree != null)
        {
            TreeGrower grower = targetTree.GetComponent<TreeGrower>();
            if (grower != null) grower.StopEating(this); // موجود است. :contentReference[oaicite:3]{index=3}
        }

        // --- NEW: stop eating SFX ---
        if (_eatSrc != null && _eatSrc.isPlaying)
            _eatSrc.Stop();

        agent.stoppingDistance = defaultStoppingDistance;
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
        if (Time.time < scaredUntil) return; // همان گارد
        targetTree = tree;
        repathTimer = 0f; // فوراً مسیر بگیر
    }

    public void ForceStopEating()
    {
        StopEating();
    }
    
    private void OnDisable()
    {
        if (_eatSrc != null && _eatSrc.isPlaying) _eatSrc.Stop();
    }
    private void OnDestroy()
    {
        if (_eatSrc != null && _eatSrc.isPlaying) _eatSrc.Stop();
    }
}
