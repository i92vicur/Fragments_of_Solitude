using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MigrateLevel3MapStructures
{
    private static readonly Vector2[] RegionMins =
    {
        new Vector2(-23.5f, 8.5f), new Vector2(11.5f, 8.5f), new Vector2(3.5f, 8.5f),
        new Vector2(-15.8f, 17.8f), new Vector2(11.5f, 17.8f), new Vector2(-17.5f, 14f),
        new Vector2(-23.5f, 0.2f), new Vector2(-21.5f, -4.5f), new Vector2(-17.1f, -4.5f),
        new Vector2(-18.8f, -9.4f), new Vector2(-18.8f, -18.3f), new Vector2(5.8f, -9.5f),
        new Vector2(13.8f, -9.5f), new Vector2(13.8f, -18.3f), new Vector2(-6.5f, -9.5f),
        new Vector2(-1.5f, -14f), new Vector2(4.5f, -14.5f), new Vector2(-10.5f, -13.8f),
        new Vector2(-19.5f, -13.8f), new Vector2(9.5f, -13.8f)
    };

    private static readonly Vector2[] RegionMaxs =
    {
        new Vector2(-9f, 16.5f), new Vector2(20f, 16.5f), new Vector2(7f, 16.5f),
        new Vector2(-6.5f, 19.5f), new Vector2(20.5f, 19.5f), new Vector2(-8.5f, 15.7f),
        new Vector2(-8.2f, 1.8f), new Vector2(-18.3f, -1.4f), new Vector2(-9.3f, -2.8f),
        new Vector2(-17.1f, -5.7f), new Vector2(-5.5f, -16.5f), new Vector2(13.3f, -7.8f),
        new Vector2(15.5f, -5.7f), new Vector2(25f, -16.5f), new Vector2(0.5f, -7.8f),
        new Vector2(0.2f, -10.5f), new Vector2(6.2f, -10.5f), new Vector2(-8.8f, -10.8f),
        new Vector2(-11.8f, -12.1f), new Vector2(17.5f, -12.1f)
    };

    [MenuItem("Tools/Level3/Migrate Map Structures To Scene")]
    public static void Migrate()
    {
        const string scenePath = "Assets/Scenes/Level3.unity";
        const string prefabPath = "Assets/Prefabs/Level3/MapStructure.prefab";

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("MapStructure prefab was not found at " + prefabPath);
            return;
        }

        GameObject existing = GameObject.Find("MapStructures");
        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject root = new GameObject("MapStructures");
        Undo.RegisterCreatedObjectUndo(root, "Create MapStructures");

        MapStructure source = prefab.GetComponent<MapStructure>();
        BoxCollider2D sourceCollider = source != null ? prefab.GetComponent<BoxCollider2D>() : null;
        if (sourceCollider == null)
        {
            Debug.LogError("MapStructure prefab must have a root BoxCollider2D.");
            Object.DestroyImmediate(root);
            return;
        }

        for (int i = 0; i < RegionMins.Length; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = $"Structure_{i + 1:00}";
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = (RegionMins[i] + RegionMaxs[i]) * 0.5f;
            instance.transform.localScale = new Vector3(
                (RegionMaxs[i].x - RegionMins[i].x) / sourceCollider.size.x,
                (RegionMaxs[i].y - RegionMins[i].y) / sourceCollider.size.y,
                1f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Migrated " + RegionMins.Length + " MapStructure prefab instances into " + scenePath);
    }
}
