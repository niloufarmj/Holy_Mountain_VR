using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns groups of animals across a Unity Terrain in clustered formations.
/// Each <see cref="AnimalGroup"/> defines multiple prefabs and how many clusters/animals to spawn.
/// This component samples the terrain height to place animals flush with the ground,
/// and ensures spawned positions stay within the terrain bounds.
/// </summary>
public class AnimalSpawner : MonoBehaviour
{
    /// <summary>
    /// Configuration for a group of related animal prefabs to be spawned in clusters.
    /// </summary>
    [System.Serializable]
    public class AnimalGroup
    {
        /// <summary>
        /// Display name for this animal group (for organization in the Inspector).
        /// </summary>
        public string groupName;

        /// <summary>
        /// Prefabs to spawn for this group. A random prefab is chosen for each animal.
        /// </summary>
        public GameObject[] prefabs;

        /// <summary>
        /// Number of clusters to spawn for this group.
        /// </summary>
        public int clusterCount = 10;

        /// <summary>
        /// Number of animals to spawn per cluster.
        /// </summary>
        public int animalsPerCluster = 5;

        /// <summary>
        /// Radius (in world units) around the cluster center within which animals are distributed.
        /// </summary>
        public float clusterRadius = 20f;
    }

    /// <summary>
    /// Target terrain used for height sampling and boundary checks.
    /// If left null, the active terrain will be used.
    /// </summary>
    public Terrain terrain;

    /// <summary>
    /// List of animal groups to spawn when the scene starts.
    /// </summary>
    public List<AnimalGroup> animalGroups;

    /// <summary>
    /// Max attempts to find a valid point near a cluster center before falling back to the center.
    /// </summary>
    public int maxSpawnAttempts = 100;

    /// <summary>
    /// Layer used for ground queries if needed elsewhere (not used directly here but kept for extensibility).
    /// </summary>
    public LayerMask groundLayer;

    // Cached terrain data and origin position for efficient queries.
    private TerrainData terrainData;
    private Vector3 terrainPos;

    private void Start()
    {
        // Default to the active terrain if none is assigned.
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        terrainData = terrain.terrainData;
        terrainPos = terrain.transform.position;

        SpawnAllGroups();
    }

    /// <summary>
    /// Spawns all configured animal groups.
    /// </summary>
    private void SpawnAllGroups()
    {
        foreach (var group in animalGroups)
        {
            SpawnAnimalClusters(group);
        }
    }

    /// <summary>
    /// Spawns clusters for a specific <paramref name="group"/> across the terrain.
    /// Each cluster gets a random center; animals are scattered within <see cref="AnimalGroup.clusterRadius"/>.
    /// </summary>
    /// <param name="group">The animal group to spawn.</param>
    public void SpawnAnimalClusters(AnimalGroup group)
    {
        for (int i = 0; i < group.clusterCount; i++)
        {
            // Pick a random cluster center on the terrain.
            Vector3 clusterCenter = GetRandomPointOnTerrain();

            // Spawn animals around the cluster center.
            for (int j = 0; j < group.animalsPerCluster; j++)
            {
                Vector3 spawnPos = GetRandomPointNear(clusterCenter, group.clusterRadius);

                // Choose a random prefab from the group's list.
                GameObject prefab = group.prefabs[Random.Range(0, group.prefabs.Length)];

                // Instantiate at sampled ground height with no rotation.
                GameObject animal = Instantiate(prefab, spawnPos, Quaternion.identity);

                // Ensure animals have a basic wander behavior if not already present.
                if (animal.GetComponent<AnimalWander>() == null)
                    animal.AddComponent<AnimalWander>();
            }
        }
    }

    /// <summary>
    /// Returns a random position on the terrain, sampling height so the point sits on the ground.
    /// </summary>
    private Vector3 GetRandomPointOnTerrain()
    {
        float x = Random.Range(0f, terrainData.size.x);
        float z = Random.Range(0f, terrainData.size.z);
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
        return new Vector3(x + terrainPos.x, y, z + terrainPos.z);
    }

    /// <summary>
    /// Returns a random valid position within <paramref name="radius"/> of <paramref name="center"/>.
    /// Ensures the position lies within terrain bounds; falls back to <paramref name="center"/> if none found.
    /// </summary>
    private Vector3 GetRandomPointNear(Vector3 center, float radius)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Random 2D offset within the circle.
            Vector2 offset = Random.insideUnitCircle * radius;
            float x = center.x + offset.x;
            float z = center.z + offset.y;

            // Check bounds against the terrain rectangle.
            if (x >= terrainPos.x && x <= terrainPos.x + terrainData.size.x &&
                z >= terrainPos.z && z <= terrainPos.z + terrainData.size.z)
            {
                // Sample terrain height at the candidate x/z.
                float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
                return new Vector3(x, y, z);
            }
        }

        // If we failed to find a valid point within attempts, use the center itself.
        return center;
    }
}
