/*
CreatePlatformerScene.cs  (Editor tool)

Purpose:
- One-click creation of the initial PlatformerLevel3 prototype scene.
- Creates a minimal, testable 2D platformer layout: ground + three stepped platforms + player placeholder.
- Saves a persistent white placeholder sprite asset so SpriteRenderer references survive scene reload.
- Appends the new scene to Build Settings without touching existing entries.

Run from: Prototype → Platformer → Create Platformer Level 3 Scene

Notes:
- The scene intentionally contains zero gameplay scripts — those are added in later steps.
- All geometry (Ground, Platform_A/B/C, Player) is explicitly temporary.
- SceneLoader is reused from Core because it has zero roguelite coupling.
*/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreatePlatformerScene
{
    private const string ScenePath  = "Assets/Scenes/Platformer/PlatformerLevel3.unity";
    private const string SpritePath = "Assets/Art/Platformer/Placeholder_White.png";

    [MenuItem("Prototype/Platformer/Create Platformer Level 3 Scene")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogWarning("[CreatePlatformerScene] Scene already exists at " + ScenePath);
            return;
        }

        // Ensure required folders exist via AssetDatabase (not just OS mkdir)
        EnsureFolder("Assets/Scenes/Platformer");
        EnsureFolder("Assets/Art/Platformer");

        // Create and save a white placeholder sprite so references persist after scene reload
        Sprite whiteSprite = EnsurePlaceholderSprite();
        if (whiteSprite == null)
        {
            Debug.LogError("[CreatePlatformerScene] Failed to create placeholder sprite. Aborting.");
            return;
        }

        // Create an empty scene (no default camera or light — we add exactly what we need)
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── Main Camera ──────────────────────────────────────────────────────────────
        // Orthographic, taller framing than the roguelite arena to show vertical jump space.
        // Camera starts centred at Y=4 so the ground and lowest platforms are both visible.
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 6f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.13f, 0.18f, 0.28f, 1f); // dark blue-grey sky
        cameraGO.transform.position = new Vector3(0f, 4f, -10f);
        cameraGO.AddComponent<AudioListener>();

        // ── SceneLoader ──────────────────────────────────────────────────────────────
        // Reused from Assets/Scripts/Core — zero roguelite logic, purely scene-name config.
        var slGO = new GameObject("SceneLoader");
        var sl   = slGO.AddComponent<SceneLoader>();
        var slSO = new SerializedObject(sl);
        slSO.FindProperty("gameplaySceneName").stringValue = "PlatformerLevel3";
        slSO.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
        slSO.ApplyModifiedProperties();

        // ── Ground ───────────────────────────────────────────────────────────────────
        // 20 units wide, 1 unit tall. Top surface sits at Y=0 so player spawn at Y=0.5 lands flush.
        // TEMPORARY — replace with real tilemap/terrain in the final map.
        CreatePlatform("Ground", whiteSprite,
            pos:   new Vector3(0f, -0.5f, 0f),
            scale: new Vector3(20f, 1f, 1f),
            col:   new Color(0.42f, 0.42f, 0.42f));

        // ── Platforms ────────────────────────────────────────────────────────────────
        // Three platforms stepped over ~10 vertical units to stress-test camera-follow.
        // Heights chosen so each platform is reachable from the one below with a normal jump.
        // TEMPORARY — all three will be replaced by the real level geometry.
        CreatePlatform("Platform_A", whiteSprite,
            pos:   new Vector3(-2.5f, 2.25f, 0f),
            scale: new Vector3(4f, 0.5f, 1f),
            col:   new Color(0.32f, 0.50f, 0.32f));

        CreatePlatform("Platform_B", whiteSprite,
            pos:   new Vector3(3f, 5.25f, 0f),
            scale: new Vector3(4f, 0.5f, 1f),
            col:   new Color(0.32f, 0.50f, 0.32f));

        CreatePlatform("Platform_C", whiteSprite,
            pos:   new Vector3(-1f, 9f, 0f),
            scale: new Vector3(4f, 0.5f, 1f),
            col:   new Color(0.32f, 0.50f, 0.32f));

        // ── Player placeholder ────────────────────────────────────────────────────────
        // Bright blue quad — unmistakably a character, not a platform.
        // KEY PLATFORMER DIFFERENCE: gravityScale = 2.5 (roguelite uses gravityScale = 0).
        // Rigidbody2D + BoxCollider2D only — no gameplay script yet, added in the next step.
        // TEMPORARY — will be replaced by the real PlatformerPlayerController + sprite.
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position   = new Vector3(0f, 0.5f, 0f);
        playerGO.transform.localScale = new Vector3(0.8f, 1f, 1f);

        var sr    = playerGO.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color  = new Color(0.25f, 0.65f, 1f); // bright blue

        // BoxCollider2D default size (1,1) × localScale (0.8,1,1) = world footprint (0.8 × 1 units)
        playerGO.AddComponent<BoxCollider2D>();

        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 2.5f;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // ── Save ─────────────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings();

        Debug.Log("[CreatePlatformerScene] Scene created: " + ScenePath);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    private static void CreatePlatform(string name, Sprite sprite, Vector3 pos, Vector3 scale, Color col)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = col;

        // Static collider — no Rigidbody2D means Unity treats it as a static body.
        // BoxCollider2D size (1,1) × localScale = correct world size automatically.
        go.AddComponent<BoxCollider2D>();
    }

    /// <summary>
    /// Creates and saves a 100×100 white PNG as a Sprite asset at SpritePath.
    /// Saved as a proper asset so SpriteRenderer references survive scene reload.
    /// Returns the loaded Sprite, or null if creation fails.
    /// </summary>
    private static Sprite EnsurePlaceholderSprite()
    {
        var cached = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (cached != null) return cached;

        // Build white texture
        var tex    = new Texture2D(100, 100, TextureFormat.RGBA32, false);
        var pixels = new Color[100 * 100];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        // Write PNG to disk and import
        string fullPath = Application.dataPath + "/" + SpritePath.Substring("Assets/".Length);
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(SpritePath);

        // Configure import: Sprite type, 100 PPU (1 sprite = 1 world unit), point filter for crisp edges
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.spritePivot         = new Vector2(0.5f, 0.5f);
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode          = FilterMode.Point;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts   = path.Split('/');
        string   current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void AddToBuildSettings()
    {
        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing)
            if (s.path == ScenePath) return;

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        for (int i = 0; i < existing.Length; i++)
            updated[i] = existing[i];
        updated[existing.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
