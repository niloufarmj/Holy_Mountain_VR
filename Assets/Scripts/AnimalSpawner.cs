using UnityEngine;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [System.Serializable]
    public class AnimalGroup
    {
        public string groupName;
        public GameObject[] prefabs;
        public int clusterCount = 10;            // تعداد خوشه‌هایی که از این گروه اسپاون می‌شن
        public int animalsPerCluster = 5;        // تعداد حیوانات در هر خوشه
        public float clusterRadius = 20f;        // شعاع پراکندگی حیوانات در هر خوشه
    }

    public Terrain terrain;
    public List<AnimalGroup> animalGroups;
    public int maxSpawnAttempts = 100;
    public LayerMask groundLayer;

    private TerrainData terrainData;
    private Vector3 terrainPos;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        terrainData = terrain.terrainData;
        terrainPos = terrain.transform.position;

        SpawnAllGroups();
    }

    void SpawnAllGroups()
    {
        foreach (var group in animalGroups)
        {
            SpawnAnimalClusters(group);
        }
    }

    public void SpawnAnimalClusters(AnimalGroup group)
    {
        for (int i = 0; i < group.clusterCount; i++)
        {
            Vector3 clusterCenter = GetRandomPointOnTerrain();

            for (int j = 0; j < group.animalsPerCluster; j++)
            {
                Vector3 spawnPos = GetRandomPointNear(clusterCenter, group.clusterRadius);
                GameObject prefab = group.prefabs[Random.Range(0, group.prefabs.Length)];
                GameObject animal = Instantiate(prefab, spawnPos, Quaternion.identity);

                if (animal.GetComponent<AnimalWander>() == null)
                    animal.AddComponent<AnimalWander>();
            }
        }
    }

    Vector3 GetRandomPointOnTerrain()
    {
        float x = Random.Range(0f, terrainData.size.x);
        float z = Random.Range(0f, terrainData.size.z);
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
        return new Vector3(x + terrainPos.x, y, z + terrainPos.z);
    }

    Vector3 GetRandomPointNear(Vector3 center, float radius)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            float x = center.x + offset.x;
            float z = center.z + offset.y;

            if (x >= terrainPos.x && x <= terrainPos.x + terrainData.size.x &&
                z >= terrainPos.z && z <= terrainPos.z + terrainData.size.z)
            {
                float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;
                return new Vector3(x, y, z);
            }
        }

        return center;
    }
}
