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
    }

    void Update()
    {
        if (!isGrowing) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / growDuration);
        float currentScale = Mathf.Lerp(0f, 1f, t);
        transform.localScale = Vector3.one * currentScale;

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
}
