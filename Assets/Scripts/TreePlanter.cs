using UnityEngine;

public class TreePlanter : MonoBehaviour
{
    public GameObject seedTreePrefab;
    public Transform controllerRayOrigin;
    public float rayDistance = 50f;
    public LayerMask groundLayer;
    public Terrain targetTerrain;
    public int treePrototypeIndex = 0;

    private GameStats stats;

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
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            if (stats == null || seedTreePrefab == null || targetTerrain == null) return;
            if (!HasSeeds()) return;

            Ray ray = new Ray(controllerRayOrigin.position, controllerRayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundLayer))
            {
                GameObject tree = Instantiate(seedTreePrefab, hit.point, Quaternion.identity);
                TreeGrower grower = tree.GetComponent<TreeGrower>();
                if (grower != null)
                {
                    grower.StartGrowth();
                    grower.OnGrowthComplete += HandleTreeFullyGrown;
                }

                SubtractSeed();
                stats.AddTree();
            }
        }
    }

    void HandleTreeFullyGrown(TreeGrower grower)
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
            prototypeIndex = treePrototypeIndex,
            widthScale = 1f,
            heightScale = 1f,
            color = Color.white,
            lightmapColor = Color.white
        };

        targetTerrain.AddTreeInstance(newTree);
        Destroy(grower.gameObject);
    }

    bool HasSeeds()
    {
        var seedField = typeof(GameStats).GetField("seedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)seedField.GetValue(stats) > 0;
    }

    void SubtractSeed()
    {
        var seedField = typeof(GameStats).GetField("seedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int currentCount = (int)seedField.GetValue(stats);
        seedField.SetValue(stats, currentCount - 1);
        stats.seedsText.text = $"Seeds Collected: {currentCount - 1}";
    }
}
