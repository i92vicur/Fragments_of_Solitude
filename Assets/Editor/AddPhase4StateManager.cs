/*
AddPhase4StateManager.cs

Editor utility: Add ConstraintPuzzleStateManager to ConstraintPuzzle_New.
This manager tracks the state of letter-to-slot assignments.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddPhase4StateManager
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";

    public static void Add()
    {
        Add_MenuEntry();
    }

    [MenuItem("Prototype/Phase 4A - Add State Manager")]
    public static void Add_MenuEntry()
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene: " + scenePath);
            return;
        }

        // Find or create ConstraintPuzzleCanvas
        GameObject constraintPuzzleCanvas = GameObject.Find("Canvas/ConstraintPuzzleCanvas");
        if (constraintPuzzleCanvas == null)
        {
            Debug.LogError("ConstraintPuzzleCanvas not found in scene");
            return;
        }

        // Check if state manager already exists
        ConstraintPuzzleStateManager existingManager = constraintPuzzleCanvas.GetComponent<ConstraintPuzzleStateManager>();
        if (existingManager != null)
        {
            Debug.LogWarning("ConstraintPuzzleStateManager already exists in the scene");
            return;
        }

        // Add the state manager component
        ConstraintPuzzleStateManager manager = constraintPuzzleCanvas.AddComponent<ConstraintPuzzleStateManager>();
        Debug.Log("✓ ConstraintPuzzleStateManager added to ConstraintPuzzleCanvas");

        // Save scene
        EditorSceneManager.SaveScene(scene);

        Debug.Log("=== PHASE 4A: STATE MANAGER ADDED ===");
        Debug.Log("The puzzle now has a state manager that tracks letter-to-slot assignments");
        Debug.Log("State updates will be visible in console during drops/swaps");
    }
}
