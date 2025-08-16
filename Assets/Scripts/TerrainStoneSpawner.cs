// TerrainStoneSpawner.cs
using UnityEngine;

public class TerrainStoneSpawner : MonoBehaviour
{
    public Terrain terrain;            // اگر خالیه، از activeTerrain می‌گیریم
    public GameObject stonePrefab;     // Prefab: Mesh + Rigidbody + Collider + StoneHighlightable(+ThrowableStone)
    public int count = 1500;           // تعداد کل (مواظب پرفورمنس)
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float yOffset = 0.02f;      // کمی بالاتر از زمین
    [Tooltip("شیب بالاتر از این مقدار سنگ نمی‌نشانیم (0..90)")]
    public float maxSlope = 55f;

    void Start()
    {
        if (!terrain) terrain = Terrain.activeTerrain;
        var td = terrain.terrainData;
        Vector3 tPos = terrain.GetPosition();
        Vector3 size = td.size;

        // تنظیمات تیلت اضافی
        float extraTiltMax = 8f;     // حداکثر 8 درجه کج بشه (اختیاری)

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0f, size.x);
            float z = Random.Range(0f, size.z);

            float slope = td.GetSteepness(x / size.x, z / size.z);
            if (slope > maxSlope) { i--; continue; }

            float y = td.GetInterpolatedHeight(x / size.x, z / size.z) + tPos.y;
            Vector3 pos = new Vector3(tPos.x + x, y + yOffset, tPos.z + z);

            // 1) نرمال زمین در این نقطه
            Vector3 normal = td.GetInterpolatedNormal(x / size.x, z / size.z).normalized;

            // 2) هم‌تراز کردن up با نرمال
            Quaternion align = Quaternion.FromToRotation(Vector3.up, normal);

            // 3) یـو رندوم حول خودِ نرمال سطح
            Quaternion yaw = Quaternion.AngleAxis(Random.Range(0f, 360f), normal);

            // 4) تیلت خیلی کمِ رندوم حول یک بردار مماس (اختیاری)
            Vector3 tangent = Vector3.Cross(normal, Random.onUnitSphere).normalized;
            if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.Cross(normal, Vector3.right).normalized;
            Quaternion tilt = Quaternion.AngleAxis(Random.Range(-extraTiltMax, extraTiltMax), tangent);

            Quaternion rot = yaw * tilt * align;

            // داخل TerrainStoneSpawner – همون جایی که Instantiate می‌کردی
            Transform parent = this.transform; // همون GameObject که اسکریپت روشه (Stones)
            var go = Instantiate(stonePrefab, pos, rot, parent);


            // مقیاس رندوم *بر اساس* اسکِیل پریفب
            float k = Random.Range(minScale, maxScale);
            Vector3 baseS = stonePrefab.transform.localScale;
            go.transform.localScale = new Vector3(baseS.x * k, baseS.y * k, baseS.z * k);
        }
    }

}
