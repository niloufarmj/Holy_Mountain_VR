using UnityEngine;

[DefaultExecutionOrder(100)] // تا بعد از حرکت بازیکن آپدیت شویم
public class SpectatorCamFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetRoot;   // ریشه‌ی بازیکن (بدن/rig)
    public Transform lookAt;       // CenterEyeAnchor (جایی که باید به آن نگاه کنیم)

    [Header("Framing")]
    public Vector3 offsetLocal = new Vector3(0.6f, 1.7f, -3.8f); // راست، بالا، عقب (در فضای Yaw بازیکن)
    public bool yawOnly = true;    // فقط با یاوِ بازیکن بچرخد (پایدارتر)
    public float posLerp = 8f;
    public float rotLerp = 12f;

    [Header("Collision Avoidance")]
    public bool avoidObstacles = true;
    public float collisionRadius = 0.22f;
    public float minDistance = 1.2f;
    public float maxDistance = 6.0f;
    public LayerMask collisionMask = ~0; // هرچیز به‌جز لایه‌ی Player (ترجیحاً Player را از ماسک حذف کن)

    void LateUpdate()
    {
        if (!targetRoot) return;

        // 1) محاسبه‌ی موقعیت مطلوب بر اساس یاو
        Quaternion yawRot = yawOnly
            ? Quaternion.Euler(0f, targetRoot.eulerAngles.y, 0f)
            : targetRoot.rotation;

        Vector3 desiredPos = targetRoot.position + (yawRot * offsetLocal);
        Vector3 focus = (lookAt ? lookAt.position : targetRoot.position);

        // 2) جلوگیری از ورود دوربین به داخل اشیاء (SphereCast از نقطه‌ی نگاه تا جای مطلوب)
        if (avoidObstacles)
        {
            Vector3 dir = desiredPos - focus;
            float dist = Mathf.Clamp(dir.magnitude, minDistance, maxDistance);
            dir = dir.normalized;

            if (Physics.SphereCast(focus, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // دوربین را نزدیکترِ نقطه‌ی برخورد قرار بده
                desiredPos = hit.point - dir * 0.05f; // کمی فاصله از سطح
            }
            else
            {
                desiredPos = focus + dir * dist;
            }
        }

        // 3) درونیابی نرم
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * posLerp);

        // 4) نگاه به بازیکن (yaw-only اختیاری)
        Vector3 toFocus = focus - transform.position;
        if (yawOnly) toFocus = Vector3.ProjectOnPlane(toFocus, Vector3.up);
        if (toFocus.sqrMagnitude > 1e-6f)
        {
            Quaternion wantRot = Quaternion.LookRotation(toFocus.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wantRot, Time.deltaTime * rotLerp);
        }
    }

    // کمک: برای تنظیم سریع در ادیتور
    void OnDrawGizmosSelected()
    {
        if (!targetRoot) return;
        Gizmos.color = new Color(0,1,1,0.4f);
        Quaternion yawRot = Quaternion.Euler(0f, targetRoot.eulerAngles.y, 0f);
        Vector3 p = targetRoot.position + (yawRot * offsetLocal);
        Gizmos.DrawSphere(p, 0.05f);
        if (lookAt) Gizmos.DrawLine(lookAt.position, p);
    }
}
