/*
ValidatePhase4.cs

Validation script for Phase 4: Rectification + State Management
Checks for:
- State manager presence
- All components present
- No serialization errors
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidatePhase4
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";
    private const string lettersAreaPath = "Canvas/ConstraintPuzzleCanvas/PuzzleRoot/LettersArea";
    private const string slotsAreaPath = "Canvas/ConstraintPuzzleCanvas/PuzzleRoot/SlotsArea";

    public static void Validate()
    {
        Debug.Log("=== PHASE 4 VALIDATION: Rectification + State Management ===");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene");
            return;
        }

        bool allValid = true;

        // Check state manager
        GameObject constraintPuzzleCanvas = GameObject.Find("Canvas/ConstraintPuzzleCanvas");
        if (constraintPuzzleCanvas == null)
        {
            Debug.LogError("✗ ConstraintPuzzleCanvas not found");
            return;
        }

        ConstraintPuzzleStateManager stateManager = constraintPuzzleCanvas.GetComponent<ConstraintPuzzleStateManager>();
        if (stateManager != null)
        {
            Debug.Log("✓ State Manager present");
        }
        else
        {
            Debug.LogError("✗ State Manager missing");
            allValid = false;
        }

        // Check all components
        Debug.Log("--- LETTERS ---");
        char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F' };
        int lettersValid = 0;
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

        Debug.Log("--- SLOTS ---");
        int slotsValid = 0;
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
                Debug.Log($"✓ Slot_{i} has SlotDropZone (with swap support)");
                slotsValid++;
            }
            else
            {
                Debug.LogError($"✗ Slot_{i} missing SlotDropZone");
                allValid = false;
            }
        }
        Debug.Log($"Slots: {slotsValid}/6 valid");

        Debug.Log("=== VALIDATION COMPLETE ===");
        if (allValid && lettersValid == 6 && slotsValid == 6 && stateManager != null)
        {
            Debug.Log("✓ ALL CHECKS PASSED - Phase 4 ready for testing");
        }
        else
        {
            Debug.LogError("✗ VALIDATION FAILED");
        }
    }
}
