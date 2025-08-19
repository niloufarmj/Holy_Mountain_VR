using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Central HUD/game-state counter for seeds, stones, and trees.
/// - Maintains a per-prototype seed inventory (counts per tree type).
/// - Updates TextMeshPro UI labels for stones and trees when they change.
/// Note: Seed counts are tracked in <see cref="seedInventory"/> but are not
/// directly printed to UI here; that can be added by the caller if desired.
/// </summary>
public class GameStats : MonoBehaviour
{
    [Header("UI References (TextMeshProUGUI)")]
    [Tooltip("Optional: Label for displaying total seeds (not auto-updated in this class).")]
    public TextMeshProUGUI seedsText;

    [Tooltip("Label that displays 'Stones Collected: X'.")]
    public TextMeshProUGUI stonesText;

    [Tooltip("Label that displays 'Trees Planted: X'.")]
    public TextMeshProUGUI treesText;

    [Header("Seed Inventory")]
    [Tooltip("Per-prototype seed counts (e.g., different tree types).")]
    public List<SeedInventoryEntry> seedInventory = new List<SeedInventoryEntry>();

    // Internal tallies for stones/trees (displayed when changed).
    private int seedCount = 0, stoneCount = 0, treeCount = 0; // seedCount unused by current API; kept for compatibility

    /// <summary>
    /// Adds one seed of the given prototype type into the inventory.
    /// If the type doesn't exist yet, a new entry is created.
    /// </summary>
    /// <param name="prototypeIndex">Tree prototype index (type identifier).</param>
    public event System.Action OnSeedsChanged;

    public void AddSeed(int prototypeIndex)
    {
        var entry = seedInventory.Find(e => e.prototypeIndex == prototypeIndex);
        if (entry != null) entry.count++;
        else seedInventory.Add(new SeedInventoryEntry { prototypeIndex = prototypeIndex, count = 1 });
        OnSeedsChanged?.Invoke();   // NEW
    }

    /// <summary>
    /// Checks if there is at least one seed available for the given prototype index.
    /// </summary>
    /// <param name="prototypeIndex">Tree prototype index to query.</param>
    /// <returns>True if inventory contains one or more; otherwise false.</returns>
    public bool HasSeed(int prototypeIndex) => seedInventory.Exists(e => e.prototypeIndex == prototypeIndex && e.count > 0);

    /// <summary>
    /// Consumes one seed of the specified prototype type, if available.
    /// Does nothing if the type is missing or the count is zero.
    /// </summary>
    /// <param name="prototypeIndex">Tree prototype index to consume.</param>
    

    public void UseSeed(int prototypeIndex)
    {
        var entry = seedInventory.Find(e => e.prototypeIndex == prototypeIndex);
        if (entry != null && entry.count > 0)
        {
            entry.count--;
            OnSeedsChanged?.Invoke(); // NEW
        }
    }

    /// <summary>
    /// Increments planted trees by one and updates the trees UI label.
    /// </summary>
    public void AddTree()
    {
        treeCount++;
        if (treesText != null)
            treesText.text = $"Trees Planted: {treeCount}";
    }
}

/// <summary>
/// Inventory entry for seeds of a specific tree prototype.
/// </summary>
[System.Serializable]
public class SeedInventoryEntry
{
    [Tooltip("Tree prototype index (type identifier).")]
    public int prototypeIndex;

    [Tooltip("How many seeds of this prototype are currently held.")]
    public int count;
}
