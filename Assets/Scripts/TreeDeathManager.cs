using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeDeathManager : MonoBehaviour
{
    public Terrain terrain;
    public float interval = 10f;

    private List<TreeInstance> treeList;
    [HideInInspector] public TerrainData clonedData;

    public GameObject seedPrefab;

    void Start()
    {
        // Clone terrain data to avoid modifying original in Editor
        clonedData = Instantiate(terrain.terrainData);
        terrain.terrainData = clonedData;

        treeList = new List<TreeInstance>(clonedData.treeInstances);
        treeList = treeList.FindAll(tree => tree.prototypeIndex >= 0 && tree.prototypeIndex <= 3);
        InvokeRepeating("KillRandomTree", interval, interval);


        Debug.Log(treeList.Count);
    }

    void KillRandomTree()
    {
        if (treeList.Count == 0)
        {
            CancelInvoke("KillRandomTree");
            Debug.Log("✅ all trees were removed");
            return;
        }

        int index = Random.Range(0, treeList.Count);
        TreeInstance dyingTree = treeList[index];

        // گرفتن نوع درخت
        int prototypeIndex = dyingTree.prototypeIndex;
        TreeTypeData treeType = TreeDatabase.Instance.GetTreeType(prototypeIndex);
        if (treeType == null || treeType.animatedTreePrefab == null)
        {
            Debug.LogWarning($"[TreeDeathManager] No animated prefab found for prototype index {prototypeIndex}");
            return;
        }

        // تبدیل مختصات نسبی به مختصات جهانی
        Vector3 worldPos = Vector3.Scale(dyingTree.position, terrain.terrainData.size) + terrain.transform.position;

        // محاسبه چرخش Y
        float yRotation = dyingTree.rotation * 360f;
        Quaternion rotation = Quaternion.Euler(0, yRotation + 180, 0);

        // Instantiate animated prefab مربوط به نوع درخت
        GameObject newTree = Instantiate(treeType.animatedTreePrefab, worldPos, rotation);
        StartCoroutine(AnimateFallingTree(newTree, prototypeIndex));

        // حذف درخت از Terrain
        treeList.RemoveAt(index);
        clonedData.treeInstances = treeList.ToArray();

        Debug.Log("A tree has died. Remaining: " + treeList.Count);
    }


    IEnumerator AnimateFallingTree(GameObject treeGO, int prototypeIndex)
    {
        // 1. پیدا کردن پارتیکل
        Transform aura = treeGO.transform.Find("CorruptionAura");
        if (aura != null)
            aura.gameObject.SetActive(true);

        // 2. 2 ثانیه صبر کن تا هاله دیده شه
        yield return new WaitForSeconds(2f);

        // 3. پارتیکل رو حذف کن
        if (aura != null)
            Destroy(aura.gameObject);

        // 4. پخش انیمیشن افتادن درخت
        Animator anim = treeGO.GetComponent<Animator>();
        if (anim != null)
            anim.Play("TreeFall");

        // 5. صبر کن تا انیمیشن تموم شه (فرض: 2.5 ثانیه)
        yield return new WaitForSeconds(2.5f);

        LODGroup lodGroup = treeGO.GetComponentInChildren<LODGroup>();
        if (lodGroup != null)
        {
            lodGroup.ForceLOD(3); // یا هر LODی که می‌خوای Dissolve بشه
        }

        // 6. شروع Dissolve روی LOD3
        // دسترسی به LOD3 بدون اسم
        Transform lod3 = null;

        if (treeGO.transform.childCount >= 1)
        {
            Transform modelRoot = treeGO.transform.GetChild(0); // فرزند دوم (مدل درخت)
            int lastIndex = modelRoot.childCount - 1;

            if (lastIndex >= 0)
                lod3 = modelRoot.GetChild(lastIndex); // آخرین بچه = LOD3
        }

        if (lod3 != null)
        {
            Renderer renderer = lod3.GetComponent<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(DissolveTreeViaAlphaCutoff(treeGO, renderer, prototypeIndex));
                yield break; // ادامه‌ی حذف داخل اون Coroutine انجام میشه
            }
        }

        // اگر چیزی پیدا نشد، درختو حذف کن
        Destroy(treeGO);
    }

    IEnumerator DissolveTreeViaAlphaCutoff(GameObject treeGO, Renderer renderer, int prototypeIndex, float duration = 2f)
    {
        float start = 0.3f;
        float end = 1f;
        float t = 0f;

        Material[] materials = renderer.materials;

        while (t < duration)
        {
            float value = Mathf.Lerp(start, end, t / duration);
            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Cutoff"))
                    mat.SetFloat("_Cutoff", value);
            }

            t += Time.deltaTime;
            yield return null;
        }

        // مطمئن شو Dissolve کامل شده
        foreach (var mat in materials)
        {
            if (mat.HasProperty("_Cutoff"))
                mat.SetFloat("_Cutoff", end);
        }

        // Instantiate Seed در محل درخت
        if (seedPrefab != null)
        {
            Vector3 rayOrigin = renderer.bounds.center + Vector3.up * 5f; // از بالای درخت نگاه کن
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, 10f))
            {
                Vector3 spawnPos = hitInfo.point + Vector3.up * 0.1f; // کمی بالاتر از زمین
                GameObject seed = Instantiate(seedPrefab, spawnPos, Quaternion.identity);

                // 👇 ست کردن prototypeIndex
                SeedCollectible seedScript = seed.GetComponentInChildren<SeedCollectible>();
                if (seedScript != null)
                {
                    seedScript.prototypeIndex = prototypeIndex; // از TreeInstance مرده گرفته میشه
                }
            }
            else
            {
                Debug.LogWarning("🌱 [TreeDeathManager] Raycast برای یافتن زمین شکست خورد.");
            }
        }

       
        Destroy(treeGO);
    }

}
