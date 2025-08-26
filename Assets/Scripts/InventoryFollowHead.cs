using UnityEngine;

public class UIStickToHMD : MonoBehaviour
{
    [Header("HMD / Camera")]
    public Transform cameraTransform;        // بزار CenterEyeAnchor یا Camera.main
    public bool autoFindCamera = true;

    [Header("Placement")]
    public float distance = 0.9f;            // جلو چشم
    public float verticalOffset = -0.05f;    // کمی پایین‌تر از خط دید
    public bool yawOnly = true;              // فقط چرخش افقی (VR-friendly)
    public float smooth = 14f;               // نرمی حرکت/چرخش

    void Awake()
    {
        if (autoFindCamera && !cameraTransform)
        {
            var cam = Camera.main;
            if (cam) cameraTransform = cam.transform;
            if (!cameraTransform)
            {
                var t = GameObject.Find("CenterEyeAnchor");
                if (t) cameraTransform = t.transform;
            }
        }

        // اطمینان از اسکیل مثبت (جلوگیری از میرِر شدن)
        var ls = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
    }

    void LateUpdate()
    {
        if (!cameraTransform) return;

        // جهت جلو + آپ
        Vector3 fwd = cameraTransform.forward;
        Vector3 up  = cameraTransform.up;

        if (yawOnly)
        {
            fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            up  = Vector3.up;
        }

        // جای‌گذاری جلوِ صورت
        Vector3 targetPos = cameraTransform.position + fwd * distance + up * verticalOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        // *** نکته کلیدی: به خودِ دوربین نگاه کن (LookAt) تا اشتباه علامت پیش نیاد ***
        Vector3 toCam = cameraTransform.position - transform.position;
        if (yawOnly) { toCam.y = 0f; if (toCam.sqrMagnitude < 1e-6f) toCam = fwd; }
        Quaternion want = Quaternion.LookRotation(toCam.normalized, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
