/*
MakeEnvironmentIndestructible.cs

Editor utility: scan scene/prefab environment objects and remove Health components so decorative geometry
cannot be damaged. This is a destructive normalization tool; review the confirmation dialog before running.
*/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MakeEnvironmentIndestructible
{
    [MenuItem("Tools/Prototype/Make Environment Indestructible (remove Health components)")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Remove Health from Environment",
            "This will search prefabs and scene objects on the 'Environment' layer and remove any Health components found. This cannot be undone easily — please ensure you have a backup or use source control. Proceed?",
            "Proceed", "Cancel"))
            return;

        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer == -1)
        {
            EditorUtility.DisplayDialog("Layer missing", "Environment layer not found. Please create the 'Environment' layer first.", "OK");
            return;
        }

        Type healthType = typeof(Health);
        if (healthType == null)
        {
            Debug.LogError("Health type not found in project. Aborting.");
            return;
        }

        int prefabCount = 0;
        int removedFromPrefabs = 0;

        // Prefabs
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool prefabChanged = false;
            Component[] comps = root.GetComponentsInChildren(healthType, true);
            if (comps != null && comps.Length > 0)
            {
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    // Only remove if the component's gameObject is on Environment layer
                    if (c.gameObject.layer == envLayer)
                    {
                        UnityEngine.Object.DestroyImmediate(c, true);
                        removedFromPrefabs++;
                        prefabChanged = true;
                    }
                }
            }

            if (prefabChanged)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabCount++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        int sceneRemoved = 0;
        int sceneObjectsProcessed = 0;
        int sceneCount = EditorSceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                Component[] comps = r.GetComponentsInChildren(healthType, true);
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    if (c.gameObject.layer == envLayer)
                    {
                        UnityEngine.Object.DestroyImmediate(c, true);
                        sceneRemoved++;
                    }
                }
                sceneObjectsProcessed++;
            }
            if (sceneRemoved > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", $"Removed {removedFromPrefabs} Health components from prefabs and {sceneRemoved} from scene objects.", "OK");
        Debug.Log($"MakeEnvironmentIndestructible: Removed {removedFromPrefabs} prefab Health comps, {sceneRemoved} scene Health comps.");
    }
}
