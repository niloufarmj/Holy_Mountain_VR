using UnityEngine;
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

            Animator anim = newTree.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("TreeFall");
            }
        }

        // حذف درخت از Terrain
        treeList.RemoveAt(index);
        clonedData.treeInstances = treeList.ToArray();


        Debug.Log("A tree has died. Remaining: " + treeList.Count);

    }
}
