using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeDeathManager : MonoBehaviour
{
    public Terrain terrain;
    public float interval = 10f;
    public GameObject animatedTreePrefab; // همون Prefab که انیمیشن "TreeFall" روشه

    private List<TreeInstance> treeList;
    [HideInInspector] public TerrainData clonedData;

    public GameObject seedPrefab;

    void Start()
    {
        // Clone terrain data to avoid modifying original in Editor
        clonedData = Instantiate(terrain.terrainData);
        terrain.terrainData = clonedData;

        treeList = new List<TreeInstance>(clonedData.treeInstances);
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

        // تبدیل مختصات نسبی به مختصات جهانی
        Vector3 worldPos = Vector3.Scale(dyingTree.position, terrain.terrainData.size) + terrain.transform.position;

        // محاسبه چرخش Y
        float yRotation = dyingTree.rotation * 360f;
        Quaternion rotation = Quaternion.Euler(0, yRotation + 180, 0);

        // Instantiate Prefab با انیمیشن
        if (animatedTreePrefab != null)
        {
            GameObject newTree = Instantiate(animatedTreePrefab, worldPos, rotation);
            StartCoroutine(AnimateFallingTree(newTree));
        }

        // حذف درخت از Terrain
        treeList.RemoveAt(index);
        clonedData.treeInstances = treeList.ToArray();


        Debug.Log("A tree has died. Remaining: " + treeList.Count);

    }

    IEnumerator AnimateFallingTree(GameObject treeGO)
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
        Transform lod3 = treeGO.transform.Find("PW_Tree_Spruce_04 Variant/PW_Tree_Spruce_01_04_LOD3");
        if (lod3 != null)
        {
            Renderer renderer = lod3.GetComponent<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(DissolveTreeViaAlphaCutoff(treeGO, renderer));
                yield break; // ادامه‌ی حذف داخل اون Coroutine انجام میشه
            }
        }

        // اگر چیزی پیدا نشد، درختو حذف کن
        Destroy(treeGO);
    }

    IEnumerator DissolveTreeViaAlphaCutoff(GameObject treeGO, Renderer renderer, float duration = 2f)
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
            Vector3 seedPos = renderer.bounds.center;
            seedPos = seedPos - Vector3.up * 0.5f;
            Instantiate(seedPrefab, seedPos, Quaternion.identity);
        }

       
        Destroy(treeGO);
    }

}
