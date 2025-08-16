using UnityEngine;

public class StonePickupVR : MonoBehaviour
{
    [Header("Refs")]
    public Transform aimOrigin;         // RightControllerAnchor (forward برای Ray)
    public Transform holdAnchor;        // ViewHoldAnchor (جلوی چشم)
    public LayerMask stoneLayer;        // فقط لایه‌ی Stone

    [Header("Pickup Settings")]
    public float pickDistance = 3f;     // حداکثر فاصله‌ی برداشتن
    public float sphereRadius = 0.2f;   // کمک برای انتخاب داخل یک مخروط کوچک
    public OVRInput.Button pickupButton = OVRInput.Button.One; // دکمه A
    public KeyCode pickupKey = KeyCode.E;                       // برای تست PC

    private ThrowableStone held;

    void Reset()
    {
        // اگر لایه ست نشده بود، اتومات Stone رو پیدا کن
        if (stoneLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Stone");
            if (idx >= 0) stoneLayer = 1 << idx;
        }
    }

    void Update()
    {
        bool pressed = OVRInput.GetDown(pickupButton) || Input.GetKeyDown(pickupKey);

        if (!pressed) return;

        if (held == null) TryPickup();
        else Drop();
    }

    void TryPickup()
    {
        // 1) Raycast مستقیم از کنترلر
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickDistance, stoneLayer, QueryTriggerInteraction.Ignore))
        {
            var stone = hit.collider.GetComponentInParent<ThrowableStone>();
            if (stone != null && !stone.IsHeld)
            {
                Pickup(stone);
                return;
            }
        }

        // 2) اگر Ray چیزی نزد، نزدیک‌ترین سنگ تو یک مخروط کوچک جلو را پیدا کن
        Collider[] hits = Physics.OverlapSphere(aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f),
                                                sphereRadius, stoneLayer, QueryTriggerInteraction.Ignore);
        ThrowableStone best = null;
        float bestDot = 0.75f; // فقط چیزهایی که تقریباً جلو هستن

        for (int i = 0; i < hits.Length; i++)
        {
            var s = hits[i].GetComponentInParent<ThrowableStone>();
            if (s == null || s.IsHeld) continue;

            Vector3 to = (s.transform.position - aimOrigin.position).normalized;
            float d = Vector3.Dot(aimOrigin.forward, to);
            if (d > bestDot)
            {
                bestDot = d;
                best = s;
            }
        }

        if (best != null) Pickup(best);
        // اگر هیچی نبود، کار خاصی نکن
    }

    void Pickup(ThrowableStone stone)
    {
        held = stone;
        stone.PickUp(holdAnchor);

        // اختیاری: کمی چرخش بده که خوشگل‌تر دیده شه
        held.transform.localRotation = Quaternion.identity;

        // هپتیک کوتاه (اختیاری)
        try { OVRInput.SetControllerVibration(0.1f, 0.2f, OVRInput.Controller.RTouch); } catch {}
        Invoke(nameof(StopHaptics), 0.08f);
    }

    void Drop()
    {
        if (held == null) return;
        held.Drop();
        held = null;
    }

    void StopHaptics()
    {
        try { OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch); } catch {}
    }

    void OnDrawGizmosSelected()
    {
        if (!aimOrigin) return;
        Gizmos.color = new Color(0,1,1,0.25f);
        Gizmos.DrawLine(aimOrigin.position, aimOrigin.position + aimOrigin.forward * pickDistance);
        Gizmos.DrawWireSphere(aimOrigin.position + aimOrigin.forward * (pickDistance * 0.5f), sphereRadius);
    }
}
