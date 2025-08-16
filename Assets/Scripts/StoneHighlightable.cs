using UnityEngine;

[DisallowMultipleComponent]
public class StoneHighlightable : MonoBehaviour
{
    [Header("Shader Property (Display vs Reference)")]
    [Tooltip("Display name in the material, often 'Scale'")]
    public string propertyName = "Scale";     // مثلا نمایش در متریال
    [Tooltip("Fallback if your ShaderGraph Reference is '_Scale'")]
    public string fallbackPropertyName = "_Scale";

    [Header("Pulse")]
    public float baseScale = 1f;              // خاموش
    public float highlightMax = 1.2f;         // روشن
    public float pulseSpeed = 3f;

    Renderer[] renderers;
    int propID = -1;
    bool isHighlighted;
    float tOffset;
    MaterialPropertyBlock block;
    ThrowableStone throwable;

    void Awake()
    {
        // همیشه خودم پیدا می‌کنم تا اشتباه دستی پیش نیاد
        renderers = GetComponentsInChildren<Renderer>(true);

        block = new MaterialPropertyBlock();
        tOffset = Random.value * 100f;
        throwable = GetComponent<ThrowableStone>();

        // انتخاب هوشمند اسم پراپرتی
        int id1 = Shader.PropertyToID(propertyName);
        int id2 = Shader.PropertyToID(fallbackPropertyName);

        if (AnyRendererHasProperty(id1))      propID = id1;
        else if (AnyRendererHasProperty(id2)) propID = id2;
        else
        {
            Debug.LogWarning(
                $"[StoneHighlightable] Neither '{propertyName}' nor '{fallbackPropertyName}' found on materials of {name}. " +
                "Open your Shader Graph and copy the exact 'Reference' name into this script.");
            // یک شناسه پیش‌فرض می‌ذاریم که کرش نکنه
            propID = id1;
        }

        Apply(baseScale);
    }

    void Update()
    {
        // وقتی تو دست بازیکنه، هایلایت خاموش
        if (throwable != null && throwable.IsHeld)
        {
            if (isHighlighted) { isHighlighted = false; Apply(baseScale); }
            return;
        }

        if (!isHighlighted) { Apply(baseScale); return; }

        float s = 0.5f + 0.5f * Mathf.Sin((Time.time + tOffset) * pulseSpeed);
        float value = Mathf.Lerp(baseScale, highlightMax, s);
        Apply(value);
    }

    void Apply(float value)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            if (block == null) block = new MaterialPropertyBlock(); // احتیاط
            r.GetPropertyBlock(block);
            block.SetFloat(propID, value);
            r.SetPropertyBlock(block);
        }
    }

    public void SetHighlighted(bool on)
    {
        if (isHighlighted == on) return;
        isHighlighted = on;
        if (!on) Apply(baseScale);
    }

    bool AnyRendererHasProperty(int id)
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
