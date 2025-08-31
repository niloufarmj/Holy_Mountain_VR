using UnityEngine;

public class StonePickupVR : MonoBehaviour
{
    [Header("Refs")]
    public Transform aimOrigin;           // RightControllerAnchor
    public Transform holdAnchor;          // ViewHoldAnchor (جلوی چشم)
    public Transform trackingSpace;       // OVRCameraRig/TrackingSpace (برای تبدیل vel محلی به جهانی)
    public LayerMask stoneLayer;

    [Header("Pickup")]
    public float pickDistance = 3f;
    public float sphereRadius = 0.2f;
    public OVRInput.Button pickupButton = OVRInput.Button.One;           // A
    public KeyCode pickupKey = KeyCode.E;                                 // تست PC

    [Header("Throw (charge mode)")]
    public OVRInput.Button throwHoldButton = OVRInput.Button.SecondaryIndexTrigger; // Right Trigger
    public float minThrowSpeed = 6f;
    public float maxThrowSpeed = 18f;
    public float chargeTime = 1.0f;     // زمان تا رسیدن به max
    public bool useGestureThrow = false; // اگر true: سرعت واقعی کنترلر

    [Header("Gesture tuning (only if useGestureThrow)")]
    public float gestureMinSpeed = 4f;    // حداقل سرعت لازم
    public float gestureMaxSpeed = 16f;   // سقف
    public float forwardBoost = 2f;       // کمی بُست رو به جلو اضافه کن

    private ThrowableStone held;
    private float chargeT = 0f;
    private bool charging = false;

    // داخل StonePickupVR
    [Header("Arc Preview")]
    public bool showArc = true;
    public LineRenderer arcLine;
    public int arcResolution = 24;
    public float previewSpeed = 14f; // باید تقریباً با maxThrowSpeed همخوان باشه

    [Header("Hand Pose Link")]                 // NEW
    public ControllerHandCurl handCurl;        // NEW
    public float gripOnPickup = 1f;            // NEW
    public float gripOnRelease = 0f;           // NEW
    public bool driveTriggerToIndex = true;    // NEW

    void Reset()
    {
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

        // --- Charge & Throw ---
        bool holdTrig    = OVRInput.Get(throwHoldButton);
        bool releaseTrig = OVRInput.GetUp(throwHoldButton);

        // NEW: وقتی تریگر را نگه می‌داری، اشاره کمی بیشتر جمع شود
        if (driveTriggerToIndex && handCurl != null)
            handCurl.SetIndexAdd(holdTrig ? 1f : 0f);

        if (held != null && holdTrig)
        {
            // شروع/ادامه شارژ
            charging = true;
            if (!useGestureThrow)
                chargeT = Mathf.Clamp01(chargeT + Time.deltaTime / chargeTime);

            // پیش‌نمایش آرک
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
            // وقتی رها شدی یا سنگ در دست نیست، آرک رو خاموش کن
            if (arcLine && arcLine.enabled) arcLine.enabled = false;
        }

        if (held != null && charging && releaseTrig)
        {
            charging = false;
            if (arcLine) arcLine.enabled = false;
            Throw();
        }


    }

    void TryPickup()
    {
        // 1) Ray مستقیم
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickDistance, stoneLayer, QueryTriggerInteraction.Ignore))
        {
            var stone = hit.collider.GetComponentInParent<ThrowableStone>();
            if (stone != null && !stone.IsHeld) { Pickup(stone); return; }
        }

        // 2) نزدیک‌ترین جلو
        Collider[] hits = Physics.OverlapSphere(aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f),
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

    void Pickup(ThrowableStone stone)
    {
        held = stone;
        stone.PickUp(holdAnchor);                 // worldPositionStays داخل خود سنگ هندل شده
        chargeT = 0f;
        charging = false;

        if (handCurl) handCurl.SetGripTarget(gripOnPickup);   // NEW

        // هپتیک کوتاه
        TryBuzz(0.1f, 0.25f, 0.08f);
    }

    void Drop()
    {
        if (held == null) return;
        held.Drop();
        held = null;
        chargeT = 0f;
        charging = false;

        if (handCurl) { handCurl.SetGripTarget(gripOnRelease); handCurl.SetIndexAdd(0f); } // NEW
    }

    void Throw()
    {
        charging = false;

        Vector3 velocity;

        if (useGestureThrow)
        {
            // سرعت محلی کنترلر (در فضای Tracking)
            Vector3 vLocal = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            // تبدیل به ورلد
            Transform ts = trackingSpace ? trackingSpace : aimOrigin; // fallback
            Vector3 vWorld = ts.TransformVector(vLocal);

            // کمی بُست در راستای aim برای پایداری
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

        if (handCurl) { handCurl.SetGripTarget(gripOnRelease); handCurl.SetIndexAdd(0f); } // NEW

        // هپتیک پرتاب
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
    
    void DrawArc(Vector3 startPos, Vector3 startVel) {
        if (!arcLine) return;
        arcLine.positionCount = arcResolution;
        Vector3 p = startPos;
        Vector3 v = startVel;
        float dt = Time.fixedDeltaTime;

        for (int i = 0; i < arcResolution; i++) {
            arcLine.SetPosition(i, p);
            v += Physics.gravity * dt;
            p += v * dt;
        }
        arcLine.enabled = true;
    }
}
