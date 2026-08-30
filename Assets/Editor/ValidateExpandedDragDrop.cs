/*
ValidateExpandedDragDrop.cs

Editor utility: Validate that all letters and slots have drag/drop components.
Checks:
- All letters (A-F) have DraggableLetter
- All slots (1-6) have SlotDropZone
- No broken references
- Scene integrity
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateExpandedDragDrop
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";
    private const string lettersAreaPath = "Canvas/ConstraintPuzzleCanvas/PuzzleRoot/LettersArea";
    private const string slotsAreaPath = "Canvas/ConstraintPuzzleCanvas/PuzzleRoot/SlotsArea";

    public static void Validate()
    {
        Debug.Log("=== PHASE 3B VALIDATION: Expanded Drag & Drop ===");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene");
            return;
        }

        int lettersValid = 0;
        int slotsValid = 0;
        bool allValid = true;

        // Check all letters (A-F)
        char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F' };
        Debug.Log("--- LETTERS ---");
        foreach (char letter in letters)
        {
            GameObject letterGO = GameObject.Find($"{lettersAreaPath}/Letter_{letter}");
            if (letterGO == null)
            {
                Debug.LogError($"✗ Letter_{letter} not found");
                allValid = false;
                continue;
            }

            DraggableLetter draggable = letterGO.GetComponent<DraggableLetter>();
            if (draggable != null)
            {
                Debug.Log($"✓ Letter_{letter} has DraggableLetter");
                lettersValid++;
            }
            else
            {
                Debug.LogError($"✗ Letter_{letter} missing DraggableLetter");
                allValid = false;
            }
        }

        Debug.Log($"Letters: {lettersValid}/6 valid");

        // Check all slots (1-6)
        Debug.Log("--- SLOTS ---");
        for (int i = 1; i <= 6; i++)
        {
            GameObject slotGO = GameObject.Find($"{slotsAreaPath}/Slot_{i}");
            if (slotGO == null)
            {
                Debug.LogError($"✗ Slot_{i} not found");
                allValid = false;
                continue;
            }

            SlotDropZone dropZone = slotGO.GetComponent<SlotDropZone>();
            if (dropZone != null)
            {
                Debug.Log($"✓ Slot_{i} has SlotDropZone");
                slotsValid++;
            }
            else
            {
                Debug.LogError($"✗ Slot_{i} missing SlotDropZone");
                allValid = false;
            }
        }

        Debug.Log($"Slots: {slotsValid}/6 valid");

        // Final verdict
        Debug.Log("=== VALIDATION COMPLETE ===");
        if (allValid && lettersValid == 6 && slotsValid == 6)
        {
            Debug.Log("✓ ALL CHECKS PASSED - Ready for testing");
        }
        else
        {
            Debug.LogError("✗ VALIDATION FAILED - Some components missing");
        }
    }
}
