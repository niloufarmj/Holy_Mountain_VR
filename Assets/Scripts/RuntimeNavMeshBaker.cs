using UnityEngine;
using Unity.AI.Navigation; // ✅ مخصوص NavMeshSurface و دیگر ابزارها

public class RuntimeNavMeshBaker : MonoBehaviour
{
    public NavMeshSurface surface;

    void Start()
    {
        // صبر می‌کنیم تا Terrain کامل لود بشه
        StartCoroutine(DelayedBake());
    }

    System.Collections.IEnumerator DelayedBake()
    {
        yield return new WaitForSeconds(2f); // صبر برای اطمینان از لود کامل Terrain
        if (surface != null)
        {
            Debug.Log("Building NavMesh at runtime...");
            surface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("NavMeshSurface is missing!");
        }
    }
}
