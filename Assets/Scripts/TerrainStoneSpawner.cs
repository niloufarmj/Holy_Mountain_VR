// TerrainStoneSpawner.cs
using UnityEngine;

/// <summary>
/// Spawns a number of stone prefabs across a terrain surface,
/// respecting terrain slope and applying random position, rotation, and scale.
/// - Places stones slightly above the ground to avoid z-fighting.
/// - Aligns each stone to the terrain normal.
/// - Applies a random yaw and optional tilt for natural variation.
/// - Parents all spawned stones under this GameObject.
/// </summary>
public class TerrainStoneSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Target terrain. If null, uses Terrain.activeTerrain.")]
    public Terrain terrain;

    [Tooltip("Stone prefab (should include Mesh, Rigidbody, Collider, StoneHighlightable, and optionally ThrowableStone).")]
    public GameObject stonePrefab;

    [Header("Spawn Settings")]
    [Tooltip("Total number of stones to spawn. Large numbers can affect performance.")]
    public int count = 1500;

    [Tooltip("Minimum scale multiplier applied to prefab scale.")]
    public float minScale = 0.8f;

    [Tooltip("Maximum scale multiplier applied to prefab scale.")]
    public float maxScale = 1.2f;

    [Tooltip("Vertical offset to lift stones slightly above the terrain surface.")]
    public float yOffset = 0.02f;

    [Tooltip("Maximum terrain slope angle (0..90) allowed for placing stones.")]
    public float maxSlope = 55f;

    private void Start()
    {
        if (!terrain) terrain = Terrain.activeTerrain;
        var td = terrain.terrainData;
        Vector3 tPos = terrain.GetPosition();
        Vector3 size = td.size;

        // Extra random tilt angle (degrees) for added realism
        float extraTiltMax = 8f;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0f, size.x);
            float z = Random.Range(0f, size.z);

            // Skip if terrain slope is too steep
            float slope = td.GetSteepness(x / size.x, z / size.z);
            if (slope > maxSlope) { i--; continue; }

            // Terrain height at this position
            float y = td.GetInterpolatedHeight(x / size.x, z / size.z) + tPos.y;
            Vector3 pos = new Vector3(tPos.x + x, y + yOffset, tPos.z + z);

            // 1) Terrain surface normal
            Vector3 normal = td.GetInterpolatedNormal(x / size.x, z / size.z).normalized;

            // 2) Align stone's up vector with the terrain normal
            Quaternion align = Quaternion.FromToRotation(Vector3.up, normal);

            // 3) Apply random yaw rotation around the normal
            Quaternion yaw = Quaternion.AngleAxis(Random.Range(0f, 360f), normal);

            // 4) Apply small random tilt around a tangent vector (optional realism)
            Vector3 tangent = Vector3.Cross(normal, Random.onUnitSphere).normalized;
            if (tangent.sqrMagnitude < 1e-4f)
                tangent = Vector3.Cross(normal, Vector3.right).normalized;

            Quaternion tilt = Quaternion.AngleAxis(Random.Range(-extraTiltMax, extraTiltMax), tangent);

            // Final rotation combines yaw, tilt, and alignment
            Quaternion rot = yaw * tilt * align;

            // Spawn under this spawner object (parent transform)
            Transform parent = this.transform;
            var go = Instantiate(stonePrefab, pos, rot, parent);

            // Apply random scale relative to prefab's base scale
            float k = Random.Range(minScale, maxScale);
            Vector3 baseS = stonePrefab.transform.localScale;
            go.transform.localScale = new Vector3(baseS.x * k, baseS.y * k, baseS.z * k);
        }
    }
}
