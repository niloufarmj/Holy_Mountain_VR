using System.Collections.Generic;
using UnityEngine;

public class ProximityStoneHighlighter : MonoBehaviour
{
    public float radius = 8f;
    public LayerMask stoneLayer;           // set in Inspector to 'Stone'
    public float scanInterval = 0.25f;
    public bool debugLogs = true;

    HashSet<StoneHighlightable> active = new HashSet<StoneHighlightable>();
    Collider[] hits = new Collider[512];
    float timer;

    void Start()
    {
        // Auto-fix: if not set in Inspector, try "Stone" layer by name
        if (stoneLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Stone");
            if (idx >= 0) stoneLayer = 1 << idx;
            if (debugLogs) Debug.Log($"[PSH] stoneLayer was 0; set to 'Stone' -> {stoneLayer.value}");
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = scanInterval;

        int n = Physics.OverlapSphereNonAlloc(
            transform.position, radius, hits, stoneLayer, QueryTriggerInteraction.Ignore);

        if (debugLogs) Debug.Log($"[PSH] hits: {n} within r={radius}");

        // mark seen
        HashSet<StoneHighlightable> seen = new HashSet<StoneHighlightable>();
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (!h) continue;

            // robust: search up the hierarchy
            var s = h.GetComponent<StoneHighlightable>();
            if (!s) continue;

            seen.Add(s);
            if (!active.Contains(s))
                s.SetHighlighted(true);
        }

        // turn off gone ones
        foreach (var s in active)
            if (!seen.Contains(s))
                s.SetHighlighted(false);

        active = seen;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0,1,1,0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
