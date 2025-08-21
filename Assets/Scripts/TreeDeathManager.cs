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

        var tc = terrain.GetComponent<TerrainCollider>();
        if (tc && tc.terrainData != terrain.terrainData)
            tc.terrainData = terrain.terrainData;   // keep collider in sync with cloned TD
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

        DisableTreeColliders(newTree);   // ← add this line

        StartCoroutine(AnimateFallingTree(newTree, prototypeIndex, treeWorldPos));

        // Remove the tree from terrain
        treeList.RemoveAt(randomIndex);

        // WRITE BACK FIRST (critical)
        clonedData.treeInstances = treeList.ToArray();

        // NOW force the terrain to rebuild its tree-collider cache
        RefreshTerrainTreeColliders(terrain);
        

    
        Debug.Log($"🌳 A random tree near player has died. Remaining trees: {treeList.Count}");
    }

   
    // Forces terrain to rebuild tree colliders by doing a no-op height write.
    // Uses (0,0,0,0) like the forum trick; if not supported, falls back to 1x1.
    void ForceRebuildTerrainTreeColliders_HeightPoke(Terrain t)
    {
        if (!t || !t.terrainData) return;
        var td = t.terrainData;

        try
        {
            // The exact trick from the Unity forum post:
            float[,] h = td.GetHeights(0, 0, 0, 0);
            td.SetHeights(0, 0, h);
        }
        catch
        {
            // Some Unity versions require positive sizes — use 1x1 (still very cheap)
            float[,] h1 = td.GetHeights(0, 0, 1, 1);
            #if UNITY_6000_0_OR_NEWER
            td.SetHeightsDelayLOD(0, 0, h1);
            td.SyncHeightmap();
            #else
            td.SetHeights(0, 0, h1);
            #endif
        }

        // Make sure physics picks it up immediately
        try { t.Flush(); } catch { }
        Physics.SyncTransforms();
    }


    // EXACT forum trick + extra nudges for Unity 6
    void RefreshTerrainTreeColliders(Terrain t)
    {
        if (!t || !t.terrainData) return;
        var td = t.terrainData;

        // 1) Forum "height-poke": read/write back unchanged heights
        try
        {
            // Some Unity versions accept 0x0
            float[,] h = td.GetHeights(0, 0, 0, 0);
            td.SetHeights(0, 0, h);
        }
        catch
        {
            // Fallback: 1x1 (same effect, still very cheap)
            float[,] h1 = td.GetHeights(0, 0, 1, 1);
            #if UNITY_6000_0_OR_NEWER
            td.SetHeightsDelayLOD(0, 0, h1);
            td.SyncHeightmap();
            #else
            td.SetHeights(0, 0, h1);
            #endif
        }

        // 2) Extra nudge: toggle TerrainCollider + ensure it points at the same data
        var tc = t.GetComponent<TerrainCollider>();
        if (tc)
        {
            tc.enabled = false;
            tc.terrainData = td;   // self-assign is OK; forces internal refresh
            tc.enabled = true;
        }

        // 3) Make physics pick it up immediately
        try { t.Flush(); } catch { }
        Physics.SyncTransforms();
    }



    /// <summary>
    /// Plays the full falling/dissolve sequence for a tree prefab.
    /// </summary>
    private IEnumerator AnimateFallingTree(GameObject treeGO, int prototypeIndex, Vector3 baseWorldPos)
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
                StartCoroutine(DissolveTreeViaAlphaCutoff(treeGO, renderer, prototypeIndex, baseWorldPos));
                yield break; // Remainder handled in dissolve coroutine
            }
        }

        // If dissolve target not found, just destroy
        Destroy(treeGO);
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        var playerCapsule = FindObjectOfType<CapsuleCollider>();
        int killed = CleanupGhostTreeColliders(baseWorldPos, 1.25f, playerCapsule);
        if (killed > 0) Debug.Log($"[TreeDeath] Removed {killed} ghost colliders near stump.");
    }

    /// <summary>
    /// Performs dissolve effect by animating the material’s alpha cutoff, then spawns a seed and destroys the tree.
    /// </summary>
    private IEnumerator DissolveTreeViaAlphaCutoff(GameObject treeGO, Renderer renderer, int prototypeIndex, Vector3 baseWorldPos, float duration = 2f)
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
        // --- جایگزین کامل منطق اسپاون ---
        if (seedPrefab != null)
        {
            Vector3 spawnPos;

            // 1) تلاش اول: Raycast از بالای baseWorldPos به پایین (پایدار روی هر سطح/کالایدر)
            Vector3 rayStart = baseWorldPos + Vector3.up * 5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point + Vector3.up * 0.1f;
            }
            else
            {
                // 2) fallback: SampleHeight از Terrain
                float y = baseWorldPos.y;
                if (terrain != null && terrain.terrainData != null)
                    y = terrain.SampleHeight(baseWorldPos) + terrain.transform.position.y;

                spawnPos = new Vector3(baseWorldPos.x, y, baseWorldPos.z) + Vector3.up * 0.1f;
            }

            DisableTreeColliders(treeGO);

            GameObject seed = Instantiate(seedPrefab, spawnPos, Quaternion.identity);
            NormalizeSeedColliders(seed);

            // prototypeIndex را ست کن (مثل قبل)
            SeedCollectible sc = seed.GetComponentInChildren<SeedCollectible>();
            if (sc != null) sc.prototypeIndex = prototypeIndex;
        }

        Destroy(treeGO);
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        var playerCapsule = FindObjectOfType<CapsuleCollider>();
        int killed = CleanupGhostTreeColliders(baseWorldPos , 1.25f, playerCapsule);
        if (killed > 0) Debug.Log($"[TreeDeath] Removed {killed} ghost colliders near stump.");
    }

    private void NormalizeSeedColliders(GameObject seed)
    {
        if (!seed) return;

        // Ensure all colliders on the seed are triggers and put them on the Seed layer.
        int seedLayer = LayerMask.NameToLayer("Seed");
        foreach (var c in seed.GetComponentsInChildren<Collider>(true))
        {
            c.isTrigger = true;
            if (seedLayer >= 0) c.gameObject.layer = seedLayer;
        }

        // Keep pickup radius sane
        var sc = seed.GetComponentInChildren<SphereCollider>();
        if (sc && sc.radius > 0.25f) sc.radius = 0.15f;
    }

    void DisableTreeColliders(GameObject treeGO)
    {
        foreach (var c in treeGO.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in treeGO.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
    }

    // Call right after you remove a TreeInstance from terrainData
    void ForceRebuildTerrainTreeColliders(Terrain terrain)
    {
        if (!terrain) return;

        // 1) Try the direct flush (older and many current Unity versions)
        try { terrain.Flush(); } catch { }

        // 2) Fallback: toggle drawTrees to force a rebuild path
        bool prev = terrain.drawTreesAndFoliage;
        terrain.drawTreesAndFoliage = false;
        // one frame so terrain systems have time to rebuild
        // (call this from a coroutine or follow with WaitForEndOfFrame)
        terrain.drawTreesAndFoliage = prev;

        // 3) Re-sync physics transforms
        Physics.SyncTransforms();
    }
    
    int CleanupGhostTreeColliders(Vector3 center, float radius, CapsuleCollider playerCapsule)
    {
        int disabled = 0;
        var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            if (!c || !c.enabled || c.isTrigger) continue;
            if (c is TerrainCollider) continue;                          // keep terrain
            if (playerCapsule && c.transform.IsChildOf(playerCapsule.transform)) continue;

            // skip seed pickup triggers
            if (c.GetComponentInParent<SeedCollectible>()) continue;

            // heuristics: no renderer or treey names (common with Gaia)
            var rend = c.GetComponentInParent<Renderer>();
            string n = c.name.ToLowerInvariant();
            bool noVisibleRenderer = (rend == null || !rend.enabled);
            bool looksTreey = n.Contains("tree") || n.Contains("trunk") ||
                            n.Contains("collider") || n.Contains("gaia") ||
                            n.Contains("pw") || n.Contains("impostor") || n.Contains("lod");

            if (noVisibleRenderer || looksTreey)
            {
                c.enabled = false;
                disabled++;
                Debug.Log($"[GhostCleanup] Disabled leftover collider: {c.name}  path={c.transform.root.name}/{c.transform.name}");
            }
        }
        return disabled;
    }

}
