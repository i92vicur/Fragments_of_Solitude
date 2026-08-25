/*
PrototypePhysicsLayerSetup.cs

Editor utility: add the prototype layers, configure the 2D collision matrix, and infer root layers and
projectile target masks for matching prefabs and scene objects. It changes project settings and assets, so
the confirmation dialog should be treated as a deliberate migration step.
*/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypePhysicsLayerSetup
{
    private static readonly string[] RequiredLayers = new[]
    {
        "Player",
        "Enemy",
        "PlayerProjectile",
        "Environment",
        "Pickup"
    };

    [MenuItem("Tools/Prototype/Setup Layers and Physics")]
    public static void SetupLayersAndPhysics()
    {
        if (!EditorUtility.DisplayDialog("Prototype Layer & Physics Setup",
            "This tool will add layers and adjust the 2D physics collision matrix. It will also attempt to set prefab root layers based on common components and update PlayerAutoAttack targetLayerMask on matching prefabs and scene objects. Make a backup if you are unsure.",
            "Proceed", "Cancel"))
        {
            return;
        }

        AddLayers();
        AssetDatabase.Refresh();

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int projectileLayer = LayerMask.NameToLayer("PlayerProjectile");
        int environmentLayer = LayerMask.NameToLayer("Environment");
        int pickupLayer = LayerMask.NameToLayer("Pickup");

        if (playerLayer == -1 || enemyLayer == -1 || projectileLayer == -1 || environmentLayer == -1)
        {
            Debug.LogError("One or more required layers were not created. Aborting physics setup.");
            return;
        }

        ConfigurePhysicsMatrix(playerLayer, enemyLayer, projectileLayer, environmentLayer, pickupLayer);
        ProcessPrefabsAndScenes(enemyLayer, playerLayer, projectileLayer, pickupLayer);

        Physics2D.queriesHitTriggers = true;

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", "Prototype layers, prefab/scene assignments and 2D physics matrix configured. Please verify in Project Settings -> Physics 2D and inspect Player/Enemy prefabs in the Inspector.", "OK");
        Debug.Log("Prototype layer & physics setup complete.");
    }

    private static void AddLayers()
    {
        UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < RequiredLayers.Length; i++)
        {
            string layerName = RequiredLayers[i];
            if (LayerMask.NameToLayer(layerName) != -1)
            {
                Debug.Log($"Layer '{layerName}' already exists.");
                continue;
            }

            bool assigned = false;
            for (int j = 8; j < layersProp.arraySize; j++) // user layers start at 8
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(j);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    assigned = true;
                    Debug.Log($"Assigned layer '{layerName}' to slot {j}.");
                    break;
                }
            }

            if (!assigned)
            {
                Debug.LogWarning($"No free user layer slot found for '{layerName}'. Please add it manually in the Tags & Layers settings.");
            }

            tagManager.ApplyModifiedProperties();
        }
    }

    private static void ConfigurePhysicsMatrix(int playerLayer, int enemyLayer, int projectileLayer, int environmentLayer, int pickupLayer)
    {
        // First, enable pairs we want (ensure they are NOT ignored)
        SetCollision(playerLayer, environmentLayer, true);
        SetCollision(enemyLayer, environmentLayer, true);
        SetCollision(playerLayer, enemyLayer, true);
        SetCollision(projectileLayer, enemyLayer, true);
        SetCollision(projectileLayer, environmentLayer, true);
        SetCollision(pickupLayer, playerLayer, true);

        // Disable common unnecessary collisions
        SetCollision(projectileLayer, playerLayer, false); // projectiles shouldn't hit player
        SetCollision(projectileLayer, pickupLayer, false);
        SetCollision(pickupLayer, environmentLayer, false);
        SetCollision(enemyLayer, enemyLayer, false); // optional: enemies don't collide with each other
        SetCollision(playerLayer, playerLayer, false); // optional

        Debug.Log("Configured Physics2D collision pairs (use Project Settings -> Physics 2D to review visually).");
    }

    private static void SetCollision(int a, int b, bool enabled)
    {
        // Physics2D.IgnoreLayerCollision takes "ignore" parameter, so invert enabled
        if (a < 0 || b < 0)
            return;
        Physics2D.IgnoreLayerCollision(a, b, !enabled);
    }

    private static void ProcessPrefabsAndScenes(int enemyLayer, int playerLayer, int projectileLayer, int pickupLayer)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int processed = 0;

        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null)
                continue;

            bool changed = false;

            // Determine appropriate layer by tag and common component names
            int targetLayer = -1;
            if (prefabRoot.tag == "Player")
                targetLayer = playerLayer;
            else if (prefabRoot.tag == "Enemy")
                targetLayer = enemyLayer;
            else
            {
                // component-name heuristics (use name-based lookup to avoid Type.GetType null issues)
                if (FindComponentByName(prefabRoot, "PlayerAutoAttack") != null || FindComponentByName(prefabRoot, "PlayerMovement") != null)
                    targetLayer = playerLayer;
                else if (FindComponentByName(prefabRoot, "EnemyMovement") != null || FindComponentByName(prefabRoot, "EnemyDeath") != null)
                    targetLayer = enemyLayer;
                else if (FindComponentByName(prefabRoot, "PlayerProjectile") != null || prefabRoot.name.ToLower().Contains("projectile"))
                    targetLayer = projectileLayer;
                else if (FindComponentByName(prefabRoot, "ExperiencePickup") != null || prefabRoot.name.ToLower().Contains("pickup"))
                    targetLayer = pickupLayer;
            }

            if (targetLayer != -1)
            {
                if (prefabRoot.layer != targetLayer)
                {
                    SetLayerRecursively(prefabRoot, targetLayer);
                    changed = true;
                    Debug.Log($"Set prefab layer for {path} to {LayerMask.LayerToName(targetLayer)}");
                }
            }

            // Update PlayerAutoAttack targetLayerMask on prefab if component exists
            Component paa = FindComponentByName(prefabRoot, "PlayerAutoAttack");
            if (paa != null)
            {
                SerializedObject compSO = new SerializedObject(paa);
                SerializedProperty maskProp = compSO.FindProperty("targetLayerMask");
                if (maskProp != null)
                {
                    int maskValue = (1 << enemyLayer);
                    if (maskProp.intValue != maskValue)
                    {
                        maskProp.intValue = maskValue;
                        compSO.ApplyModifiedProperties();
                        changed = true;
                        Debug.Log($"Updated PlayerAutoAttack.targetLayerMask on prefab {path} to Enemy layer.");
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                processed++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log($"Processed and updated {processed} prefabs.");

        // Now process open scenes' root objects
        int totalSceneChanges = 0;
        int sceneCount = EditorSceneManager.sceneCount;
        for (int si = 0; si < sceneCount; si++)
        {
            var scene = EditorSceneManager.GetSceneAt(si);
            if (!scene.isLoaded)
                continue;

            var roots = scene.GetRootGameObjects();
            int localChanges = 0;
            foreach (var root in roots)
            {
                // Player GameObject in scene
                if (root.CompareTag("Player") || FindComponentByName(root, "PlayerAutoAttack") != null || FindComponentByName(root, "PlayerMovement") != null)
                {
                    if (root.layer != playerLayer)
                    {
                        SetLayerRecursively(root, playerLayer);
                        localChanges++;
                        Debug.Log($"Set scene object '{root.name}' layer to Player.");
                    }
                }

                // Enemy objects
                if (root.CompareTag("Enemy") || FindComponentByName(root, "EnemyMovement") != null)
                {
                    if (root.layer != enemyLayer)
                    {
                        SetLayerRecursively(root, enemyLayer);
                        localChanges++;
                        Debug.Log($"Set scene object '{root.name}' layer to Enemy.");
                    }
                }

                // PlayerAutoAttack on scene objects - update mask
                var comps = root.GetComponentsInChildren<Component>(true);
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    if (c.GetType().Name == "PlayerAutoAttack")
                    {
                        SerializedObject compSO = new SerializedObject(c);
                        SerializedProperty maskProp = compSO.FindProperty("targetLayerMask");
                        if (maskProp != null)
                        {
                            int maskValue = (1 << enemyLayer);
                            if (maskProp.intValue != maskValue)
                            {
                                maskProp.intValue = maskValue;
                                compSO.ApplyModifiedProperties();
                                localChanges++;
                                Debug.Log($"Updated PlayerAutoAttack.targetLayerMask on scene object '{root.name}' to Enemy layer.");
                            }
                        }
                    }
                }
            }

            if (localChanges > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                totalSceneChanges += localChanges;
            }
        }

        if (totalSceneChanges > 0)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"Updated {totalSceneChanges} scene objects.");
        }
    }

    private static Component FindComponentByName(GameObject go, string compName)
    {
        var comps = go.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            if (c.GetType().Name == compName)
                return c;
        }
        return null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform t in root.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
