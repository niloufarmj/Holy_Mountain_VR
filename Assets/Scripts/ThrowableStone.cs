using UnityEngine;

[RequireComponent(typeof(Rigidbody)), DisallowMultipleComponent]
public class ThrowableStone : MonoBehaviour
{
    // بالای کلاس:
    [Header("Landing / Settle")]
    public float dropDrag = 0.25f;                // مقاومت حرکت پس از ول‌کردن
    public float dropAngularDrag = 3.0f;          // مقاومت چرخش
    public float settleSpeedThreshold = 0.15f;    // اگر سرعت خطی کمتر از این شد...
    public float settleAngularThreshold = 1.0f;   // و سرعت زاویه‌ای کمتر از این شد...
    public float settleDelay = 0.6f;              // به مدت این زمان...
    public float groundCheckDist = 0.35f;         // فاصله Raycast برای تشخیص زمین
    public LayerMask groundMask = ~0;             // پیش‌فرض همه لایه‌ها

    float _stillTimer;





    [Header("Setup")]
    public StoneHighlightable highlightable;   // optional
    public bool disableCollidersWhileHeld = true;
    public bool startAsStaticOnGround = true;  // ✅ جدید: در شروع استاتیک باشه

    [Header("Throw")]
    public float randomSpin = 8f;

    public bool IsHeld { get; private set; }

    Rigidbody rb;
    Collider[] colls;
    Transform lastThrower;

    Vector3 initialWorldScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colls = GetComponentsInChildren<Collider>(true);
        if (!highlightable) highlightable = GetComponent<StoneHighlightable>();

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // ❗ دیگر SetHeld(false) نمی‌زنیم.
        if (startAsStaticOnGround)
        {
            rb.isKinematic = true;   // استاتیک
            rb.useGravity  = false;
        }
        else
        {
            rb.isKinematic = false;  // اگر خواستی از اول داینامیک
            rb.useGravity  = true;
        }
    }

    void Start()
    {
        // اندازه‌ی اصلی در لحظه‌ی اسپاون
        initialWorldScale = transform.lossyScale;
    }


    void Update()
    {
        // وقتی دستِ پلیر نیست و فیزیک روشنه
        if (!IsHeld && !rb.isKinematic)
        {
            // تشخیص زمین زیر سنگ
            bool grounded = Physics.Raycast(
                transform.position + Vector3.up * 0.05f,
                Vector3.down, out RaycastHit hit, groundCheckDist, groundMask,
                QueryTriggerInteraction.Ignore);

            // اگر روی زمینیم و سرعت‌ها کم‌اند، تایمر ساکن بودن زیاد کن
            if (grounded &&
                rb.linearVelocity.sqrMagnitude < (settleSpeedThreshold * settleSpeedThreshold) &&
                rb.angularVelocity.magnitude < settleAngularThreshold)
            {
                _stillTimer += Time.deltaTime;

                if (_stillTimer >= settleDelay)
                {
                    // (اختیاری) کمی هم‌راستاسازی با نرمال زمین برای پایداری بیشتر
                    //transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

                    // بخوابون و استاتیک کن تا کامل وایسه
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    rb.useGravity  = false;
                }
            }
            else
            {
                _stillTimer = 0f;
            }
        }
    }

    // کمک: ست کردن world scale بدون توجه به اسکیل والد
    void SetWorldScale(Vector3 targetWorldScale)
    {
        Vector3 p = (transform.parent ? transform.parent.lossyScale : Vector3.one);
        transform.localScale = new Vector3(
            targetWorldScale.x / Mathf.Max(p.x, 1e-6f),
            targetWorldScale.y / Mathf.Max(p.y, 1e-6f),
            targetWorldScale.z / Mathf.Max(p.z, 1e-6f)
        );
    }

    public void PickUp(Transform hand)
    {
        
        transform.SetParent(hand, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // هر بارِ برداشتن، اندازه را به مقدار اصلی برگردان
        SetWorldScale(initialWorldScale);

        SetHeld(true);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        if (highlightable) highlightable.SetHighlighted(false);
    }

    public void Drop()
    {
        if (!IsHeld) return;
        // جدا شدن با حفظ world transform
        transform.SetParent(null, true);
        SetHeld(false);

        // داخل Drop() بعد از SetHeld(false):
        rb.linearDamping = dropDrag;
        rb.angularDamping = dropAngularDrag;
        _stillTimer = 0f;
    }

    public void Throw(Vector3 velocity, Transform thrower)
    {
        transform.SetParent(null, true);  // world scale حفظ می‌شود
        SetHeld(false);

        // داخل Throw() بعد از SetHeld(false):
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

        // اینجا بعداً حیوان رو خبر می‌کنیم؛ فعلاً خالی بمونه
        // var wander = c.collider.GetComponentInParent<AnimalWander>();
        // if (wander) wander.OnHitByStone(lastThrower ? lastThrower.position : transform.position);
    }

    void SetHeld(bool held)
    {
        IsHeld = held;

        // وقتی تو دسته: فیزیک خاموش
        if (held)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
        }

        if (disableCollidersWhileHeld)
        {
            foreach (var c in colls) c.enabled = !held;
        }

        if (highlightable && held)
            highlightable.SetHighlighted(false);
    }
}
