using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlacePrototypeArenaObstacles
{
    private const string parentName = "Arena_Obstacles";

    [MenuItem("Tools/Prototype/Place Arena Obstacles")]
    public static void PlaceObstacles()
    {
        if (!EditorUtility.DisplayDialog("Place Arena Obstacles",
            "This will place a set of BasicObstacle instances into the Main scene (Assets/Scenes/Main.unity) if it exists, otherwise into the currently open scene. Existing Arena_Obstacles group will be replaced. Proceed?",
            "Place", "Cancel"))
            return;

        string mainScenePath = "Assets/Scenes/Main.unity";
        if (File.Exists(mainScenePath))
        {
            var scene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            Debug.Log("Opened scene: " + mainScenePath);
        }
        else
        {
            Debug.Log("Main.unity not found; using currently active scene.");
        }

        GameObject existing = GameObject.Find(parentName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Replace existing obstacles?",
                "An Arena_Obstacles group already exists in the scene. Do you want to replace it with a fresh placement?",
                "Replace", "Cancel"))
            {
                Debug.Log("Placement canceled by user.");
                return;
            }
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject parent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parent, "Create Arena Obstacles Parent");

        var prefabPaths = new Dictionary<string, string>
        {
            { "Small", "Assets/Prefabs/Environment/BasicObstacle_Small.prefab" },
            { "Medium", "Assets/Prefabs/Environment/BasicObstacle_Medium.prefab" },
            { "Large", "Assets/Prefabs/Environment/BasicObstacle_Large.prefab" }
        };

        // Expanded, more intricate placement across a larger arena roughly -20..+20 by -14..+14
        var placements = new List<(Vector2 pos, string size)>()
        {
            // Perimeter clusters (left and right)
            (new Vector2(-18f, 6f), "Medium"),
            (new Vector2(-18f, 2f), "Small"),
            (new Vector2(-18f, -2f), "Small"),
            (new Vector2(-18f, -6f), "Medium"),

            (new Vector2(18f, 6f), "Medium"),
            (new Vector2(18f, 2f), "Small"),
            (new Vector2(18f, -2f), "Small"),
            (new Vector2(18f, -6f), "Medium"),

            // Top perimeter (leave boss area near x=0,y=10 clear)
            (new Vector2(-12f, 12f), "Medium"),
            (new Vector2(-4f, 12f), "Small"),
            (new Vector2(4f, 12f), "Small"),
            (new Vector2(12f, 12f), "Medium"),

            // Bottom perimeter
            (new Vector2(-12f, -12f), "Medium"),
            (new Vector2(-4f, -12f), "Small"),
            (new Vector2(4f, -12f), "Small"),
            (new Vector2(12f, -12f), "Medium"),

            // Central open area with scattered cover
            (new Vector2(-6f, 2f), "Medium"),
            (new Vector2(-2f, 1f), "Small"),
            (new Vector2(2f, -1f), "Small"),
            (new Vector2(6f, -2f), "Medium"),

            // Corridor segments (pairs forming narrow passages ~2.5-3 units wide)
            (new Vector2(-10f, 4f), "Medium"),
            (new Vector2(-7f, 4f), "Small"),
            (new Vector2(-10f, 0f), "Medium"),
            (new Vector2(-7f, 0f), "Small"),

            (new Vector2(7f, 4f), "Small"),
            (new Vector2(10f, 4f), "Medium"),
            (new Vector2(7f, 0f), "Small"),
            (new Vector2(10f, 0f), "Medium"),

            // Inner maze-like but simple corridors
            (new Vector2(-3f, 6f), "Small"),
            (new Vector2(-1f, 6f), "Small"),
            (new Vector2(1f, 6f), "Small"),
            (new Vector2(3f, 6f), "Small"),

            (new Vector2(-3f, -6f), "Small"),
            (new Vector2(-1f, -6f), "Small"),
            (new Vector2(1f, -6f), "Small"),
            (new Vector2(3f, -6f), "Small"),

            // Flank obstacles
            (new Vector2(-14f, -3f), "Medium"),
            (new Vector2(14f, 3f), "Medium")
        };

        int envLayer = LayerMask.NameToLayer("Environment");

        foreach (var p in placements)
        {
            string selectedPrefabPath = prefabPaths.ContainsKey(p.size) ? prefabPaths[p.size] : null;
            if (string.IsNullOrEmpty(selectedPrefabPath))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(selectedPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Prefab missing", $"Could not find prefab at {selectedPrefabPath}. Please create the obstacle variants first.", "OK");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name + "_" + p.size;
            instance.transform.position = new Vector3(p.pos.x, p.pos.y, 0f);

            // ensure layer
            if (envLayer != -1)
                SetLayerRecursively(instance, envLayer);

            // ensure box collider matches sprite bounds (local size)
            var sr = instance.GetComponent<SpriteRenderer>();
            var col = instance.GetComponent<BoxCollider2D>();
            if (sr != null && sr.sprite != null && col != null)
            {
                Vector2 spriteSize = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                col.size = spriteSize; // local size; transform scale will apply
                col.offset = Vector2.zero;
                col.isTrigger = false;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place Arena Obstacle");
            instance.transform.SetParent(parent.transform, true);
        }

        // select the parent and mark scene dirty
        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Placement complete", "Placed obstacles and saved scene. Parent GameObject: " + parentName, "OK");
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform t in root.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
