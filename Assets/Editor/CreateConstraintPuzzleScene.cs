/*
CreateConstraintPuzzleScene.cs

Editor utility: Create a minimal ConstraintPuzzle_New scene with minimal, healthy structure.
- Main Camera
- Canvas with EventSystem
- ConstraintPuzzleCanvas root object
- PuzzleRoot empty container inside

No gameplay logic, no complex components - just the foundational structure.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class CreateConstraintPuzzleScene
{
    private const string puzzleScenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";

    // Public static method for batch mode execution
    public static void CreateScene()
    {
        CreateConstraintPuzzleScene_MenuEntry();
    }

    [MenuItem("Prototype/Create ConstraintPuzzle_New Scene")]
    public static void CreateConstraintPuzzleScene_MenuEntry()
    {
        // Check if scene already exists
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(puzzleScenePath) != null)
        {
            Debug.LogWarning("ConstraintPuzzle_New scene already exists at " + puzzleScenePath);
            return;
        }

        // Create a new scene with default setup (includes Main Camera and Light)
        Scene puzzleScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(puzzleScene, puzzleScenePath);

        // Now open it and set it up
        puzzleScene = EditorSceneManager.OpenScene(puzzleScenePath, OpenSceneMode.Single);

        // --- Main Camera Setup ---
        Camera mainCamera = Object.FindAnyObjectByType<Camera>();
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5;
            mainCamera.gameObject.name = "Main Camera";
            mainCamera.tag = "MainCamera";
        }

        // --- Ensure EventSystem exists ---
        EventSystem existingES = Object.FindAnyObjectByType<EventSystem>();
        if (existingES == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem with StandaloneInputModule");
        }
        else if (existingES.GetComponent<StandaloneInputModule>() == null)
        {
            existingES.gameObject.AddComponent<StandaloneInputModule>();
            Debug.Log("Added StandaloneInputModule to existing EventSystem");
        }

        // --- Canvas Setup ---
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

        // --- ConstraintPuzzleCanvas (root container for puzzle UI) ---
        GameObject constraintPuzzleCanvasGO = new GameObject("ConstraintPuzzleCanvas");
        constraintPuzzleCanvasGO.transform.SetParent(canvasGO.transform, false);
        RectTransform constraintPuzzleCanvasRect = constraintPuzzleCanvasGO.AddComponent<RectTransform>();
        constraintPuzzleCanvasRect.anchorMin = Vector2.zero;
        constraintPuzzleCanvasRect.anchorMax = Vector2.one;
        constraintPuzzleCanvasRect.offsetMin = Vector2.zero;
        constraintPuzzleCanvasRect.offsetMax = Vector2.zero;

        // --- PuzzleRoot (empty container inside ConstraintPuzzleCanvas) ---
        GameObject puzzleRootGO = new GameObject("PuzzleRoot");
        puzzleRootGO.transform.SetParent(constraintPuzzleCanvasGO.transform, false);
        RectTransform puzzleRootRect = puzzleRootGO.AddComponent<RectTransform>();
        puzzleRootRect.anchorMin = Vector2.zero;
        puzzleRootRect.anchorMax = Vector2.one;
        puzzleRootRect.offsetMin = Vector2.zero;
        puzzleRootRect.offsetMax = Vector2.zero;

        // --- SceneLoader (for navigation back to menu) ---
        GameObject sceneLoaderGO = new GameObject("SceneLoader");
        SceneLoader sceneLoader = sceneLoaderGO.AddComponent<SceneLoader>();
        Debug.Log("Added SceneLoader to scene for menu navigation");

        // Save the scene
        EditorSceneManager.SaveScene(puzzleScene);

        Debug.Log("ConstraintPuzzle_New scene created successfully at " + puzzleScenePath);
        Debug.Log("Scene structure:");
        Debug.Log("  - Main Camera");
        Debug.Log("  - EventSystem");
        Debug.Log("  - Canvas");
        Debug.Log("    - ConstraintPuzzleCanvas");
        Debug.Log("      - PuzzleRoot");
        Debug.Log("  - SceneLoader");
    }
}
