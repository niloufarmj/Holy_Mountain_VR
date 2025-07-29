using UnityEngine;
using System;

public class TreeGrower : MonoBehaviour
{
    public float growDuration = 10f;
    public Material barkMat, endingMat, needlesMat;

    private bool isGrowing = false;
    private float timer = 0f;
    private Renderer rend;
    private Material[] currentMats;

    public event Action<TreeGrower> OnGrowthComplete;

    public enum GrowthPhase { Seedling, Sapling, FullGrown }
    public GrowthPhase currentPhase = GrowthPhase.Seedling;

    private GrowthPhase lastPhase = GrowthPhase.Seedling;

    [Header("Animal Attraction")]
    public float attractRadius = 40f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        currentMats = new Material[1] { barkMat };
        rend.materials = currentMats;
        transform.localScale = Vector3.zero;
    }

    public void StartGrowth()
    {
        isGrowing = true;

        BroadcastToNearbyAnimals();
    }

    void Update()
    {
        if (!isGrowing) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / growDuration);
        float currentScale = Mathf.Lerp(0f, 1f, t);
        transform.localScale = Vector3.one * currentScale;

        GrowthPhase newPhase = currentPhase;
        if (currentScale >= 1f)
        {
            currentPhase = GrowthPhase.FullGrown;
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

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;

            // فقط اگر فاز جذاب هست (Seedling یا Sapling)
            if (currentPhase == GrowthPhase.Seedling || currentPhase == GrowthPhase.Sapling)
            {
                BroadcastToNearbyAnimals();
            }

            lastPhase = currentPhase;
        }

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

        if (currentScale >= 1f)
        {
            OnGrowthComplete?.Invoke(this); // صدا زدن ایونت پایان رشد
            isGrowing = false; // فقط یک بار
        }
    }


    void BroadcastToNearbyAnimals()
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
}
