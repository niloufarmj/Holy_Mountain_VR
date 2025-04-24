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

        // 3. پارتیکل رو غیرفعال یا destroy کن
        if (aura != null)
            Destroy(aura.gameObject);

        // 4. حالا انیمیشن افتادن درخت رو پخش کن
        Animator anim = treeGO.GetComponent<Animator>();
        if (anim != null)
            anim.Play("TreeFall");
    }

}
