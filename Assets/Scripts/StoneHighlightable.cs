using UnityEngine;

/// <summary>
/// Makes a stone object highlightable by scaling a shader property.
/// - Detects shader property dynamically (supports display name and fallback reference).
/// - Applies a pulsing highlight effect when active.
/// - Disables highlight when the stone is being held by the player.
/// - Uses <see cref="MaterialPropertyBlock"/> to avoid creating new material instances.
/// </summary>
[DisallowMultipleComponent]
public class StoneHighlightable : MonoBehaviour
{
    [Header("Shader Property (Display vs Reference)")]
    [Tooltip("Display name in the material (commonly 'Scale').")]
    public string propertyName = "Scale";

    [Tooltip("Fallback property name if the shader reference is '_Scale'.")]
    public string fallbackPropertyName = "_Scale";

    [Header("Pulse Settings")]
    [Tooltip("Scale when not highlighted (off).")]
    public float baseScale = 1f;

    [Tooltip("Maximum scale when fully highlighted (on).")]
    public float highlightMax = 1.2f;

    [Tooltip("Speed of pulsing effect while highlighted.")]
    public float pulseSpeed = 3f;

    // Cached references
    private Renderer[] renderers;
    private int propID = -1;
    private bool isHighlighted;
    private float tOffset;
    private MaterialPropertyBlock block;
    private ThrowableStone throwable;

    private void Awake()
    {
        // Collect all renderers in children (ensures nested meshes are included)
        renderers = GetComponentsInChildren<Renderer>(true);

        block = new MaterialPropertyBlock();
        tOffset = Random.value * 100f; // Random phase offset for pulsing effect
        throwable = GetComponent<ThrowableStone>();

        // Resolve shader property: try display name first, fallback otherwise
        int id1 = Shader.PropertyToID(propertyName);
        int id2 = Shader.PropertyToID(fallbackPropertyName);

        if (AnyRendererHasProperty(id1))      propID = id1;
        else if (AnyRendererHasProperty(id2)) propID = id2;
        else
        {
            Debug.LogWarning(
                $"[StoneHighlightable] Neither '{propertyName}' nor '{fallbackPropertyName}' found on materials of {name}. " +
                "Open your Shader Graph and copy the exact 'Reference' name into this script."
            );
            // Assign default to avoid crashes
            propID = id1;
        }

        // Initialize to base scale
        Apply(baseScale);
    }

    private void Update()
    {
        // If being held, ensure highlight is off
        if (throwable != null && throwable.IsHeld)
        {
            if (isHighlighted)
            {
                isHighlighted = false;
                Apply(baseScale);
            }
            return;
        }

        // If not highlighted, apply base scale only
        if (!isHighlighted)
        {
            Apply(baseScale);
            return;
        }

        // Pulsing effect while highlighted
        float s = 0.5f + 0.5f * Mathf.Sin((Time.time + tOffset) * pulseSpeed);
        float value = Mathf.Lerp(baseScale, highlightMax, s);
        Apply(value);
    }

    /// <summary>
    /// Applies a float value to the shader property for all renderers in this object.
    /// </summary>
    private void Apply(float value)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            if (block == null) block = new MaterialPropertyBlock(); // Safety fallback
            r.GetPropertyBlock(block);
            block.SetFloat(propID, value);
            r.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// Enables or disables highlighting.
    /// </summary>
    public void SetHighlighted(bool on)
    {
        if (isHighlighted == on) return;
        isHighlighted = on;
        if (!on) Apply(baseScale);
    }

    /// <summary>
    /// Utility to check if any renderer in this object supports the given property.
    /// </summary>
    private bool AnyRendererHasProperty(int id)
    {
        foreach (var r in renderers)
        {
            if (!r) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m) continue;
                if (m.HasProperty(id)) return true;
            }
        }
        return false;
    }
}
