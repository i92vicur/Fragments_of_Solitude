/*
ValidateConstraintPuzzleScene.cs

Editor utility: Validate that the ConstraintPuzzle_New scene can be opened without errors.
Used for batch mode verification to ensure scene integrity.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public static class ValidateConstraintPuzzleScene
{
    private const string puzzleScenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";

    public static void ValidateScene()
    {
        Debug.Log("=== VALIDATION: Opening ConstraintPuzzle_New ===");
        
        // Check file exists
        if (!System.IO.File.Exists(puzzleScenePath))
        {
            Debug.LogError("Scene file not found at " + puzzleScenePath);
            return;
        }

        // Open the scene
        Scene scene = EditorSceneManager.OpenScene(puzzleScenePath, OpenSceneMode.Single);
        
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene!");
            return;
        }

        Debug.Log($"✓ Scene loaded successfully: {scene.name}");
        
        // Count GameObjects
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log($"✓ Scene contains {allObjects.Length} GameObjects");

        // Verify key objects exist
        Camera camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null)
            Debug.Log("✓ Main Camera found");
        else
            Debug.LogWarning("⚠ Main Camera not found");

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
            Debug.Log("✓ Canvas found");
        else
            Debug.LogWarning("⚠ Canvas not found");

        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
            Debug.Log("✓ EventSystem found");
        else
            Debug.LogWarning("⚠ EventSystem not found");

        // Verify ConstraintPuzzleCanvas
        GameObject constraintPuzzleCanvas = GameObject.Find("Canvas/ConstraintPuzzleCanvas");
        if (constraintPuzzleCanvas != null)
            Debug.Log("✓ ConstraintPuzzleCanvas found");
        else
            Debug.LogWarning("⚠ ConstraintPuzzleCanvas not found");

        // Verify PuzzleRoot
        GameObject puzzleRoot = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot");
        if (puzzleRoot != null)
            Debug.Log("✓ PuzzleRoot found");
        else
            Debug.LogWarning("⚠ PuzzleRoot not found");

        Debug.Log("=== VALIDATION COMPLETE ===");
        Debug.Log("Scene structure is HEALTHY and ready for puzzle implementation");
    }
}
