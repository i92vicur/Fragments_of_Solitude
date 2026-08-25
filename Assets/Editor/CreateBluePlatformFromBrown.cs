/*
CreateBluePlatformFromBrown.cs

Editor utility: create the blue placeholder platform sprite/prefab used by the platformer prototype from the
existing brown platform asset, preserving the project asset workflow through AssetDatabase import.
*/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateBluePlatformFromBrown
{
    private const string SourcePrefabPath = "Assets/Prefabs/Level3/Environment/BasicObstacle.prefab";
    private const string SpritePath = "Assets/Art/Arena/Level4/blue_platform_square.png";
    private const string PrefabPath = "Assets/Prefabs/Level4/BluePlatformFromBrown.prefab";
    private static readonly Color Blue = new Color(73f / 255f, 150f / 255f, 255f / 255f, 1f);

    [MenuItem("Tools/Level4/Create Blue Platform From Brown")]
    public static void Create()
    {
        EnsureBlueSprite();
        var blueSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (blueSprite == null)
        {
            Debug.LogError("[BluePlatform] Generated sprite is missing.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);
        var prefabRoot = new GameObject("BluePlatformFromBrown");
        prefabRoot.layer = 10;
        prefabRoot.transform.localScale = Vector3.one;

        var renderer = prefabRoot.AddComponent<SpriteRenderer>();
        renderer.sprite = blueSprite;
        renderer.sharedMaterial = null;
        renderer.color = Color.white;

        var collider = prefabRoot.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.32f, 0.32f);
        collider.offset = Vector2.zero;
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        ReplacePlatformPrefabSprites(blueSprite);
        ReplaceScenePlatformSprites(blueSprite);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BluePlatform] Created " + PrefabPath + " and linked the blue square sprite in Level4.");
    }

    [MenuItem("Tools/Level4/Restore Original Platforms")]
    public static void RestoreOriginalPlatforms()
    {
        EnsureBlueSprite();
        var blueSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level4/Level4.unity", OpenSceneMode.Single);
        var damagedPlatforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var transform in damagedPlatforms)
        {
            if (transform.name.StartsWith("BluePlatformFromBrown"))
                Object.DestroyImmediate(transform.gameObject);
        }

        var names = new[] { "Platform_A", "Platform_B", "Platform_C", "Platform_D", "Platform_E", "Platform_F" };
        var positions = new[]
        {
            new Vector3(-5f, 1.8f, 0f),
            new Vector3(-0.5f, 4f, 0f),
            new Vector3(3.5f, 6.6f, 0f),
            new Vector3(0.75f, 8.8f, 0f),
            new Vector3(-3.75f, 11.4f, 0f),
            new Vector3(2.5f, 14.1f, 0f)
        };
        var widths = new[] { 2f, 4f, 7f, 2f, 4f, 2f };
        foreach (var existing in names)
        {
            var old = GameObject.Find(existing);
            if (old != null) Object.DestroyImmediate(old);
        }
        for (var i = 0; i < names.Length; i++)
        {
            var platform = new GameObject(names[i]);
            platform.layer = 10;
            platform.transform.position = positions[i];
            platform.transform.localScale = new Vector3(widths[i], 0.4f, 1f);
            var renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = blueSprite;
            renderer.color = Color.white;
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = 0;
            renderer.size = Vector2.one;
            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.offset = Vector2.zero;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BluePlatform] Restored Platform_A-F as six normal GameObjects at original positions.");
    }

    private static void ReplacePlatformPrefabSprites(Sprite blueSprite)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Level4" });
        foreach (var prefabGuid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            if (path.EndsWith("PlatformerPlayer.prefab")) continue;

            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            var renderer = prefabRoot.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = blueSprite;
                renderer.color = Color.white;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void EnsureBlueSprite()
    {
        if (!File.Exists(SpritePath))
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Blue;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
        }

        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void ReplaceScenePlatformSprites(Sprite blueSprite)
    {
        var scenePath = "Assets/Scenes/Level4/Level4.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var platformNames = new[] { "Platform_A", "Platform_B", "Platform_C", "Platform_D", "Platform_E", "Platform_F" };
        foreach (var platformName in platformNames)
        {
            var existing = GameObject.Find(platformName);
            if (existing == null || prefab == null) continue;

            var parent = existing.transform.parent;
            var position = existing.transform.position;
            var rotation = existing.transform.rotation;
            var scale = existing.transform.localScale;
            var layer = existing.layer;
            var active = existing.activeSelf;

            Object.DestroyImmediate(existing);
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            replacement.name = platformName;
            replacement.transform.SetParent(parent);
            replacement.transform.position = position;
            replacement.transform.rotation = rotation;
            replacement.transform.localScale = scale;
            replacement.layer = layer;
            replacement.SetActive(active);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
