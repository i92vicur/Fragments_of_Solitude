using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateBasicObstaclePrefab
{
    [MenuItem("Tools/Prototype/Create Basic Obstacle Prefab")]
    public static void CreatePrefab()
    {
        if (!EditorUtility.DisplayDialog("Create Basic Obstacle Prefab",
            "This will create a placeholder obstacle texture and a prefab at Assets/Prefabs/Environment/BasicObstacle.prefab. Proceed?",
            "Create", "Cancel"))
            return;

        // Ensure folders
        string artFolder = "Assets/Art";
        string prefabFolder = "Assets/Prefabs/Environment";
        if (!AssetDatabase.IsValidFolder(artFolder))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Environment");

        // Create a small placeholder texture
        string pngPath = Path.Combine(artFolder, "basic_obstacle.png");
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color fill = new Color(0.6f, 0.4f, 0.3f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, fill);
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(pngPath, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

        // Configure importer as Sprite
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

        // Create GameObject
        GameObject go = new GameObject("BasicObstacle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingOrder = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        // Assign to Environment layer if exists
        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer != -1)
            go.layer = envLayer;

        // Save as prefab
        string prefabPathFull = Path.Combine(prefabFolder, "BasicObstacle.prefab");
        if (File.Exists(prefabPathFull))
        {
            if (!EditorUtility.DisplayDialog("Overwrite Prefab?", "BasicObstacle.prefab already exists. Overwrite?", "Yes", "No"))
            {
                Object.DestroyImmediate(go);
                return;
            }
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPathFull, InteractionMode.UserAction);

        // Instantiate a scene instance at the Scene view pivot for convenience
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathFull);
        if (prefabAsset != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            Vector3 spawnPos = Vector3.zero;
            var sv = UnityEditor.SceneView.lastActiveSceneView;
            if (sv != null)
            {
                spawnPos = sv.pivot;
                spawnPos.z = 0f; // keep in 2D plane
            }
            else if (Camera.main != null)
            {
                spawnPos = Camera.main.transform.position;
                spawnPos.z = 0f;
            }

            instance.transform.position = spawnPos;
            Undo.RegisterCreatedObjectUndo(instance, "Create BasicObstacle Instance");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Basic Obstacle Created", $"Prefab saved to {prefabPathFull} and an instance was placed in the open scene. You can resize instances by changing the SpriteRenderer's drawMode or scale the transform.", "OK");
    }
}