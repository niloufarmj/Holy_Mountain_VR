using UnityEngine;

/// <summary>
/// Handles planting trees in the world:
/// - Listens for VR or keyboard input to attempt planting the currently selected tree type.
/// - Raycasts from a controller origin to place the tree on valid ground.
/// - Instantiates a growable tree prefab and starts its growth process.
/// - When fully grown, replaces it with a permanent terrain tree instance.
/// - Consumes a seed from <see cref="GameStats"/> and increments planted tree count.
/// </summary>
public class TreePlanter : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Origin transform for raycast (usually controller position/orientation).")]
    public Transform controllerRayOrigin;

    [Tooltip("Max distance for ground placement raycast.")]
    public float rayDistance = 50f;

    [Tooltip("Layer mask for what counts as ground when planting.")]
    public LayerMask groundLayer;

    [Tooltip("Target terrain where permanent tree instances are stored.")]
    public Terrain targetTerrain;

    private GameStats stats;

    /// <summary>
    /// Prototype index of the tree type selected by the UI.
    /// -1 means no selection.
    /// </summary>
    [HideInInspector] public int selectedPrototypeIndex = -1;

    private void Start()
    {
        stats = FindObjectOfType<GameStats>();
        if (stats == null)
        {
            Debug.LogWarning("[TreePlanter] GameStats not found in scene.");
        }
    }

    private void Update()
    {
        // Plant button: Oculus B button or fallback P key
        if (OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.P))
        {
            TryPlantSelectedTree();
        }
    }

    /// <summary>
    /// Attempts to plant the currently selected tree:
    /// - Verifies seed availability.
    /// - Raycasts to find a valid ground hit point.
    /// - Instantiates a growable prefab and starts its growth.
    /// - Registers callback for when tree finishes growing.
    /// - Consumes the seed and increments tree counter.
    /// </summary>
    public void TryPlantSelectedTree()
    {
        if (stats == null || targetTerrain == null) return;
        if (selectedPrototypeIndex < 0) return;
        if (!stats.HasSeed(selectedPrototypeIndex)) return;

        Ray ray = new Ray(controllerRayOrigin.position, controllerRayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundLayer))
        {
            TreeTypeData data = TreeDatabase.Instance.GetTreeType(selectedPrototypeIndex);
            if (data == null || data.growableTreePrefab == null)
            {
                Debug.LogWarning($"[TreePlanter] No prefab found for prototype {selectedPrototypeIndex}");
                return;
            }

            // Spawn growable prefab
            GameObject tree = Instantiate(data.growableTreePrefab, hit.point, Quaternion.identity);
            TreeGrower grower = tree.GetComponent<TreeGrower>();
            if (grower != null)
            {
                grower.StartGrowth();
                grower.OnGrowthComplete += g => HandleTreeFullyGrown(g, selectedPrototypeIndex);
            }

            stats.UseSeed(selectedPrototypeIndex);
            stats.AddTree();
        }
    }

    /// <summary>
    /// Called when a planted tree finishes growth.
    /// Replaces the growable prefab with a permanent terrain tree instance.
    /// </summary>
    private void HandleTreeFullyGrown(TreeGrower grower, int prototypeIndex)
    {
        Vector3 worldPos = grower.transform.position;
        Vector3 terrainPos = worldPos - targetTerrain.transform.position;

        Vector3 normalizedPos = new Vector3(
            terrainPos.x / targetTerrain.terrainData.size.x,
            0f,
            terrainPos.z / targetTerrain.terrainData.size.z
        );

        // Add a tree instance to the terrain data
        TreeInstance newTree = new TreeInstance
        {
            position = normalizedPos,
            prototypeIndex = prototypeIndex,
            widthScale = 1f,
            heightScale = 1f,
            color = Color.white,
            lightmapColor = Color.white
        };

        targetTerrain.AddTreeInstance(newTree);

        // Remove the growable prefab from the scene
        Destroy(grower.gameObject);
    }
}
