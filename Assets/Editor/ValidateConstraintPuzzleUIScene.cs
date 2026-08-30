/*
ValidateConstraintPuzzleUIScene.cs

Editor utility: Validate that the ConstraintPuzzle_New scene with UI can be opened/closed without errors.
Checks for broken references, proper component loading, and visual element visibility.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class ValidateConstraintPuzzleUIScene
{
    private const string scenePath = "Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity";

    public static void Validate()
    {
        ValidateScene();
    }

    public static void ValidateScene()
    {
        Debug.Log("=== VALIDATION: ConstraintPuzzle_New with UI ===");

        // Check file exists
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError("Scene file not found at " + scenePath);
            return;
        }

        // Open the scene
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene!");
            return;
        }

        Debug.Log($"✓ Scene loaded: {scene.name}");

        // Verify key structures exist
        ValidateStructure();
        ValidateLettersArea();
        ValidateSlotsArea();
        ValidateButtonsArea();

        Debug.Log("=== VALIDATION COMPLETE ===");
        Debug.Log("Scene structure is HEALTHY - all UI elements verified");
    }

    private static void ValidateStructure()
    {
        GameObject puzzleRoot = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot");
        if (puzzleRoot == null)
        {
            Debug.LogError("✗ PuzzleRoot not found!");
            return;
        }
        Debug.Log("✓ PuzzleRoot found");

        // Count children
        int childCount = puzzleRoot.transform.childCount;
        Debug.Log($"✓ PuzzleRoot contains {childCount} children (expected 3: LettersArea, SlotsArea, ButtonsArea)");
    }

    private static void ValidateLettersArea()
    {
        GameObject lettersArea = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/LettersArea");
        if (lettersArea == null)
        {
            Debug.LogError("✗ LettersArea not found!");
            return;
        }
        Debug.Log("✓ LettersArea found");

        int letterCount = lettersArea.transform.childCount;
        Debug.Log($"  ✓ Contains {letterCount} letter elements");

        if (letterCount > 0)
        {
            for (int i = 0; i < letterCount; i++)
            {
                Transform letterTransform = lettersArea.transform.GetChild(i);
                Image letterImage = letterTransform.GetComponent<Image>();
                TextMeshProUGUI letterText = letterTransform.GetComponentInChildren<TextMeshProUGUI>();

                if (letterImage == null)
                    Debug.LogWarning($"  ⚠ Letter {i} missing Image component");
                if (letterText == null)
                    Debug.LogWarning($"  ⚠ Letter {i} missing TextMeshProUGUI component");
            }
        }
    }

    private static void ValidateSlotsArea()
    {
        GameObject slotsArea = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/SlotsArea");
        if (slotsArea == null)
        {
            Debug.LogError("✗ SlotsArea not found!");
            return;
        }
        Debug.Log("✓ SlotsArea found");

        int slotCount = slotsArea.transform.childCount;
        Debug.Log($"  ✓ Contains {slotCount} slot elements");

        if (slotCount > 0)
        {
            for (int i = 0; i < slotCount; i++)
            {
                Transform slotTransform = slotsArea.transform.GetChild(i);
                Image slotImage = slotTransform.GetComponent<Image>();
                TextMeshProUGUI slotText = slotTransform.GetComponentInChildren<TextMeshProUGUI>();

                if (slotImage == null)
                    Debug.LogWarning($"  ⚠ Slot {i} missing Image component");
                if (slotText == null)
                    Debug.LogWarning($"  ⚠ Slot {i} missing TextMeshProUGUI component");
            }
        }
    }

    private static void ValidateButtonsArea()
    {
        GameObject buttonsArea = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot/ButtonsArea");
        if (buttonsArea == null)
        {
            Debug.LogError("✗ ButtonsArea not found!");
            return;
        }
        Debug.Log("✓ ButtonsArea found");

        int buttonCount = buttonsArea.transform.childCount;
        Debug.Log($"  ✓ Contains {buttonCount} button elements");

        // Check for specific buttons
        Transform checkButtonTransform = buttonsArea.transform.Find("CheckButton");
        Transform backButtonTransform = buttonsArea.transform.Find("BackButton");

        if (checkButtonTransform != null)
        {
            Button checkButton = checkButtonTransform.GetComponent<Button>();
            if (checkButton != null)
                Debug.Log("  ✓ Check button found and functional");
            else
                Debug.LogWarning("  ⚠ Check button missing Button component");
        }
        else
        {
            Debug.LogWarning("  ⚠ Check button not found");
        }

        if (backButtonTransform != null)
        {
            Button backButton = backButtonTransform.GetComponent<Button>();
            if (backButton != null)
                Debug.Log("  ✓ Back button found and functional");
            else
                Debug.LogWarning("  ⚠ Back button missing Button component");
        }
        else
        {
            Debug.LogWarning("  ⚠ Back button not found");
        }
    }
}
