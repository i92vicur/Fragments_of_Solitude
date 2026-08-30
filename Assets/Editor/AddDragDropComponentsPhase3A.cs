/*
AddDragDropComponentsPhase3A.cs

Editor utility: Add minimal drag & drop components to Letter_A and Slot_1 for Phase 3A testing.

This script:
- Adds DraggableLetter to Letter_A
- Adds SlotDropZone to Slot_1
- Does NOT modify scene structure or layout
- Maintains strict isolation to ConstraintPuzzle_New.unity
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddDragDropComponentsPhase3A
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";

    public static void AddComponents()
    {
        AddComponents_MenuEntry();
    }

    [MenuItem("Prototype/Phase 3A - Add Drag & Drop Components")]
    public static void AddComponents_MenuEntry()
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene: " + scenePath);
            return;
        }

        // Find Letter_A
        GameObject letterA = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/LettersArea/Letter_A");
        if (letterA == null)
        {
            Debug.LogError("Letter_A not found in scene");
            return;
        }

        // Find Slot_1
        GameObject slot1 = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/SlotsArea/Slot_1");
        if (slot1 == null)
        {
            Debug.LogError("Slot_1 not found in scene");
            return;
        }

        // Add DraggableLetter to Letter_A if not already present
        if (letterA.GetComponent<DraggableLetter>() == null)
        {
            letterA.AddComponent<DraggableLetter>();
            Debug.Log("✓ Added DraggableLetter component to Letter_A");
        }
        else
        {
            Debug.Log("⚠ Letter_A already has DraggableLetter component");
        }

        // Add SlotDropZone to Slot_1 if not already present
        if (slot1.GetComponent<SlotDropZone>() == null)
        {
            slot1.AddComponent<SlotDropZone>();
            Debug.Log("✓ Added SlotDropZone component to Slot_1");
        }
        else
        {
            Debug.Log("⚠ Slot_1 already has SlotDropZone component");
        }

        // Save the scene
        EditorSceneManager.SaveScene(scene);

        Debug.Log("=== PHASE 3A: DRAG & DROP COMPONENTS ADDED ===");
        Debug.Log("Letter_A is now draggable");
        Debug.Log("Slot_1 now accepts drops");
        Debug.Log("Test: Drag Letter_A and drop it on Slot_1");
    }
}
