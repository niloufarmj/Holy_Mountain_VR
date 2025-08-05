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


    private int eatingAnimalCount = 0;
    public float damageFactor = 0.5f; // شدت تأثیر حیوونا روی رشد (بین ۰ تا ۱)

    [Header("Animal Attraction")]
    public float attractRadius = 25f;


    private float currentGrowthRate;
    private float initialGrowDuration;

    private int activeEaters = 0;
    private float eatDamageInterval = 2f;
    private float damageTimer = 0f;

    private bool isShrinking = false;

    private float growTimer = 0f;
    private float shrinkTimer = 0f;

    private readonly System.Collections.Generic.List<AnimalWander> currentEaters = new();

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
        initialGrowDuration = growDuration;
        currentGrowthRate = 1f; // 1 یعنی با سرعت کامل

        BroadcastToNearbyAnimals();
    }

    void Update()
    {
        if (!isGrowing) return;

        timer += Time.deltaTime * currentGrowthRate;

        // هر eatDamageInterval ثانیه یک بار آسیب بزن
        if (activeEaters > 0)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= eatDamageInterval)
            {
                damageTimer = 0f;

                // کاهش سرعت رشد به نسبت تعداد حیوانات
                currentGrowthRate = Mathf.Max(0f, currentGrowthRate - 0.2f * activeEaters);

                // اگر رسید به صفر، شروع به کوچک شدن
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
                // اگر shrink فعال بود و دیگه دشمنی نبود، دوباره برگرده به رشد
                isShrinking = false;
                growTimer = (1f - Mathf.Clamp01(shrinkTimer / growDuration)) * growDuration;
            }

            // اگر هیچ حیوانی نیست و رشد کم شده بود، دوباره برگرده به سرعت اولیه
            currentGrowthRate = Mathf.MoveTowards(currentGrowthRate, 1f, Time.deltaTime * 0.5f);
        }

        float t;

        if (isShrinking)
        {
            shrinkTimer += Time.deltaTime * (1f + activeEaters * 0.3f); // سرعت تخریب متناسب با تعداد حیوونا
            t = 1f - Mathf.Clamp01(shrinkTimer / growDuration);
        }
        else
        {
            growTimer += Time.deltaTime * currentGrowthRate;
            t = Mathf.Clamp01(growTimer / growDuration);
        }

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

        if (isShrinking && transform.localScale.x <= 0.01f)
        {
            foreach (var animal in currentEaters)
            {
                if (animal != null)
                    animal.ForceStopEating();
            }
            Destroy(gameObject);
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

    public void StartEating(AnimalWander animal)
    {
        if (!currentEaters.Contains(animal))
            currentEaters.Add(animal);

        activeEaters++;
    }

    public void StopEating(AnimalWander animal)
    {
        currentEaters.Remove(animal);
        activeEaters = Mathf.Max(0, activeEaters - 1);
    }
}
