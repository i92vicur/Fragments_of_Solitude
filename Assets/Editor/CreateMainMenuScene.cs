/*
CreateMainMenuScene.cs

Editor utility: scaffold the MainMenu scene, including camera, Canvas, EventSystem and navigation buttons.
The generated UI is prototype wiring intended to be refined manually afterward.
*/

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Editor utility: Create a minimal MainMenu scene with title and Play button
public static class CreateMainMenuScene
{
    private const string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string mainGameScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Prototype/Create MainMenu Scene")]
    public static void CreateMenu()
    {
        // Check if MainMenu already exists
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuScenePath) != null)
        {
            Debug.LogWarning("MainMenu scene already exists at " + mainMenuScenePath);
            return;
        }

        // Create a new scene
        Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(mainMenuScene, mainMenuScenePath);

        // Now open it and set it up
        mainMenuScene = EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);

        // Find the Main Camera (created by default)
        Camera mainCamera = Object.FindAnyObjectByType<Camera>();
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.19f, 0.30f, 0.47f, 1f);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5;
            mainCamera.gameObject.name = "Main Camera";
        }

        // Ensure EventSystem exists with an input module so button clicks are processed
        var existingES = Object.FindAnyObjectByType<EventSystem>();
        if (existingES == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
        else if (existingES.GetComponent<StandaloneInputModule>() == null)
        {
            existingES.gameObject.AddComponent<StandaloneInputModule>();
        }

        // --- SceneLoader (used by MainMenuController and any future buttons) ---
        GameObject sceneLoaderGO = new GameObject("SceneLoader");
        SceneLoader sceneLoader = sceneLoaderGO.AddComponent<SceneLoader>();

        // --- Canvas ---
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // MainMenuController lives on Canvas; set its SceneLoader reference directly
        MainMenuController menuController = canvasGO.AddComponent<MainMenuController>();
        SerializedObject menuControllerSO = new SerializedObject(menuController);
        SerializedProperty sceneLoaderProp = menuControllerSO.FindProperty("sceneLoader");
        if (sceneLoaderProp != null)
        {
            sceneLoaderProp.objectReferenceValue = sceneLoader;
            menuControllerSO.ApplyModifiedProperties();
        }

        // --- Title text ---
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "ROGUELITE PROTOTYPE";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 60;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.color = Color.white;

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(1000, 120);

        // --- Buttons panel (future menu entries can go here) ---
        GameObject buttonsPanelGO = new GameObject("Buttons_Panel");
        buttonsPanelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform buttonsPanelRect = buttonsPanelGO.AddComponent<RectTransform>();
        buttonsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsPanelRect.pivot = new Vector2(0.5f, 0.5f);
        buttonsPanelRect.anchoredPosition = new Vector2(0, -50);
        buttonsPanelRect.sizeDelta = new Vector2(300, 80);

        // --- Play button ---
        GameObject buttonGO = new GameObject("PlayButton");
        buttonGO.transform.SetParent(buttonsPanelGO.transform, false);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        Button button = buttonGO.AddComponent<Button>();

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(300, 80);

        // Button label
        GameObject buttonTextGO = new GameObject("Text");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        Text buttonText = buttonTextGO.AddComponent<Text>();
        buttonText.text = "PLAY LEVEL 3";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 30;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        // Wire button -> PlayLevel3 as a PERSISTENT listener so it survives scene save/reload
        UnityEventTools.AddPersistentListener(button.onClick, menuController.PlayLevel3);

        EditorSceneManager.MarkSceneDirty(mainMenuScene);
        EditorSceneManager.SaveScene(mainMenuScene);

        Debug.Log("MainMenu scene created at " + mainMenuScenePath);

        UpdateBuildSettings();
    }

    private static void UpdateBuildSettings()
    {
        var sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");
        string mainMenuGuid = null;
        string mainGameGuid = null;

        foreach (var guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == mainMenuScenePath) mainMenuGuid = guid;
            if (path == mainGameScenePath) mainGameGuid = guid;
        }

        if (mainMenuGuid == null) { Debug.LogError("MainMenu scene not found in assets!"); return; }
        if (mainGameGuid == null) { Debug.LogError("Main (gameplay) scene not found in assets!"); return; }

        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;

        // Rebuild list: MainMenu first, Main second, rest after (no duplicates)
        System.Collections.Generic.List<EditorBuildSettingsScene> newList = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        newList.Add(new EditorBuildSettingsScene(mainMenuScenePath, true));

        bool mainAdded = false;
        foreach (var s in existing)
        {
            if (s.path == mainMenuScenePath) continue; // skip, already added
            if (s.path == mainGameScenePath)
            {
                if (!mainAdded) { newList.Add(s); mainAdded = true; }
                continue;
            }
            newList.Add(s);
        }
        if (!mainAdded)
            newList.Insert(1, new EditorBuildSettingsScene(mainGameScenePath, true));

        EditorBuildSettings.scenes = newList.ToArray();
        Debug.Log("Build Settings updated: MainMenu = Scene 0, Main = Scene 1");
    }
}
