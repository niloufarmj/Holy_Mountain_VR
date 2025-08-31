using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// PlanarReflectionURP
///
/// Attempts to implement real-time planar reflections in Unity URP by rendering a secondary
/// camera from a mirrored perspective and passing the texture into the water material.
///
/// ✅ Intended Features:
/// - Reflection texture rendered into a <see cref="RenderTexture"/>
/// - Assigns texture to material property (_PlanarReflectionTex by default)
/// - Supports custom reflection layers, texture resolution, clip-plane offset
/// - Optional shadow rendering for reflections
/// - Works with Shader Graph if you read from the global texture
///
/// ⚠️ Known Issues:
/// - This script is currently **bugged and does not work as intended** in URP.
/// - Reflections may not appear, or they may be incorrect/unstable.
/// - Future fixes may resolve the problems, but right now it is not reliable.
/// </summary>
[ExecuteAlways]
public class PlanarReflectionURP : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Usually the water plane itself.")]
    public Transform waterPlane;
    [Tooltip("Renderer of the water material (where the reflection texture will be set).")]
    public Renderer waterRenderer;
    [Tooltip("Material property name used for the reflection texture.")]
    public string textureProperty = "_PlanarReflectionTex";
    [Tooltip("Layers visible in the reflection.")]
    public LayerMask reflectLayers = ~0;
    [Tooltip("Resolution of the reflection texture (512 on Quest for performance).")]
    public int textureSize = 1024;
    [Tooltip("Offset for clipping plane to avoid z-fighting/flickering.")]
    public float clipPlaneOffset = 0.07f;

    [Header("Quality")]
    [Tooltip("Enable shadow rendering in reflection camera (expensive in VR).")]
    public bool renderShadows = false;

    Camera _mainCam;
    Camera _reflCam;
    RenderTexture _rt;

    static readonly int _texId = Shader.PropertyToID("_PlanarReflectionTex");

    void OnEnable()
    {
        if (!waterPlane) waterPlane = transform;
        if (!_mainCam) _mainCam = Camera.main;
        CreateOrResizeRT();
        CreateOrSetupReflectionCamera();
        UpdateMaterialTexture();
    }

    void OnDisable()
    {
        if (_reflCam) _reflCam.targetTexture = null;
        if (_rt) _rt.Release();
    }

    void Update()
    {
        if (!_mainCam) _mainCam = Camera.main;
        if (!_mainCam || !_reflCam || !_rt) return;

        // Sync reflection camera parameters
        _reflCam.cullingMask = reflectLayers & ~(1 << LayerMask.NameToLayer("Water"));
        _reflCam.allowMSAA = false;
        _reflCam.allowHDR = true;

        // Disable VR rendering for reflection camera
        _reflCam.stereoTargetEye = StereoTargetEyeMask.None;
        var add = _reflCam.GetUniversalAdditionalCameraData();
        if (add)
        {
            add.renderShadows = renderShadows;
            add.requiresColorOption = CameraOverrideOption.Off;
            add.requiresDepthOption = CameraOverrideOption.Off;
            add.allowXRRendering = false;
        }

        // Mirror matrix
        Vector3 pos = waterPlane.position;
        Vector3 normal = waterPlane.up;
        float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.identity;
        CalculateReflectionMatrix(ref reflection, plane);

        // Mirror camera position/rotation
        _reflCam.transform.position = ReflectPosition(_mainCam.transform.position, pos, normal);
        _reflCam.transform.rotation = ReflectRotation(_mainCam.transform.rotation, normal);

        // Match camera projection
        _reflCam.fieldOfView = _mainCam.fieldOfView;
        _reflCam.nearClipPlane = _mainCam.nearClipPlane;
        _reflCam.farClipPlane = _mainCam.farClipPlane;
        _reflCam.projectionMatrix = _mainCam.projectionMatrix;

        // Oblique clip plane
        Vector4 clipPlaneCameraSpace = CameraSpacePlane(_reflCam, pos, normal, 1.0f);
        _reflCam.projectionMatrix = _reflCam.CalculateObliqueMatrix(clipPlaneCameraSpace);

        // Render before main camera
        _reflCam.depth = _mainCam.depth - 1;
    }

    void LateUpdate()
    {
        // Ensure material always has the correct texture
        UpdateMaterialTexture();
    }

    void CreateOrResizeRT()
    {
        if (_rt != null && (_rt.width != textureSize || _rt.height != textureSize))
        {
            _rt.Release();
            _rt = null;
        }
        if (_rt == null)
        {
            _rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGBHalf)
            {
                name = "PlanarReflectionRT",
                useMipMap = false,
                autoGenerateMips = false
            };
            _rt.Create();
        }
    }

    void CreateOrSetupReflectionCamera()
    {
        if (!_reflCam)
        {
            GameObject go = new GameObject("PlanarReflectionCamera");
            go.hideFlags = HideFlags.HideAndDontSave;
            _reflCam = go.AddComponent<Camera>();
            var data = _reflCam.gameObject.GetComponent<UniversalAdditionalCameraData>();
            if (!data) data = _reflCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
        }
        _reflCam.enabled = true;
        _reflCam.targetTexture = _rt;
        _reflCam.clearFlags = CameraClearFlags.Skybox;
        _reflCam.cullingMask = reflectLayers;
    }

    void UpdateMaterialTexture()
    {
        if (waterRenderer && waterRenderer.sharedMaterial)
            waterRenderer.sharedMaterial.SetTexture(textureProperty, _rt);

        // Also set globally for Shader Graph
        Shader.SetGlobalTexture(_texId, _rt);
    }

    // --- Helpers ---

    static Vector3 ReflectPosition(Vector3 p, Vector3 planePos, Vector3 planeNormal)
    {
        float dist = Vector3.Dot(planeNormal, p - planePos);
        return p - 2f * dist * planeNormal;
    }

    static Quaternion ReflectRotation(Quaternion r, Vector3 planeNormal)
    {
        Vector3 f = Vector3.Reflect(r * Vector3.forward, planeNormal);
        Vector3 u = Vector3.Reflect(r * Vector3.up, planeNormal);
        return Quaternion.LookRotation(f, u);
    }

    static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * 0.01f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cPos = m.MultiplyPoint(offsetPos);
        Vector3 cNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }

    static void CalculateReflectionMatrix(ref Matrix4x4 m, Vector4 p)
    {
        m.m00 = (1F - 2F * p[0] * p[0]); m.m01 = (-2F * p[0] * p[1]);    m.m02 = (-2F * p[0] * p[2]);    m.m03 = (-2F * p[3] * p[0]);
        m.m10 = (-2F * p[1] * p[0]);    m.m11 = (1F - 2F * p[1] * p[1]); m.m12 = (-2F * p[1] * p[2]);    m.m13 = (-2F * p[3] * p[1]);
        m.m20 = (-2F * p[2] * p[0]);    m.m21 = (-2F * p[2] * p[1]);    m.m22 = (1F - 2F * p[2] * p[2]); m.m23 = (-2F * p[3] * p[2]);
        m.m30 = 0F;                     m.m31 = 0F;                     m.m32 = 0F;                      m.m33 = 1F;
    }
}
