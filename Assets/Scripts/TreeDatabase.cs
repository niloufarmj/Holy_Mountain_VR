using UnityEngine;
using System.Collections.Generic;

public class TreeDatabase : MonoBehaviour
{
    public List<TreeTypeData> treeTypes;

    public static TreeDatabase Instance;

    void Awake()
    {
        Instance = this;
    }

    public TreeTypeData GetTreeType(int prototypeIndex)
    {
        return treeTypes.Find(t => t.prototypeIndex == prototypeIndex);
    }
}

[System.Serializable]
public class TreeTypeData
{
    public int prototypeIndex;
    public GameObject animatedTreePrefab;
    public GameObject growableTreePrefab;
    public Material barkMat;
    public Material endingMat;
    public Material needlesMat;
}
