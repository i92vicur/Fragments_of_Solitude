/*
ValidatePhase3ADragDrop.cs

Editor utility: Validate that Phase 3A drag & drop components are correctly added.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ValidatePhase3ADragDrop
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";

    public static void Validate()
    {
        Debug.Log("=== PHASE 3A VALIDATION: Checking Drag & Drop Components ===");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene");
            return;
        }

        // Check Letter_A
        GameObject letterA = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/LettersArea/Letter_A");
        if (letterA == null)
        {
            Debug.LogError("✗ Letter_A not found");
            return;
        }

        DraggableLetter draggable = letterA.GetComponent<DraggableLetter>();
        if (draggable != null)
        {
            Debug.Log("✓ Letter_A has DraggableLetter component");
        }
        else
        {
            Debug.LogWarning("✗ Letter_A missing DraggableLetter component");
        }

        // Check Slot_1
        GameObject slot1 = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/SlotsArea/Slot_1");
        if (slot1 == null)
        {
            Debug.LogError("✗ Slot_1 not found");
            return;
        }

        SlotDropZone dropZone = slot1.GetComponent<SlotDropZone>();
        if (dropZone != null)
        {
            Debug.Log("✓ Slot_1 has SlotDropZone component");
        }
        else
        {
            Debug.LogWarning("✗ Slot_1 missing SlotDropZone component");
        }

        // Check Canvas prerequisites
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log("✓ Canvas has GraphicRaycaster");
            }
            else
            {
                Debug.LogWarning("✗ Canvas missing GraphicRaycaster");
            }
        }

        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log("✓ EventSystem found");
        }
        else
        {
            Debug.LogWarning("✗ EventSystem not found");
        }

        Debug.Log("=== VALIDATION COMPLETE ===");
    }
}
