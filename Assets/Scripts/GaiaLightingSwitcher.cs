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
        public Material skyboxMaterial; // Only used to extract Cubemap
        public Cubemap skyboxCubemap; // <--- Add this
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

            // Blend light properties
            directionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
            directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
            directionalLight.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            // Blend skybox
            blendedSkyboxMaterial.SetFloat("_Blend", t);
            blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);

            if (t >= 1f)
            {
                EndBlend();
            }
        }

        // Rotate skybox continuously
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

        // Use the current phase's cubemap as both A and B initially
        currentCubemap = phases[currentIndex].skyboxCubemap;
        blendedSkyboxMaterial.SetTexture("_SkyboxA", currentCubemap);
        blendedSkyboxMaterial.SetTexture("_SkyboxB", currentCubemap);
        blendedSkyboxMaterial.SetFloat("_Blend", 0f);
        blendedSkyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);
        RenderSettings.skybox = blendedSkyboxMaterial;

        // Disable all other volumes
        foreach (var p in phases)
        {
            if (p.volume != null)
                p.volume.enabled = false;
        }

        // Enable current volume
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

        // Extract cubemap from material
        currentCubemap = phases[currentIndex].skyboxCubemap;
        nextCubemap = nextPhase.skyboxCubemap;

        blendedSkyboxMaterial.SetTexture("_SkyboxA", currentCubemap);
        blendedSkyboxMaterial.SetTexture("_SkyboxB", nextCubemap);
        blendedSkyboxMaterial.SetFloat("_Blend", 0f);
        RenderSettings.skybox = blendedSkyboxMaterial;

        // Disable all other volumes
        foreach (var p in phases)
        {
            if (p.volume != null)
                p.volume.enabled = false;
        }

        if (nextPhase.volume != null)
            nextPhase.volume.enabled = true;

        // Setup light blending
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

    // Extracts Cubemap from skybox material
    Cubemap ExtractCubemap(Material mat)
    {
        if (mat.HasProperty("_Tex") && mat.GetTexture("_Tex") is Cubemap cube)
        {
            return cube;
        }
        else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") is Cubemap mainCube)
        {
            return mainCube;
        }
        else if (mat.HasProperty("_TexCube") && mat.GetTexture("_TexCube") is Cubemap texCube)
        {
            return texCube;
        }

        Debug.LogWarning("No cubemap found on skybox material: " + mat.name);
        return null;
    }
}
