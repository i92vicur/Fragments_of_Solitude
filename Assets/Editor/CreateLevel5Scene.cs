/*
CreateLevel5Scene.cs  (Editor tool)

Purpose:
- One-click creation of the initial Level5 prototype scene: a single turn-based battle arena.
- Placeholder squares only: one large enemy on the left, four smaller characters on the right.
- Canvas with empty placeholder areas for enemy info, character info, combat messages and actions.

Run from: Prototype -> Level5 -> Create Level5 Scene

Notes:
- No combat, turn, health, AI, inventory or win/lose logic. Visual layout only.
- Buttons are intentionally unwired.
- Uses the same conventions as CreatePlatformerScene / CreateMainMenuScene:
  empty scene, orthographic camera, persistent white placeholder sprite asset,
  ScreenSpaceOverlay canvas at 1920x1080, SceneLoader + EventSystem, Build Settings append.
*/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CreateLevel5Scene
{
    private const string ScenePath  = "Assets/Scenes/Level5/Level5.unity";
    private const string SpritePath = "Assets/Art/Level5/Placeholder_White.png";
    private const string FontPath   = "Assets/Fonts/PressStart2P-Regular.ttf";

    [MenuItem("Prototype/Level5/Create Level5 Scene")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogWarning("[CreateLevel5Scene] Scene already exists at " + ScenePath);
            return;
        }

        EnsureFolder("Assets/Scenes/Level5");
        EnsureFolder("Assets/Art/Level5");

        Sprite whiteSprite = EnsurePlaceholderSprite();
        if (whiteSprite == null)
        {
            Debug.LogError("[CreateLevel5Scene] Failed to create placeholder sprite. Aborting.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Main Camera ──────────────────────────────────────────────────────────────
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.05f, 0.05f, 0.08f, 1f);
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.AddComponent<AudioListener>();

        // ── SceneLoader (navigation only, no buttons wired yet) ─────────────────────
        var sceneLoaderGO = new GameObject("SceneLoader");
        sceneLoaderGO.AddComponent<SceneLoader>();

        // ── Battle arena ─────────────────────────────────────────────────────────────
        var arenaGO = new GameObject("BattleArena");

        // Background: wide dark plate so the placeholder squares read clearly.
        CreateQuad("Background", arenaGO.transform, whiteSprite,
            pos:   new Vector3(0f, 1f, 0f),
            scale: new Vector3(18f, 8f, 1f),
            col:   new Color(0.16f, 0.18f, 0.24f, 1f),
            sortingOrder: -10);

        // Enemy: one large square on the LEFT.
        CreateQuad("Enemy_Placeholder", arenaGO.transform, whiteSprite,
            pos:   new Vector3(-5f, 1.5f, 0f),
            scale: new Vector3(4f, 4f, 1f),
            col:   new Color(0.72f, 0.18f, 0.18f, 1f),
            sortingOrder: 0);

        // Party: four smaller squares on the RIGHT in a 2x2 formation.
        var partyGO = new GameObject("Party");
        partyGO.transform.SetParent(arenaGO.transform, false);

        Vector2[] partyPositions =
        {
            new Vector2(3.6f, 2.6f),
            new Vector2(5.9f, 2.6f),
            new Vector2(3.6f, 0.4f),
            new Vector2(5.9f, 0.4f)
        };

        Color[] partyColors =
        {
            new Color(0.25f, 0.55f, 0.95f, 1f),
            new Color(0.30f, 0.75f, 0.45f, 1f),
            new Color(0.90f, 0.75f, 0.25f, 1f),
            new Color(0.70f, 0.40f, 0.85f, 1f)
        };

        for (int i = 0; i < partyPositions.Length; i++)
        {
            CreateQuad($"Character_{i + 1}_Placeholder", partyGO.transform, whiteSprite,
                pos:   new Vector3(partyPositions[i].x, partyPositions[i].y, 0f),
                scale: new Vector3(1.6f, 1.6f, 1f),
                col:   partyColors[i],
                sortingOrder: 0);
        }

        // ── EventSystem ──────────────────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Canvas ───────────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("BattleCanvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        Font font = LoadUiFont();

        // Enemy info: top-left, above the enemy square.
        var enemyPanel = CreatePanel("EnemyInfo_Panel", canvasGO.transform,
            anchor: new Vector2(0f, 1f),
            anchoredPos: new Vector2(40f, -40f),
            size: new Vector2(520f, 140f));
        CreateLabel("EnemyInfo_Placeholder", enemyPanel, "ENEMY INFO\n(placeholder)", font, 18);

        // Character info: bottom-right, four empty slots for the party.
        var partyPanel = CreatePanel("CharacterInfo_Panel", canvasGO.transform,
            anchor: new Vector2(1f, 0f),
            anchoredPos: new Vector2(-40f, 40f),
            size: new Vector2(620f, 300f));
        for (int i = 0; i < 4; i++)
        {
            var slot = CreatePanel($"CharacterSlot_{i + 1}", partyPanel.transform,
                anchor: new Vector2(0.5f, 1f),
                anchoredPos: new Vector2(0f, -30f - (i * 68f)),
                size: new Vector2(580f, 60f));
            var slotImage = slot.GetComponent<Image>();
            slotImage.color = new Color(0.15f, 0.15f, 0.20f, 0.85f);
            slotImage.preserveAspect = true;
            CreateLabel($"CharacterSlot_{i + 1}_Placeholder", slot, $"CHARACTER {i + 1} (placeholder)", font, 16);
        }

        // Combat messages: bottom-left banner.
        var messagePanel = CreatePanel("CombatMessage_Panel", canvasGO.transform,
            anchor: new Vector2(0f, 0f),
            anchoredPos: new Vector2(40f, 260f),
            size: new Vector2(1180f, 120f));
        CreateLabel("CombatMessage_Placeholder", messagePanel, "COMBAT MESSAGES (placeholder)", font, 18);

        // Action buttons: bottom-left, under the message banner. Not wired on purpose.
        var actionPanel = CreatePanel("ActionButtons_Panel", canvasGO.transform,
            anchor: new Vector2(0f, 0f),
            anchoredPos: new Vector2(40f, 40f),
            size: new Vector2(1180f, 200f));

        string[] actionLabels = { "ATTACK", "MAGIC", "ITEM", "DEFEND" };
        for (int i = 0; i < actionLabels.Length; i++)
        {
            CreatePlaceholderButton(actionLabels[i], actionPanel.transform, font,
                new Vector2(30f + (i * 285f), -30f));
        }

        // ── Save ─────────────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log("[CreateLevel5Scene] Scene created: " + ScenePath);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    private static void CreateQuad(string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, Color col, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.color        = col;
        sr.sortingOrder = sortingOrder;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin       = anchor;
        rect.anchorMax       = anchor;
        rect.pivot           = anchor;
        rect.sizeDelta       = size;
        rect.anchoredPosition = anchoredPos;

        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        return go;
    }

    private static void CreateLabel(string name, GameObject parent, string text, Font font, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 8f);
        rect.offsetMax = new Vector2(-12f, -8f);

        var label = go.GetComponent<Text>();
        label.font          = font;
        label.fontSize      = fontSize;
        label.alignment     = TextAnchor.MiddleLeft;
        label.color         = Color.white;
        label.text          = text;
        label.raycastTarget = false;
    }

    private static void CreatePlaceholderButton(string label, Transform parent, Font font, Vector2 anchoredPos)
    {
        var go = new GameObject($"{label}_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 1f);
        rect.anchorMax        = new Vector2(0f, 1f);
        rect.pivot            = new Vector2(0f, 1f);
        rect.sizeDelta        = new Vector2(260f, 70f);
        rect.anchoredPosition = anchoredPos;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.24f, 1f);
        go.GetComponent<Button>().targetGraphic = image;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(go.transform, false);

        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGO.GetComponent<Text>();
        labelText.font          = font;
        labelText.fontSize      = 16;
        labelText.alignment     = TextAnchor.MiddleCenter;
        labelText.color         = Color.white;
        labelText.text          = label;
        labelText.raycastTarget = false;
    }

    private static Font LoadUiFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        var cached = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (cached != null) return cached;

        var tex    = new Texture2D(100, 100, TextureFormat.RGBA32, false);
        var pixels = new Color[100 * 100];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        string fullPath = Application.dataPath + "/" + SpritePath.Substring("Assets/".Length);
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(SpritePath);

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
