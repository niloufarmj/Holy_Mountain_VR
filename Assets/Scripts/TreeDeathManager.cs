// TreeDeathManager.cs
using UnityEngine;
using System.Collections.Generic;

public class TreeDeathManager : MonoBehaviour
{
    public Terrain terrain;
    public float interval = 10f;

    [HideInInspector] public TerrainData clonedData;

    private List<TreeInstance> treeList;

    void Start()
    {
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
            return;
        }

        int index = Random.Range(0, treeList.Count);
        treeList.RemoveAt(index);

        clonedData.treeInstances = treeList.ToArray();

        Debug.Log("A tree has died. Remaining: " + treeList.Count);
    }
}
