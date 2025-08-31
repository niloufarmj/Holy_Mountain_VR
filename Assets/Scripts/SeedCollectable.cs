// SeedCollectible.cs  (PASSIVE version)
// Put this on each seed prefab (usually on the visible child). 
// Holds prototypeIndex and exposes TryCollect(...) for the controller.
using UnityEngine;

public class SeedCollectible : MonoBehaviour
{
    [Tooltip("Tree prototype index this seed belongs to.")]
    public int prototypeIndex;

    private bool _collected = false;

    /// <summary>
    /// Increments seed count in GameStats and removes the seed from the scene.
    /// Returns true if collection happened now, false if already collected.
    /// </summary>
    public bool TryCollect(GameStats stats)
    {
        if (_collected) return false;
        _collected = true;

        if (stats != null)
            stats.AddSeed(prototypeIndex);

        // Keep old behaviour: remove the whole seed object (child or root)
        var root = transform.parent ? transform.parent.gameObject : gameObject;
        Object.Destroy(root);
        return true;
    }
}
