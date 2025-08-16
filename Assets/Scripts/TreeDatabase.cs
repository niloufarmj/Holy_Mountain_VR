using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton database holding definitions of all available tree types.
/// Provides lookup by prototype index to retrieve prefab and material references.
/// </summary>
public class TreeDatabase : MonoBehaviour
{
    [Tooltip("List of tree type data entries (prototype index + prefabs/materials).")]
    public List<TreeTypeData> treeTypes;

    /// <summary>
    /// Global singleton instance of the TreeDatabase.
    /// </summary>
    public static TreeDatabase Instance;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Finds and returns the tree type definition for the given prototype index.
    /// </summary>
    /// <param name="prototypeIndex">Tree prototype index to look up.</param>
    /// <returns><see cref="TreeTypeData"/> if found, otherwise null.</returns>
    public TreeTypeData GetTreeType(int prototypeIndex)
    {
        return treeTypes.Find(t => t.prototypeIndex == prototypeIndex);
    }
}

/// <summary>
/// Data structure describing one type of tree, including prefab and materials.
/// </summary>
[System.Serializable]
public class TreeTypeData
{
    [Tooltip("Tree prototype index (unique identifier).")]
    public int prototypeIndex;

    [Tooltip("Prefab of the fully animated tree (e.g., for wind sway, idle states).")]
    public GameObject animatedTreePrefab;

    [Tooltip("Prefab of the growable version (used during planting and growth stages).")]
    public GameObject growableTreePrefab;

    [Header("Materials")]
    [Tooltip("Bark material applied during growth/animation.")]
    public Material barkMat;

    [Tooltip("Ending material applied at final growth stage.")]
    public Material endingMat;

    [Tooltip("Needles/leaves material applied to this tree type.")]
    public Material needlesMat;
}
