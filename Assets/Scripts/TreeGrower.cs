using UnityEngine;
using System;

/// <summary>
/// Handles growth and destruction of a planted tree:
/// - Grows through multiple phases (Seedling, Sapling, FullGrown).
/// - Materials and visuals update as growth progresses.
/// - Nearby animals can be attracted to eat the tree, slowing/stopping growth.
/// - If animals overwhelm growth, the tree shrinks and may be destroyed.
/// - Supports broadcasting to animals, damage over time, and event on full growth.
/// </summary>
public class TreeGrower : MonoBehaviour
{
    [Header("Growth Settings")]
    [Tooltip("Time (seconds) for the tree to fully grow under normal conditions.")]
    public float growDuration = 10f;

    [Header("Materials")]
    public Material barkMat;
    public Material endingMat;
    public Material needlesMat;

    // Growth state
    private bool isGrowing = false;
    private float timer = 0f;
    private Renderer rend;
    private Material[] currentMats;

    /// <summary>Event invoked when the tree finishes full growth.</summary>
    public event Action<TreeGrower> OnGrowthComplete;

    /// <summary>Growth phases representing stages of scale.</summary>
    public enum GrowthPhase { Seedling, Sapling, FullGrown }
    public GrowthPhase currentPhase = GrowthPhase.Seedling;
    private GrowthPhase lastPhase = GrowthPhase.Seedling;

    [Header("Animal Interaction")]
    [Tooltip("Growth reduction factor applied by animals eating (0–1 scale).")]
    public float damageFactor = 0.5f;

    [Tooltip("Radius within which animals will be attracted during growth.")]
    public float attractRadius = 25f;

    private int activeEaters = 0;
    private readonly System.Collections.Generic.List<AnimalWander> currentEaters = new();

    // Growth timers
    private float currentGrowthRate;
    private float initialGrowDuration;
    private float eatDamageInterval = 2f;
    private float damageTimer = 0f;

    // Shrinking state
    private bool isShrinking = false;
    private float growTimer = 0f;
    private float shrinkTimer = 0f;

    [Header("FX")]
    public Transform auraAttach;         // اگر خالی بود، خودِ transform
    GameObject eatingAuraInstance;
    [SerializeField] float auraScaleMultiplier = 1.05f; // 5% larger than the tree

    // --- Add near the top ---
    [Header("Audio (Growth Loop)")]
    [SerializeField] private AudioClip growLoopSfx;
    [SerializeField, Range(0f,1f)] private float growLoopVolume = 0.65f;
    [SerializeField] private float growMinDistance = 2f;
    [SerializeField] private float growMaxDistance = 28f;

    private AudioSource _growSrc;

    void Start()
    {
        rend = GetComponent<Renderer>();
        currentMats = new Material[1] { barkMat };
        rend.materials = currentMats;
        transform.localScale = Vector3.zero;
        if (!auraAttach) auraAttach = transform;

        if (growLoopSfx != null)
        {
            _growSrc = gameObject.AddComponent<AudioSource>();
            _growSrc.playOnAwake = false;
            _growSrc.loop = true;                         // replay automatically if shorter than growth
            _growSrc.clip = growLoopSfx;
            _growSrc.volume = growLoopVolume;
            _growSrc.spatialBlend = 1f;                   // fully 3D for VR
            _growSrc.rolloffMode = AudioRolloffMode.Logarithmic;
            _growSrc.minDistance = growMinDistance;
            _growSrc.maxDistance = growMaxDistance;
            _growSrc.spatialize = true;                   // if your XR audio plugin supports it
        }
    }

    /// <summary>
    /// Begins growth process from seed state and attracts nearby animals.
    /// </summary>
    public void StartGrowth()
    {
        isGrowing = true;
        initialGrowDuration = growDuration;
        currentGrowthRate = 1f;

        if (_growSrc && !_growSrc.isPlaying) _growSrc.Play();

        BroadcastToNearbyAnimals();
    }

    private void Update()
    {
        if (!isGrowing) return;

        timer += Time.deltaTime * currentGrowthRate;

        // Apply periodic growth damage from active animals
        if (activeEaters > 0)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= eatDamageInterval)
            {
                damageTimer = 0f;
                currentGrowthRate = Mathf.Max(0f, currentGrowthRate - 0.2f * activeEaters);

                // If growth halts, begin shrinking
                if (currentGrowthRate <= 0f && !isShrinking)
                {
                    isShrinking = true;
                    shrinkTimer = (1f - Mathf.Clamp01(growTimer / growDuration)) * growDuration;
                }
            }
        }
        else
        {
            damageTimer = 0f;

            if (isShrinking)
            {
                // If shrinking but no longer attacked, revert to growth
                isShrinking = false;
                growTimer = (1f - Mathf.Clamp01(shrinkTimer / growDuration)) * growDuration;
            }

            // Recover growth rate gradually if reduced
            currentGrowthRate = Mathf.MoveTowards(currentGrowthRate, 1f, Time.deltaTime * 0.5f);
        }

        // Calculate normalized growth/shrink factor
        float t;
        if (isShrinking)
        {
            shrinkTimer += Time.deltaTime * (1f + activeEaters * 0.3f);
            t = 1f - Mathf.Clamp01(shrinkTimer / growDuration);
        }
        else
        {
            growTimer += Time.deltaTime * currentGrowthRate;
            t = Mathf.Clamp01(growTimer / growDuration);
        }

        // Scale tree accordingly
        float currentScale = Mathf.Lerp(0f, 1f, t);
        transform.localScale = Vector3.one * currentScale;

        // Determine growth phase
        GrowthPhase newPhase = currentPhase;
        if (currentScale >= 1f)
        {
            currentPhase = GrowthPhase.FullGrown;
            if (_growSrc && _growSrc.isPlaying) _growSrc.Stop();  // stop at end of growth
            OnGrowthComplete?.Invoke(this);
            isGrowing = false;
        }
        else if (currentScale >= 0.6f && currentPhase != GrowthPhase.Sapling)
        {
            currentPhase = GrowthPhase.Sapling;
        }
        else if (currentScale >= 0.2f && currentPhase != GrowthPhase.Seedling)
        {
            currentPhase = GrowthPhase.Seedling;
        }

        // Handle phase transitions
        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            if (currentPhase == GrowthPhase.Seedling || currentPhase == GrowthPhase.Sapling)
            {
                BroadcastToNearbyAnimals();
            }
            lastPhase = currentPhase;
        }

        // Progressively add materials as tree grows
        if (currentScale >= 0.35f && currentMats.Length == 1)
        {
            currentMats = new Material[2] { barkMat, endingMat };
            rend.materials = currentMats;
        }

        if (currentScale >= 0.4f && currentMats.Length == 2)
        {
            currentMats = new Material[3] { barkMat, endingMat, new Material(needlesMat) };
            rend.materials = currentMats;
        }

        if (currentScale >= 0.4f && currentMats.Length == 3)
        {
            float alphaValue = Mathf.Lerp(1f, 0.4f, Mathf.InverseLerp(0.5f, 1f, currentScale));
            currentMats[2].SetFloat("_Cutoff", alphaValue);
        }

        // Finalize growth
        if (currentScale >= 1f)
        {
            if (_growSrc && _growSrc.isPlaying) _growSrc.Stop();
            OnGrowthComplete?.Invoke(this);
            isGrowing = false;
        }

        // Destroy if fully shrunk
        if (isShrinking && transform.localScale.x <= 0.01f)
        {
            if (_growSrc && _growSrc.isPlaying) _growSrc.Stop();
            foreach (var animal in currentEaters)
            {
                if (animal != null)
                    animal.ForceStopEating();
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Notifies nearby animals within <see cref="attractRadius"/> to target this tree.
    /// </summary>
    private void BroadcastToNearbyAnimals()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attractRadius);
        foreach (var hit in hitColliders)
        {
            AnimalWander wanderer = hit.GetComponentInParent<AnimalWander>();
            if (wanderer != null)
            {
                wanderer.SetTargetTree(transform);
            }
        }
    }

    /// <summary>
    /// Called when an animal starts eating this tree.
    /// </summary>
    public void StartEating(AnimalWander animal)
    {
        if (!currentEaters.Contains(animal))
            currentEaters.Add(animal);
        activeEaters++;

        // Stop growth SFX while animals are eating
        if (_growSrc && _growSrc.isPlaying) _growSrc.Pause();

        if (eatingAuraInstance == null)
        {
            // try to find a child named EatingAura_Red
            var child = transform.Find("EatingAura_Red");
            if (child) eatingAuraInstance = child.gameObject;
        }

        if (eatingAuraInstance)
        {
            // ensure it’s parented and aligned to the tree
            var t = eatingAuraInstance.transform;
            if (t.parent != auraAttach) t.SetParent(auraAttach ? auraAttach : transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            // --- SCALE ---
            // We want the aura's *world* scale to match this tree's world scale (× multiplier).
            // If the aura is a direct child of this object, localScale = Vector3.one * multiplier is enough.
            if (t.parent == transform || t.parent == (auraAttach ? auraAttach : transform))
            {
                t.localScale = Vector3.one * auraScaleMultiplier;
            }
            else
            {
                // robust case: compute local scale that yields desired world scale
                Vector3 desiredWorld = transform.lossyScale * auraScaleMultiplier;
                Vector3 parentWorld  = t.parent ? t.parent.lossyScale : Vector3.one;
                t.localScale = new Vector3(
                    desiredWorld.x / Mathf.Max(parentWorld.x, 1e-6f),
                    desiredWorld.y / Mathf.Max(parentWorld.y, 1e-6f),
                    desiredWorld.z / Mathf.Max(parentWorld.z, 1e-6f)
                );
            }

            eatingAuraInstance.SetActive(true);
        }
    }

    /// <summary>
    /// Called when an animal stops eating this tree.
    /// </summary>
    public void StopEating(AnimalWander animal)
    {
        currentEaters.Remove(animal);
        activeEaters = Mathf.Max(0, activeEaters - 1);

        if (activeEaters == 0 && eatingAuraInstance != null)
        {
            eatingAuraInstance.SetActive(false);
            // اگر Prefab Instance ساختی و نمی‌خواهی نگه داری:
            // Destroy(eatingAuraInstance); eatingAuraInstance = null;

            // Animals gone. If still growing, resume the loop.
            if (isGrowing && _growSrc && !_growSrc.isPlaying)
            {
                // If it was paused, UnPause continues smoothly; if it was stopped, Play restarts.
                _growSrc.UnPause();
                if (!_growSrc.isPlaying) _growSrc.Play();
            }
        }
    }
}
