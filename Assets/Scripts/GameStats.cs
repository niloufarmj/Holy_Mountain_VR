using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameStats : MonoBehaviour
{
    public TextMeshProUGUI seedsText, stonesText, treesText;

    private int seedCount = 0, stoneCount = 0, treeCount = 0;

    public void AddSeed()
    {
        seedCount++;
        seedsText.text = $"Seeds Collected: {seedCount}";
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
