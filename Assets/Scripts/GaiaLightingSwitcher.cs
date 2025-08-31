using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages transitions between multiple lighting phases in a scene.
/// Each phase can include a directional light setup, post-processing volume,
/// and a cubemap skybox. Transitions are smoothly blended over time,
/// including light color/intensity, rotation, post-processing volumes, and skybox cubemaps.
/// </summary>
public class GaiaLightingSwitcher : MonoBehaviour
{
    /// <summary>
    /// A lighting "phase" representing one stage of the day/night cycle.
    /// Includes skybox cubemap, directional light, and post-processing volume.
    /// </summary>
    [System.Serializable]
    public class LightingPhase
    {
        [Tooltip("Friendly name for this lighting phase (e.g., Morning, Night).")]
        public string name;

        [Tooltip("Directional light prefab representing this phase.")]
        public Light lighting;

        [Tooltip("Post-processing volume to apply during this phase.")]
        public Volume volume;

        [Tooltip("Material reference (not directly used, just for extracting cubemap).")]
        public Material skyboxMaterial;

        [Tooltip("Cubemap skybox for this phase.")]
        public Cubemap skyboxCubemap;

        [Tooltip("How long this phase lasts before blending to the next.")]
        public float duration = 60f;
    }

    [Header("Phases Configuration")]
    [Tooltip("Array of lighting phases (cycled through sequentially).")]
    public LightingPhase[] phases;

    [Tooltip("Duration of blending between phases.")]
    public float blendDuration = 5f;

    [Tooltip("Directional light in the scene controlled by blending.")]
    public Light directionalLight;

    [Header("Blended Skybox Setup")]
    [Tooltip("Material that supports blending between two cubemap textures.")]
    public Material blendedSkyboxMaterial;

    private int currentIndex = 0;
    private float phaseTimer = 0f;
    private float blendTimer = 0f;
    private bool isBlending = false;

    // Directional light blending data
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private Color startLightColor;
    private Color targetLightColor;
    private float startLightIntensity;
    private float targetLightIntensity;

    // Skybox blending/rotation
    private float currentRotation = 0f;
    [Tooltip("Speed at which the skybox rotates over time.")]
    public float skyboxRotationSpeed = 0.5f;

    private Cubemap currentCubemap;
    private Cubemap nextCubemap;

    // Post-processing volume blending
    private Volume currentVolume;
    private Volume nextVolume;

    private void Start()
    {
        StartPhase(currentIndex);
    }

    private void Update()
    {
        if (!isBlending)
        {
            // Count down the active phase
            phaseTimer += Time.deltaTime;
            if (phaseTimer >= phases[currentIndex].duration)
            {
                StartBlendToNext();
            }
        }
        else
        {
            // Blend progress ratio
            blendTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blendTimer / blendDuration);

            // Blend directional light color, intensity, and rotation
            directionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
            directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
            directionalLight.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            // Update skybox blending
            blendedSkyboxMaterial.SetFloat("_Blend", t);
            blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);

            // Blend post-processing volumes
            if (currentVolume != null) currentVolume.weight = 1f - t;
            if (nextVolume != null) nextVolume.weight = t;

            if (t >= 1f)
            {
                EndBlend();
            }
        }

        // Rotate skybox continuously regardless of blending state
        currentRotation += skyboxRotationSpeed * Time.deltaTime;
        if (blendedSkyboxMaterial != null)
        {
            blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);
        }
    }

    /// <summary>
    /// Initializes a specific lighting phase and applies its skybox, light, and volume.
    /// </summary>
    private void StartPhase(int index)
    {
        currentIndex = index;
        phaseTimer = 0f;

        var phase = phases[currentIndex];

        // Initialize skybox with this phase's cubemap
        currentCubemap = phase.skyboxCubemap;
        blendedSkyboxMaterial.SetTexture("_SkyboxA", currentCubemap);
        blendedSkyboxMaterial.SetTexture("_SkyboxB", currentCubemap);
        blendedSkyboxMaterial.SetFloat("_Blend", 0f);
        blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);
        RenderSettings.skybox = blendedSkyboxMaterial;

        // Enable all volumes so weights can be blended
        foreach (var p in phases)
        {
            if (p.volume != null)
                p.volume.enabled = true;
        }

        // Activate this phase's volume fully
        currentVolume = phase.volume;
        if (currentVolume != null) currentVolume.weight = 1f;

        // Apply directional light properties
        var light = phase.lighting;
        if (light != null)
        {
            directionalLight.color = light.color;
            directionalLight.intensity = light.intensity;
            directionalLight.transform.rotation = light.transform.rotation;
        }
    }

    /// <summary>
    /// Starts blending from the current phase to the next sequential phase.
    /// </summary>
    private void StartBlendToNext()
    {
        isBlending = true;
        blendTimer = 0f;

        int nextIndex = (currentIndex + 1) % phases.Length;
        var nextPhase = phases[nextIndex];

        // Skybox blending setup
        currentCubemap = phases[currentIndex].skyboxCubemap;
        nextCubemap = nextPhase.skyboxCubemap;
        blendedSkyboxMaterial.SetTexture("_SkyboxA", currentCubemap);
        blendedSkyboxMaterial.SetTexture("_SkyboxB", nextCubemap);
        blendedSkyboxMaterial.SetFloat("_Blend", 0f);
        RenderSettings.skybox = blendedSkyboxMaterial;

        // Post-processing volume blending setup
        currentVolume = phases[currentIndex].volume;
        nextVolume = nextPhase.volume;
        if (currentVolume != null) currentVolume.weight = 1f;
        if (nextVolume != null) nextVolume.weight = 0f;

        // Prepare directional light interpolation
        var currentLight = phases[currentIndex].lighting;
        var nextLight = nextPhase.lighting;

        startLightColor = currentLight.color;
        targetLightColor = nextLight.color;

        startLightIntensity = currentLight.intensity;
        targetLightIntensity = nextLight.intensity;

        startRotation = currentLight.transform.rotation;
        targetRotation = nextLight.transform.rotation;
    }

    /// <summary>
    /// Ends the blending and starts the new phase as active.
    /// </summary>
    private void EndBlend()
    {
        isBlending = false;
        blendTimer = 0f;
        currentIndex = (currentIndex + 1) % phases.Length;

        StartPhase(currentIndex);
    }

    /// <summary>
    /// Utility check to determine if the current phase represents a "night" cycle.
    /// </summary>
    public bool IsNight()
    {
        var currentPhase = phases[currentIndex];
        return currentPhase.name.ToLower().Contains("night") || currentPhase.name.ToLower().Contains("sleep");
    }
}
