using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages periodic tree deaths near the VR player:
/// - Clones terrain data at runtime to avoid modifying the editor terrain.
/// - At intervals, finds a random nearby tree and replaces it with an animated prefab.
/// - Plays a corruption aura, falling animation, and dissolve effect before destroying.
/// - Optionally spawns a seed collectible at the fallen tree’s location.
/// </summary>
public class TreeDeathManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Terrain that contains trees to manage.")]
    public Terrain terrain;

    [Tooltip("Prefab of the seed collectible to spawn when a tree dies.")]
    public GameObject seedPrefab;

    [Tooltip("Reference to the VR player used to determine proximity.")]
    public Transform vrPlayer;

    [Header("Timing")]
    [Tooltip("Interval (seconds) between tree death events.")]
    public float interval = 10f;

    /// <summary>Runtime clone of the terrain data (so editor asset is not modified).</summary>
    [HideInInspector] public TerrainData clonedData;

    private List<TreeInstance> treeList;

    private void Start()
    {
        // Clone terrain data at runtime
        clonedData = Instantiate(terrain.terrainData);
        terrain.terrainData = clonedData;

        // Build initial tree list (filtering prototype indices of interest: 0–3)
        treeList = new List<TreeInstance>(clonedData.treeInstances);
        treeList = treeList.FindAll(tree => tree.prototypeIndex >= 0 && tree.prototypeIndex <= 3);

        // Schedule repeated tree deaths
        InvokeRepeating(nameof(KillNearbyRandomTree), interval, interval);

        Debug.Log(treeList.Count);
    }

    /// <summary>
    /// Finds a random nearby tree (within radius) and triggers its death sequence.
    /// </summary>
    private void KillNearbyRandomTree()
    {
        if (treeList.Count == 0)
        {
            CancelInvoke(nameof(KillNearbyRandomTree));
            Debug.Log("✅ All trees were removed.");
            return;
        }

        Vector3 playerPos = vrPlayer.position;
        float radius = 30f;

        // Collect indices of nearby trees
        List<int> nearbyIndices = new List<int>();
        for (int i = 0; i < treeList.Count; i++)
        {
            Vector3 worldPos = Vector3.Scale(treeList[i].position, terrain.terrainData.size) + terrain.transform.position;
            float distance = Vector3.Distance(playerPos, worldPos);

            if (distance <= radius)
                nearbyIndices.Add(i);
        }

        if (nearbyIndices.Count == 0)
        {
            Debug.Log("⛔ No trees found within radius of player.");
            return;
        }

        // Pick a random tree among nearby ones
        int randomIndex = nearbyIndices[Random.Range(0, nearbyIndices.Count)];
        TreeInstance dyingTree = treeList[randomIndex];

        int prototypeIndex = dyingTree.prototypeIndex;
        TreeTypeData treeType = TreeDatabase.Instance.GetTreeType(prototypeIndex);
        if (treeType == null || treeType.animatedTreePrefab == null)
        {
            Debug.LogWarning($"[TreeDeathManager] No animated prefab found for prototype index {prototypeIndex}");
            return;
        }

        // Instantiate animated tree prefab at the tree’s location
        Vector3 treeWorldPos = Vector3.Scale(dyingTree.position, terrain.terrainData.size) + terrain.transform.position;
        float yRotation = dyingTree.rotation * 360f;
        Quaternion rotation = Quaternion.Euler(0, yRotation + 180, 0);

        GameObject newTree = Instantiate(treeType.animatedTreePrefab, treeWorldPos, rotation);
        StartCoroutine(AnimateFallingTree(newTree, prototypeIndex));

        // Remove the tree from terrain
        treeList.RemoveAt(randomIndex);
        clonedData.treeInstances = treeList.ToArray();

        Debug.Log($"🌳 A random tree near player has died. Remaining trees: {treeList.Count}");
    }

    /// <summary>
    /// Plays the full falling/dissolve sequence for a tree prefab.
    /// </summary>
    private IEnumerator AnimateFallingTree(GameObject treeGO, int prototypeIndex)
    {
        // Step 1: Activate corruption aura if present
        Transform aura = treeGO.transform.Find("CorruptionAura");
        if (aura != null) aura.gameObject.SetActive(true);

        // Step 2: Wait to show aura
        yield return new WaitForSeconds(2f);

        // Step 3: Remove aura
        if (aura != null) Destroy(aura.gameObject);

        // Step 4: Play tree falling animation
        Animator anim = treeGO.GetComponent<Animator>();
        if (anim != null) anim.Play("TreeFall");

        // Step 5: Wait until animation ends (approx. 2.5s)
        yield return new WaitForSeconds(2.5f);

        // Step 6: Force LOD to final level for dissolve
        LODGroup lodGroup = treeGO.GetComponentInChildren<LODGroup>();
        if (lodGroup != null) lodGroup.ForceLOD(3);

        // Step 7: Attempt to locate LOD3 child renderer for dissolve
        Transform lod3 = null;
        if (treeGO.transform.childCount >= 1)
        {
            Transform modelRoot = treeGO.transform.GetChild(0);
            int lastIndex = modelRoot.childCount - 1;
            if (lastIndex >= 0)
                lod3 = modelRoot.GetChild(lastIndex);
        }

        if (lod3 != null)
        {
            Renderer renderer = lod3.GetComponent<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(DissolveTreeViaAlphaCutoff(treeGO, renderer, prototypeIndex));
                yield break; // Remainder handled in dissolve coroutine
            }
        }

        // If dissolve target not found, just destroy
        Destroy(treeGO);
    }

    /// <summary>
    /// Performs dissolve effect by animating the material’s alpha cutoff, then spawns a seed and destroys the tree.
    /// </summary>
    private IEnumerator DissolveTreeViaAlphaCutoff(GameObject treeGO, Renderer renderer, int prototypeIndex, float duration = 2f)
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

        // Ensure final cutoff applied
        foreach (var mat in materials)
        {
            if (mat.HasProperty("_Cutoff"))
                mat.SetFloat("_Cutoff", end);
        }

        // Spawn a seed collectible at the fallen tree’s base
        if (seedPrefab != null)
        {
            Vector3 rayOrigin = renderer.bounds.center + Vector3.up * 5f;
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, 10f))
            {
                Vector3 spawnPos = hitInfo.point + Vector3.up * 0.1f;
                GameObject seed = Instantiate(seedPrefab, spawnPos, Quaternion.identity);

                // Assign prototype index to the new seed
                SeedCollectible seedScript = seed.GetComponentInChildren<SeedCollectible>();
                if (seedScript != null)
                    seedScript.prototypeIndex = prototypeIndex;
            }
            else
            {
                Debug.LogWarning("🌱 [TreeDeathManager] Raycast failed to find ground for seed spawn.");
            }
        }

        Destroy(treeGO);
    }
}
