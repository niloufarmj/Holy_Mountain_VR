using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class SaveTerrainAfterPlay
{
    static SaveTerrainAfterPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            var manager = GameObject.FindObjectOfType<TreeDeathManager>();
            if (manager != null && manager.clonedData != null)
            {
                manager.terrain.terrainData.treeInstances = manager.clonedData.treeInstances;

                Debug.Log("🌲 terrain trees restored");
            }
        }
    }
}
