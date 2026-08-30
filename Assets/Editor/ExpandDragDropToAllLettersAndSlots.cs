/*
ExpandDragDropToAllLettersAndSlots.cs

Editor utility: Expand the working drag & drop pattern (Letter_A + Slot_1)
to all letters (B-F) and all slots (2-6).

Reutilizes the same DraggableLetter and SlotDropZone components.
No new logic, no new architecture.
Strict isolation to ConstraintPuzzle_New.unity only.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExpandDragDropToAllLettersAndSlots
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";
    private const string lettersAreaPath = "Canvas/ConstraintPuzzleCanvas/PuzzleRoot/LettersArea";
    private const string slotsAreaPath = "Canvas/ConstraintPuzzleCanvas/PuzzleRoot/SlotsArea";

    public static void Expand()
    {
        ExpandDragDrop_MenuEntry();
    }

    [MenuItem("Prototype/Phase 3B - Expand Drag & Drop to All Letters and Slots")]
    public static void ExpandDragDrop_MenuEntry()
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene: " + scenePath);
            return;
        }

        int lettersModified = 0;
        int slotsModified = 0;

        // Add DraggableLetter to all letters (A-F)
        char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F' };
        foreach (char letter in letters)
        {
            GameObject letterGO = GameObject.Find($"{lettersAreaPath}/Letter_{letter}");
            if (letterGO == null)
            {
                Debug.LogWarning($"Letter_{letter} not found");
                continue;
            }

            // Add DraggableLetter if not already present
            if (letterGO.GetComponent<DraggableLetter>() == null)
            {
                letterGO.AddComponent<DraggableLetter>();
                lettersModified++;
                Debug.Log($"✓ Added DraggableLetter to Letter_{letter}");
            }
            else
            {
                Debug.Log($"⚠ Letter_{letter} already has DraggableLetter");
            }
        }

        // Add SlotDropZone to all slots (1-6)
        for (int i = 1; i <= 6; i++)
        {
            GameObject slotGO = GameObject.Find($"{slotsAreaPath}/Slot_{i}");
            if (slotGO == null)
            {
                Debug.LogWarning($"Slot_{i} not found");
                continue;
            }

            // Add SlotDropZone if not already present
            if (slotGO.GetComponent<SlotDropZone>() == null)
            {
                slotGO.AddComponent<SlotDropZone>();
                slotsModified++;
                Debug.Log($"✓ Added SlotDropZone to Slot_{i}");
            }
            else
            {
                Debug.Log($"⚠ Slot_{i} already has SlotDropZone");
            }
        }

        // Save the scene
        EditorSceneManager.SaveScene(scene);

        Debug.Log("=== PHASE 3B: DRAG & DROP EXPANDED ===");
        Debug.Log($"Letters modified: {lettersModified}/6");
        Debug.Log($"Slots modified: {slotsModified}/6");
        Debug.Log("All letters (A-F) are now draggable");
        Debug.Log("All slots (1-6) now accept drops");
        Debug.Log("Test: Drag any letter to any slot");
    }
}
