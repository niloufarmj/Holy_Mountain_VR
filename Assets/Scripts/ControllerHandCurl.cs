// ControllerHandCurl.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives finger curl animation for Oculus hand skeletons by directly rotating bone transforms.
/// 
/// ✅ Features:
/// - Supports left/right hand and automatic bone mapping from <see cref="OVRSkeleton"/>
/// - Adjustable grip strength (0–1), lerping for smoothness
/// - Optional reading of controller grip + trigger inputs
/// - Customizable rotation axes/inversions for fingers and thumb
/// - Thumb uses different axes for proximal vs. other joints
/// - Debug visualization of grip value
/// - Public API to externally drive grip or pulse animations
///
/// ⚠️ Important Notes:
/// - The result is **not perfectly realistic**: finger motion looks mechanical.
/// - The main issue is with the **four fingers (not the thumb)**: their curling does not fully match natural human hand motion.
/// - Despite these limitations, the effect is usually *acceptable for gameplay* and gives the impression of hand grasping.
/// </summary>
public class ControllerHandCurl : MonoBehaviour
{
    public enum Handed { Left, Right }
    public enum Axis { X, Y, Z }

    [Header("Setup")]
    public Handed handed = Handed.Left;
    public OVRSkeleton skeleton;

    [Header("Drive")]
    [Tooltip("If true, also read controller input (grip/trigger) to drive curl. Useful for quick testing.")]
    public bool alsoReadController = false;
    [Range(0, 1)] public float targetGrip = 0f; // Grip target (0 = open, 1 = fist)
    [Range(0, 1)] public float indexAdd = 0f;   // Extra curl for index finger (trigger)
    public float gripLerpSpeed = 12f;           // How fast grip interpolates
    public bool useOnBeforeRender = true;       // Apply before rendering for smoother visuals

    [Header("Axes (set once, then forget)")]
    [Tooltip("Recommended for Oculus: fingers rotate around -X, thumb base around -Z, others -X.")]
    public Axis fingerAxis = Axis.X;
    public bool fingerInvert = true;
    public Axis thumbProxAxis = Axis.Z;
    public bool thumbProxInvert = true;
    public Axis thumbOtherAxis = Axis.X;
    public bool thumbOtherInvert = true;

    [Header("Angles (degrees)")]
    [Tooltip("Maximum bend angle for finger proximal joint (knuckle).")]
    public float proximalMax = 120f;
    [Tooltip("Maximum bend angle for finger intermediate joint (middle).")]
    public float intermediateMax = 140f;
    [Tooltip("Maximum bend angle for finger distal joint (tip).")]
    public float distalMax = 100f;
    [Tooltip("Maximum bend angle for thumb joints.")]
    public float thumbMax = 110f;

    [Header("Debug Visualization")]
    public bool showGripValue = true;

    [Header("DEBUG")]
    public bool verbose = false;

    float _grip;
    bool _ready;
    readonly Dictionary<OVRSkeleton.BoneId, Transform> bone = new();
    readonly Dictionary<OVRSkeleton.BoneId, Quaternion> baseRot = new();

    void Awake()
    {
        if (!skeleton) skeleton = GetComponent<OVRSkeleton>();
    }

    void OnEnable()
    {
        StartCoroutine(WaitAndInit());
        if (useOnBeforeRender) Application.onBeforeRender += OnBeforeRender;
    }

    void OnDisable()
    {
        if (useOnBeforeRender) Application.onBeforeRender -= OnBeforeRender;
    }

    IEnumerator WaitAndInit()
    {
        // Wait until skeleton is initialized
        while (!skeleton || skeleton.Bones == null || skeleton.Bones.Count < 20)
            yield return null;

        bone.Clear(); baseRot.Clear();
        foreach (var b in skeleton.Bones)
        {
            bone[b.Id] = b.Transform;
            baseRot[b.Id] = b.Transform.localRotation;
        }
        _ready = true;
        if (verbose) Debug.Log($"[{handed}] Bones READY: {skeleton.Bones.Count}", this);
    }

    void LateUpdate()
    {
        if (!_ready) return;
        Apply();
    }

    void Update()
    {
        // Manual debug toggle with T key
        if (Input.GetKeyDown(KeyCode.T))
        {
            targetGrip = targetGrip > 0.5f ? 0f : 0.95f;
            Debug.Log($"{handed} Hand grip set to: {targetGrip}");
        }
    }

    void OnBeforeRender()
    {
        if (!_ready) return;
        Apply();
    }

    /// <summary>
    /// Apply grip/trigger values and rotate bones accordingly.
    /// </summary>
    void Apply()
    {
        float g = targetGrip;

        // Optionally mix in real controller values
        if (alsoReadController)
        {
            var ctrl = handed == Handed.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            g = Mathf.Max(g, Mathf.Clamp01(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, ctrl)));
            indexAdd = Mathf.Max(indexAdd, Mathf.Clamp01(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, ctrl)));
        }

        // Smooth interpolation of grip
        _grip = Mathf.Lerp(_grip, g, 1f - Mathf.Exp(-gripLerpSpeed * Time.unscaledDeltaTime));

        // Exponential curve: faster closing at start
        float curvedGrip = Mathf.Pow(_grip, 0.5f);

        // --- Apply curl to four fingers ---
        CurlFinger(OVRSkeleton.BoneId.Hand_Middle1, OVRSkeleton.BoneId.Hand_Middle2, OVRSkeleton.BoneId.Hand_Middle3, curvedGrip);
        CurlFinger(OVRSkeleton.BoneId.Hand_Ring1, OVRSkeleton.BoneId.Hand_Ring2, OVRSkeleton.BoneId.Hand_Ring3, curvedGrip);
        CurlFinger(OVRSkeleton.BoneId.Hand_Pinky1, OVRSkeleton.BoneId.Hand_Pinky2, OVRSkeleton.BoneId.Hand_Pinky3, curvedGrip);

        // Index finger can curl extra when trigger pressed
        float idx = Mathf.Clamp01(curvedGrip + indexAdd * (1f - curvedGrip));
        CurlFinger(OVRSkeleton.BoneId.Hand_Index1, OVRSkeleton.BoneId.Hand_Index2, OVRSkeleton.BoneId.Hand_Index3, idx);

        // Thumb - handled differently (more aggressive bend)
        CurlThumb(OVRSkeleton.BoneId.Hand_Thumb1, OVRSkeleton.BoneId.Hand_Thumb2, OVRSkeleton.BoneId.Hand_Thumb3, curvedGrip * 1.3f);

        // Reset indexAdd over time
        indexAdd = Mathf.MoveTowards(indexAdd, 0f, Time.unscaledDeltaTime * 4f);
    }

    // --- Helper methods ---

    void CurlFinger(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip, float t)
    {
        Curl3(knuckle, middle, tip, t, fingerAxis, fingerInvert);
    }

    void CurlThumb(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip, float t)
    {
        Curl3(knuckle, middle, tip, Mathf.Clamp01(t),
              thumbProxAxis, thumbProxInvert, thumbOtherAxis, thumbOtherInvert);
    }

    void Curl3(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip,
               float t, Axis axis, bool invert)
    {
        if (bone.TryGetValue(knuckle, out var bKnuckle))
            bKnuckle.localRotation = baseRot[knuckle] * AxisRot(axis, proximalMax * t, invert);
        if (bone.TryGetValue(middle, out var bMiddle))
            bMiddle.localRotation = baseRot[middle] * AxisRot(axis, intermediateMax * t, invert);
        if (bone.TryGetValue(tip, out var bTip))
            bTip.localRotation = baseRot[tip] * AxisRot(axis, distalMax * t, invert);
    }

    void Curl3(OVRSkeleton.BoneId knuckle, OVRSkeleton.BoneId middle, OVRSkeleton.BoneId tip,
               float t, Axis proxAxis, bool proxInv, Axis otherAxis, bool otherInv)
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

    // --- Public API ---

    public void SetGripTarget(float v)
    {
        targetGrip = Mathf.Clamp01(v);
        if (verbose) Debug.Log($"[{handed}] SetGripTarget {targetGrip}", this);
    }

    public void SetIndexAdd(float v)
    {
        indexAdd = Mathf.Clamp01(v);
        if (verbose) Debug.Log($"[{handed}] SetIndexAdd {indexAdd}", this);
    }

    public void PulseGrip(float to, float hold, float back)
    {
        StopAllCoroutines();
        StartCoroutine(Pulse(to, hold, back));
    }

    IEnumerator Pulse(float to, float hold, float back)
    {
        float prev = targetGrip;
        targetGrip = Mathf.Clamp01(to);
        yield return new WaitForSeconds(hold);

        float t = 0;
        while (t < back)
        {
            t += Time.unscaledDeltaTime;
            targetGrip = Mathf.Lerp(to, prev, t / back);
            yield return null;
        }
        targetGrip = prev;
    }

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
            Debug.Log($"{boneInfo.Id}: {boneInfo.Transform.name} - Pos: {boneInfo.Transform.localPosition}");
    }

    void OnGUI()
    {
        if (showGripValue && Application.isPlaying)
        {
            GUI.Label(new Rect(10, handed == Handed.Left ? 30 : 60, 300, 20),
                $"{handed} Hand Grip: {_grip:F2}");
        }
    }
}
