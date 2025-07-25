using UnityEngine;

public class TreePlanter : MonoBehaviour
{
    public Transform controllerRayOrigin;
    public float rayDistance = 50f;
    public LayerMask groundLayer;
    public Terrain targetTerrain;

    private GameStats stats;

    [HideInInspector] public int selectedPrototypeIndex = -1; // از UI ست می‌شه

    void Start()
    {
        stats = FindObjectOfType<GameStats>();
        if (stats == null)
        {
            Debug.LogWarning("[TreePlanter] GameStats not found in scene.");
        }
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)) // دکمه B برای کاشت
        {
            TryPlantSelectedTree();
        }
    }

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

    void HandleTreeFullyGrown(TreeGrower grower, int prototypeIndex)
    {
        Vector3 worldPos = grower.transform.position;
        Vector3 terrainPos = worldPos - targetTerrain.transform.position;

        Vector3 normalizedPos = new Vector3(
            terrainPos.x / targetTerrain.terrainData.size.x,
            0f,
            terrainPos.z / targetTerrain.terrainData.size.z
        );

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
        Destroy(grower.gameObject);
    }
}
