using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor utility: Create solid-color floor quads to visually separate the playable arena from the outside world
public static class CreateFloorVisuals
{
    private const string scenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Prototype/Create Arena Floor Visuals")]
    public static void CreateArenaFloorVisuals()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
        {
            if (scene.path != scenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        if (!scene.isLoaded)
        {
            Debug.LogError("Main scene is not loaded. Please create and save a scene at Assets/Scenes/Main.unity first.");
            return;
        }

        // Check if Floor parent already exists
        GameObject floorParent = GameObject.Find("Floor");
        if (floorParent == null)
        {
            floorParent = new GameObject("Floor");
            Undo.RegisterCreatedObjectUndo(floorParent, "Create Floor Parent");
            floorParent.transform.position = Vector3.zero;
        }
        else
        {
            Debug.Log("Floor parent already exists. Clearing old children...");
            foreach (Transform child in floorParent.transform)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        // Create Outside Floor (large, dark background)
        CreateFloorQuad(
            floorParent,
            "OutsideFloor",
            new Vector3(0, 0, 1f),
            new Vector3(60, 60, 1),
            new Color32(51, 51, 51, 255) // Dark gray #333333
        );

        // Create Arena Floor (medium, lighter color)
        CreateFloorQuad(
            floorParent,
            "ArenaFloor",
            new Vector3(0, 0, 0.5f),
            new Vector3(38, 26, 1),
            new Color32(100, 100, 100, 255) // Medium gray #646464
        );

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Arena floor visuals created successfully. Sizes: OutsideFloor 60x60 (z=1), ArenaFloor 38x26 (z=0.5)");
    }

    private static void CreateFloorQuad(GameObject parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        // Create a GameObject with a SpriteRenderer that displays a solid color
        GameObject quad = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(quad, $"Create {name}");

        quad.transform.SetParent(parent.transform);
        quad.transform.localPosition = position;
        quad.transform.localScale = scale;

        // Add SpriteRenderer
        SpriteRenderer sr = quad.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidColorSprite(color);
        sr.color = color;
        sr.sortingOrder = -10; // Behind obstacles and gameplay elements

        // Assign to correct layer (no collider needed, purely visual)
        quad.layer = LayerMask.NameToLayer("Default");

        Debug.Log($"Created {name} at position {position} with scale {scale}");
    }

    private static Sprite CreateSolidColorSprite(Color color)
    {
        // Create a 1x1 white texture that can be tinted via SpriteRenderer.color
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white); // White so it can be tinted to any color
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        return sprite;
    }
}
