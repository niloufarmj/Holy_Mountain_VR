using UnityEngine;

public class InventoryFollowHead : MonoBehaviour
{
    public Transform cameraTransform;      // CenterEyeAnchor
    public float distance = 0.9f;
    public float heightOffset = -0.1f;
    public float followLerp = 12f;
    public bool yawOnly = true;

    [Header("Fixes")]
    public bool fixMirroringByParity = true;   // اگر true باشد، اسکیل محلی را طوری تنظیم می‌کند که حاصل‌ضرب مقیاس‌های جهانی مثبت شود
    public bool debugLogs = false;

    void LateUpdate()
    {
        if (!cameraTransform) return;

        // 1) جای پنل جلوِ چشم
        Vector3 fwd = cameraTransform.forward;
        if (yawOnly) fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;

        Vector3 targetPos = cameraTransform.position + fwd * distance + Vector3.up * heightOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followLerp);

        // 2) چرخش: پنل باید «به سمتِ دوربین» نگاه کند
        Vector3 toCam = cameraTransform.position - transform.position;
        if (yawOnly) toCam = Vector3.ProjectOnPlane(toCam, Vector3.up);
        if (toCam.sqrMagnitude < 1e-6f) toCam = transform.forward;

        // رو به دوربین
        Quaternion want = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * followLerp);

        // 3) اگر به هر دلیل هنوز پشت به دوربین بود، 180 درجه بچرخان
        float facingDot = Vector3.Dot(transform.forward, (cameraTransform.position - transform.position).normalized);
        if (facingDot < 0f)
        {
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + 180f, 0f);
        }

        // 4) فیکس ریشه‌ای mirroring: اگر حاصل‌ضرب مقیاس‌های جهانی منفی بود، localScale.x را برعکس می‌کنیم
        if (fixMirroringByParity)
        {
            Vector3 ls = transform.lossyScale;
            float parity = Mathf.Sign(ls.x) * Mathf.Sign(ls.y) * Mathf.Sign(ls.z); // <0 یعنی آینه‌ای
            if (parity < 0f)
            {
                Vector3 l = transform.localScale;
                transform.localScale = new Vector3(-l.x, l.y, l.z); // یک محور را برعکس کن تا parity مثبت شود
                if (debugLogs) Debug.Log("[InventoryFollowHead] Mirroring fixed by flipping localScale.x");
            }
        }
    }
}
