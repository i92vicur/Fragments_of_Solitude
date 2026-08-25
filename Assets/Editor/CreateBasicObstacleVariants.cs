/*
CreateBasicObstacleVariants.cs

Editor utility: derive the Small, Medium and Large obstacle prefab variants from the base obstacle prefab,
applying the configured scale differences and saving each asset for placement tools.
*/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateBasicObstacleVariants
{
    [MenuItem("Tools/Prototype/Create Obstacle Variants (Small/Medium/Large)")]
    public static void CreateVariants()
    {
        string basePrefabPath = "Assets/Prefabs/Environment/BasicObstacle.prefab";
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("Base prefab missing", "Base prefab not found at Assets/Prefabs/Environment/BasicObstacle.prefab. Please create it first via Tools → Prototype → Create Basic Obstacle Prefab.", "OK");
            return;
        }

        var variants = new[]
        {
            new { Name = "BasicObstacle_Small.prefab", Scale = 0.5f },
            new { Name = "BasicObstacle_Medium.prefab", Scale = 1.0f },
            new { Name = "BasicObstacle_Large.prefab", Scale = 2.0f }
        };

        string folder = "Assets/Prefabs/Environment";
        int placed = 0;
        float x = 0f;
        foreach (var v in variants)
        {
            string path = Path.Combine(folder, v.Name).Replace("\\", "/");

            // Instantiate temp
            GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            temp.name = Path.GetFileNameWithoutExtension(v.Name);
            temp.transform.localScale = Vector3.one * v.Scale;

            var collider = temp.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                // Adjust collider size to match scaled sprite bounds if present
                var sr = temp.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    Vector2 spriteSize = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                    collider.size = spriteSize; // collider size will be affected by transform scale
                }
            }

            // Save as prefab (overwrite if exists)
            PrefabUtility.SaveAsPrefabAssetAndConnect(temp, path, InteractionMode.UserAction);

            // Place instance in scene for convenience
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            Vector3 spawnPos = Vector3.zero;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null)
                spawnPos = sv.pivot + new Vector3(x, 0f, 0f);
            else if (Camera.main != null)
                spawnPos = Camera.main.transform.position + new Vector3(x, 0f, 0f);
            spawnPos.z = 0f;
            instance.transform.position = spawnPos;
            Undo.RegisterCreatedObjectUndo(instance, "Create Obstacle Variant Instance");
            Selection.activeGameObject = instance;

            x += 1.5f * v.Scale; // offset next spawn
            placed++;

            // Cleanup temp
            Object.DestroyImmediate(temp);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Variants created", $"Created and placed {placed} obstacle variants in {folder}.", "OK");
    }
}