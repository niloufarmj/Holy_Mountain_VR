// ControllerHandCurl_Axis.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerHandCurl : MonoBehaviour
{
    public enum Handed { Left, Right }
    public enum Axis { X, Y, Z }

    [Header("Setup")]
    public Handed handed = Handed.Left;
    public OVRSkeleton skeleton;

    [Header("Drive")]
    public bool alsoReadController = false;     // برای تست سریع می‌تونی روشن کنی
    [Range(0, 1)] public float targetGrip = 0f;  // 0..1
    [Range(0, 1)] public float indexAdd = 0f;  // تقویت انگشت اشاره (تریگر)
    public float gripLerpSpeed = 12f;
    public bool useOnBeforeRender = true;


    [Header("Axes (set once, then forget)")]
    // توصیه برای Oculus: انگشت‌ها حول X- می‌چرخند، شست بند اول حول Z- و بقیه X-
    public Axis fingerAxis = Axis.X;
    public bool fingerInvert = true;       // true یعنی به سمت داخل مشت شود
    public Axis thumbProxAxis = Axis.Z;    // بند اول شست
    public bool thumbProxInvert = true;
    public Axis thumbOtherAxis = Axis.X;   // بندهای 2 و 3 شست
    public bool thumbOtherInvert = true;

    [Header("Angles (deg)")]
    public float proximalMax = 120f;     // Knuckle bend - VERY aggressive
    public float intermediateMax = 140f; // Middle joint bend
    public float distalMax = 100f;       // Tip joint bend
    public float thumbMax = 110f;

    [Header("Debug Visualization")]
    public bool showGripValue = true;

    void OnGUI()
    {
        if (showGripValue && Application.isPlaying)
        {
            GUI.Label(new Rect(10, handed == Handed.Left ? 30 : 60, 300, 20),
                    $"{handed} Hand Grip: {_grip:F2}");
        }
    }

    [Header("DEBUG")]
    public bool verbose = false;

    float _grip; bool _ready;
    readonly Dictionary<OVRSkeleton.BoneId, Transform> bone = new();
    readonly Dictionary<OVRSkeleton.BoneId, Quaternion> baseRot = new();

    void Awake() { if (!skeleton) skeleton = GetComponent<OVRSkeleton>(); }
    void OnEnable() { StartCoroutine(WaitAndInit()); if (useOnBeforeRender) Application.onBeforeRender += OnBeforeRender; }
    void OnDisable() { if (useOnBeforeRender) Application.onBeforeRender -= OnBeforeRender; }

    IEnumerator WaitAndInit()
    {
        while (!skeleton || skeleton.Bones == null || skeleton.Bones.Count < 20) yield return null;
        bone.Clear(); baseRot.Clear();
        foreach (var b in skeleton.Bones) { bone[b.Id] = b.Transform; baseRot[b.Id] = b.Transform.localRotation; }
        _ready = true;
        if (verbose) Debug.Log($"[{handed}] Bones READY: {skeleton.Bones.Count}", this);
    }

    void LateUpdate() { if (!_ready) return; Apply(); }

    void Update()
    {
        // Manual test - press T to toggle grip
        if (Input.GetKeyDown(KeyCode.T))
        {
            targetGrip = targetGrip > 0.5f ? 0f : 0.95f;
            Debug.Log($"{handed} Hand grip set to: {targetGrip}");
        }
    }

    void OnBeforeRender() { if (!_ready) return; Apply(); }

    void Apply()
    {
        float g = targetGrip;
        if (alsoReadController)
        {
            var ctrl = handed == Handed.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            g = Mathf.Max(g, Mathf.Clamp01(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, ctrl)));
            indexAdd = Mathf.Max(indexAdd, Mathf.Clamp01(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, ctrl)));
        }
        _grip = Mathf.Lerp(_grip, g, 1f - Mathf.Exp(-gripLerpSpeed * Time.unscaledDeltaTime));

        // Use exponential curve for more dramatic movement
        float curvedGrip = Mathf.Pow(_grip, 0.5f); // Square root curve for faster initial movement

        // Four fingers - make sure we're using the correct bone IDs
        CurlFinger(OVRSkeleton.BoneId.Hand_Middle1, OVRSkeleton.BoneId.Hand_Middle2, OVRSkeleton.BoneId.Hand_Middle3, curvedGrip);
        CurlFinger(OVRSkeleton.BoneId.Hand_Ring1, OVRSkeleton.BoneId.Hand_Ring2, OVRSkeleton.BoneId.Hand_Ring3, curvedGrip);
        CurlFinger(OVRSkeleton.BoneId.Hand_Pinky1, OVRSkeleton.BoneId.Hand_Pinky2, OVRSkeleton.BoneId.Hand_Pinky3, curvedGrip);

        float idx = Mathf.Clamp01(curvedGrip + indexAdd * (1f - curvedGrip));
        CurlFinger(OVRSkeleton.BoneId.Hand_Index1, OVRSkeleton.BoneId.Hand_Index2, OVRSkeleton.BoneId.Hand_Index3, idx);

        // Thumb - more aggressive
        CurlThumb(OVRSkeleton.BoneId.Hand_Thumb1, OVRSkeleton.BoneId.Hand_Thumb2, OVRSkeleton.BoneId.Hand_Thumb3, curvedGrip * 1.3f);

        indexAdd = Mathf.MoveTowards(indexAdd, 0f, Time.unscaledDeltaTime * 4f);
    }


    // Helper methods for clearer finger control
    void CurlFinger(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip, float t)
    {
        Curl3(knuckle, middle, tip, t, fingerAxis, fingerInvert);
    }


    void CurlThumb(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip, float t)
    {
        Curl3(knuckle, middle, tip, Mathf.Clamp01(t),
              thumbProxAxis, thumbProxInvert, thumbOtherAxis, thumbOtherInvert);
    }


    // Replace the Curl3 methods in ControllerHandCurl.cs with these:

    void Curl3(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip, float t, Axis axis, bool invert)
    {
        // Knuckle (MCP joint) - rotates the most
        if (bone.TryGetValue(knuckle, out var bKnuckle))
            bKnuckle.localRotation = baseRot[knuckle] * AxisRot(axis, proximalMax * t, invert);

        // Middle joint (PIP) - rotates moderately
        if (bone.TryGetValue(middle, out var bMiddle))
            bMiddle.localRotation = baseRot[middle] * AxisRot(axis, intermediateMax * t, invert);

        // Tip joint (DIP) - rotates the least
        if (bone.TryGetValue(tip, out var bTip))
            bTip.localRotation = baseRot[tip] * AxisRot(axis, distalMax * t, invert);
    }


    void Curl3(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip, float t,
               Axis proxAxis, bool proxInv, Axis otherAxis, bool otherInv)
    {
        if (bone.TryGetValue(knuckle, out var bKnuckle))
            bKnuckle.localRotation = baseRot[knuckle] * AxisRot(proxAxis, thumbMax * t, proxInv);
        if (bone.TryGetValue(middle, out var bMiddle))
            bMiddle.localRotation = baseRot[middle] * AxisRot(otherAxis, thumbMax * t, otherInv);
        if (bone.TryGetValue(tip, out var bTip))
            bTip.localRotation = baseRot[tip] * AxisRot(otherAxis, thumbMax * t, otherInv);
    }

    Quaternion AxisRot(Axis a, float deg, bool invert)
    {
        float s = invert ? -1f : 1f;
        return a switch
        {
            Axis.X => Quaternion.Euler(deg * s, 0, 0),
            Axis.Y => Quaternion.Euler(0, deg * s, 0),
            _ => Quaternion.Euler(0, 0, deg * s)
        };
    }

    // API
    public void SetGripTarget(float v) { targetGrip = Mathf.Clamp01(v); if (verbose) Debug.Log($"[{handed}] SetGripTarget {targetGrip}", this); }
    public void SetIndexAdd(float v) { indexAdd = Mathf.Clamp01(v); if (verbose) Debug.Log($"[{handed}] SetIndexAdd {indexAdd}", this); }
    public void PulseGrip(float to, float hold, float back) { StopAllCoroutines(); StartCoroutine(Pulse(to, hold, back)); }
    IEnumerator Pulse(float to, float hold, float back)
    {
        float prev = targetGrip; targetGrip = Mathf.Clamp01(to);
        yield return new WaitForSeconds(hold);
        float t = 0; while (t < back) { t += Time.unscaledDeltaTime; targetGrip = Mathf.Lerp(to, prev, t / back); yield return null; }
        targetGrip = prev;
    }

    // Add to ControllerHandCurl.cs
    void OnValidate()
    {
        if (verbose && Application.isPlaying && _ready)
        {
            Debug.Log($"[{handed}] Current grip: {_grip}, Target: {targetGrip}");
        }
    }


    void LogBoneInfo()
    {
        if (!skeleton || skeleton.Bones == null) return;

        foreach (var boneInfo in skeleton.Bones)
        {
            Debug.Log($"{boneInfo.Id}: {boneInfo.Transform.name} - Pos: {boneInfo.Transform.localPosition}");
        }
    }

    // Call this in Start() after initialization to verify bones


}
