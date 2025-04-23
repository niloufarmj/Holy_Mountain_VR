using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaiaLightingSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class LightingPhase
    {
        public string name;
        public Light lighting;
        public Volume volume;
        public Material skyboxMaterial; // Only used to extract Cubemap
        public Cubemap skyboxCubemap;
        public float duration = 60f;
    }

    public LightingPhase[] phases;
    public float blendDuration = 5f;
    public Light directionalLight;

    [Header("Blended Skybox Setup")]
    public Material blendedSkyboxMaterial; // Material using the Shader Graph

    private int currentIndex = 0;
    private float phaseTimer = 0f;
    private float blendTimer = 0f;
    private bool isBlending = false;

    private Quaternion startRotation;
    private Quaternion targetRotation;

    private Color startLightColor;
    private Color targetLightColor;
    private float startLightIntensity;
    private float targetLightIntensity;

    private float currentRotation = 0f;
    public float skyboxRotationSpeed = 0.5f;

    private Cubemap currentCubemap;
    private Cubemap nextCubemap;

    // NEW: References to currently blending volumes
    private Volume currentVolume;
    private Volume nextVolume;

    void Start()
    {
        StartPhase(currentIndex);
    }

    void Update()
    {
        if (!isBlending)
        {
            phaseTimer += Time.deltaTime;
            if (phaseTimer >= phases[currentIndex].duration)
            {
                StartBlendToNext();
            }
        }
        else
        {
            blendTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blendTimer / blendDuration);

            // Blend directional light properties
            directionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
            directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
            directionalLight.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            // Blend skybox
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

        // Skybox rotation every frame
        currentRotation += skyboxRotationSpeed * Time.deltaTime;
        if (blendedSkyboxMaterial != null)
        {
            blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);
        }
    }

    void StartPhase(int index)
    {
        currentIndex = index;
        phaseTimer = 0f;

        var phase = phases[currentIndex];

        // Set skybox material and cubemap
        currentCubemap = phase.skyboxCubemap;
        blendedSkyboxMaterial.SetTexture("_SkyboxA", currentCubemap);
        blendedSkyboxMaterial.SetTexture("_SkyboxB", currentCubemap);
        blendedSkyboxMaterial.SetFloat("_Blend", 0f);
        blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);
        RenderSettings.skybox = blendedSkyboxMaterial;

        // Enable all volumes (so weights can be blended when needed)
        foreach (var p in phases)
        {
            if (p.volume != null)
                p.volume.enabled = true;
        }

        // Set this phase's volume as active
        currentVolume = phase.volume;
        currentVolume.weight = 1f;

        // Apply directional light from prefab
        var light = phase.lighting;
        if (light != null)
        {
            directionalLight.color = light.color;
            directionalLight.intensity = light.intensity;
            directionalLight.transform.rotation = light.transform.rotation;
        }
    }

    void StartBlendToNext()
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

        // Setup directional light interpolation
        var currentLight = phases[currentIndex].lighting;
        var nextLight = nextPhase.lighting;

        startLightColor = currentLight.color;
        targetLightColor = nextLight.color;

        startLightIntensity = currentLight.intensity;
        targetLightIntensity = nextLight.intensity;

        startRotation = currentLight.transform.rotation;
        targetRotation = nextLight.transform.rotation;
    }

    void EndBlend()
    {
        isBlending = false;
        blendTimer = 0f;
        currentIndex = (currentIndex + 1) % phases.Length;

        StartPhase(currentIndex);
    }
}
