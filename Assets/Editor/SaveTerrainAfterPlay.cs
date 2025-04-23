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
                // کپی رو برگردون به اصلی
                manager.terrain.terrainData.treeInstances = manager.clonedData.treeInstances;

                // نشون بده تغییر اعمال شده
                Debug.Log("🌲 تغییرات مرگ درخت‌ها به terrain اصلی برگشت داده شد.");
            }
        }
    }
}
