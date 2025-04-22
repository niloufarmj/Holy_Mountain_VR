using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaiaLightingSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class LightingPhase
    {
        public string name;
        public GameObject lightingRoot;
        public Volume volume;
        public Material skyboxMaterial;
        public float duration = 60f;
    }

    public LightingPhase[] phases;
    public float blendDuration = 5f;
    public Light directionalLight;

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

    private Material skyboxInstance; // Unique instance we rotate

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

            // Light interpolation
            directionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
            directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
            directionalLight.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (t >= 1f)
            {
                EndBlend();
            }
        }

        // Skybox rotation every frame
        currentRotation += skyboxRotationSpeed * Time.deltaTime;

        if (skyboxInstance != null && skyboxInstance.HasProperty("_Rotation"))
        {
            skyboxInstance.SetFloat("_Rotation", currentRotation % 360f);
        }
    }
    

    void StartPhase(int index)
    {
        currentIndex = index;
        phaseTimer = 0f;

        var phase = phases[currentIndex];

        // Clone the skybox material so we can rotate it
        skyboxInstance = new Material(phase.skyboxMaterial);
        RenderSettings.skybox = skyboxInstance;
        currentRotation = 0f;

        // Disable all other volumes
        foreach (var p in phases)
        {
            if (p.volume != null)
                p.volume.enabled = false;
        }

        // Enable only current phase's volume
        if (phase.volume != null)
            phase.volume.enabled = true;

        // Apply directional light from prefab
        var light = phase.lightingRoot.GetComponentInChildren<Light>();
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

        // Switch skybox immediately
        RenderSettings.skybox = nextPhase.skyboxMaterial;

        // Switch post-processing volume immediately
        foreach (var p in phases)
        {
            if (p.volume != null)
                p.volume.enabled = false;
        }

        if (nextPhase.volume != null)
            nextPhase.volume.enabled = true;

        // Setup light interpolation
        var currentLight = phases[currentIndex].lightingRoot.GetComponentInChildren<Light>();
        var nextLight = nextPhase.lightingRoot.GetComponentInChildren<Light>();

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
