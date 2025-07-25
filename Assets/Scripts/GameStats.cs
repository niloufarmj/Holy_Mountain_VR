using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameStats : MonoBehaviour
{
    public TextMeshProUGUI seedsText, stonesText, treesText;
    public List<SeedInventoryEntry> seedInventory = new List<SeedInventoryEntry>();

    private int seedCount = 0, stoneCount = 0, treeCount = 0;

    public void AddSeed(int prototypeIndex)
    {
        var entry = seedInventory.Find(e => e.prototypeIndex == prototypeIndex);
        if (entry != null)
            entry.count++;
        else
            seedInventory.Add(new SeedInventoryEntry { prototypeIndex = prototypeIndex, count = 1 });
    }

    public bool HasSeed(int prototypeIndex) => seedInventory.Exists(e => e.prototypeIndex == prototypeIndex && e.count > 0);

    public void UseSeed(int prototypeIndex)
    {
        var entry = seedInventory.Find(e => e.prototypeIndex == prototypeIndex);
        if (entry != null && entry.count > 0)
            entry.count--;
    }

    public void AddStone()
    {
        stoneCount++;
        stonesText.text = $"Stones Collected: {stoneCount}";
    }

    public void AddTree()
    {
        treeCount++;
        treesText.text = $"Trees Planted: {treeCount}";
    }
}

[System.Serializable]
public class SeedInventoryEntry
{
    public int prototypeIndex;
    public int count;
}
