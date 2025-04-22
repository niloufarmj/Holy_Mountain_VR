using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaiaLightingSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class LightingPhase
    {
        public string name;
        public GameObject lightingRoot; // Includes Light and Volume
        public Volume postProcessingVolume;
        public Material skyboxMaterial;
        public float duration; // seconds
    }

    public LightingPhase[] phases;
    public float blendDuration = 2f;

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isBlending = false;

    void Start()
    {
        ActivatePhase(0, true);
    }

    void Update()
    {
        if (isBlending) return;

        timer += Time.deltaTime;

        if (timer >= phases[currentIndex].duration)
        {
            int nextIndex = (currentIndex + 1) % phases.Length;
            StartCoroutine(BlendToPhase(nextIndex));
            timer = 0f;
        }
    }

    IEnumerator BlendToPhase(int nextIndex)
    {
        isBlending = true;
        
        var current = phases[currentIndex];
        var next = phases[nextIndex];

        // Create material instances for blending
        Material currentSkyboxInstance = new Material(current.skyboxMaterial);
        Material nextSkyboxInstance = new Material(next.skyboxMaterial);
        
        // Activate the next phase's lighting
        next.lightingRoot.SetActive(true);

        float t = 0f;
        while (t < blendDuration)
        {
            t += Time.deltaTime;
            float blend = Mathf.Clamp01(t / blendDuration);

            // Create a temporary blended material
            Material blendedSkybox = new Material(currentSkyboxInstance);
            
            // Blend all common properties
            BlendSkyboxMaterials(blendedSkybox, currentSkyboxInstance, nextSkyboxInstance, blend);
            
            // Apply the blended skybox
            RenderSettings.skybox = blendedSkybox;

            // Blend post-processing volumes
            if (current.postProcessingVolume != null)
                current.postProcessingVolume.weight = 1f - blend;
            if (next.postProcessingVolume != null)
                next.postProcessingVolume.weight = blend;

            yield return null;
        }

        // Cleanup
        foreach (var p in phases)
        {
            if (p != next)
            {
                if (p.lightingRoot != null) p.lightingRoot.SetActive(false);
                if (p.postProcessingVolume != null) p.postProcessingVolume.weight = 0f;
            }
        }

        // Set final skybox
        RenderSettings.skybox = new Material(next.skyboxMaterial);
        
        currentIndex = nextIndex;
        isBlending = false;
        DynamicGI.UpdateEnvironment();
    }

    void BlendSkyboxMaterials(Material target, Material a, Material b, float blend)
    {
        // Blend all float properties
        var floatProperties = a.GetFloatPropertyNames();
        foreach (var prop in floatProperties)
        {
            if (b.HasProperty(prop))
            {
                float valA = a.GetFloat(prop);
                float valB = b.GetFloat(prop);
                target.SetFloat(prop, Mathf.Lerp(valA, valB, blend));
            }
        }

        // Blend all color properties
        var colorProperties = a.GetColorPropertyNames();
        foreach (var prop in colorProperties)
        {
            if (b.HasProperty(prop))
            {
                Color colA = a.GetColor(prop);
                Color colB = b.GetColor(prop);
                target.SetColor(prop, Color.Lerp(colA, colB, blend));
            }
        }

        // Blend all texture properties (if they're the same texture)
        var textureProperties = a.GetTexturePropertyNames();
        foreach (var prop in textureProperties)
        {
            if (b.HasProperty(prop) && a.GetTexture(prop) == b.GetTexture(prop))
            {
                target.SetTexture(prop, a.GetTexture(prop));
            }
        }
    }

    void ActivatePhase(int index, bool immediate)
    {
        foreach (var p in phases)
        {
            p.lightingRoot.SetActive(false);
            if (p.postProcessingVolume != null)
                p.postProcessingVolume.weight = 0f;
        }

        var phase = phases[index];
        phase.lightingRoot.SetActive(true);
        if (phase.postProcessingVolume != null)
            phase.postProcessingVolume.weight = 1f;

        RenderSettings.skybox = new Material(phase.skyboxMaterial);
        currentIndex = index;

        if (immediate) DynamicGI.UpdateEnvironment();
    }
}