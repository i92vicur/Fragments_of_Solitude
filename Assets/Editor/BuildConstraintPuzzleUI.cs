/*
BuildConstraintPuzzleUI.cs

Editor utility: Build the visual UI structure for ConstraintPuzzle_New scene.
Creates LettersArea, SlotsArea, and ButtonsArea with basic visual elements.

Phase 2: Visual structure only, no interaction logic yet.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class BuildConstraintPuzzleUI
{
    private const string scenePath = "Assets/Scenes/Level1/ConstraintPuzzle_New.unity";
    private static Canvas mainCanvas;
    private static Transform puzzleRoot;

    // Public method for batch mode execution
    public static void Build()
    {
        BuildUI();
    }

    [MenuItem("Prototype/Build ConstraintPuzzle UI")]
    public static void BuildUI()
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene: " + scenePath);
            return;
        }

        // Find key containers
        mainCanvas = Object.FindAnyObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("Canvas not found in scene");
            return;
        }

        GameObject puzzleRootGO = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot");
        if (puzzleRootGO == null)
        {
            Debug.LogError("PuzzleRoot not found");
            return;
        }

        puzzleRoot = puzzleRootGO.transform;

        // Clear any existing content in PuzzleRoot
        for (int i = puzzleRoot.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(puzzleRoot.GetChild(i).gameObject);
        }

        // Create main areas
        CreateLettersArea();
        CreateSlotsArea();
        CreateButtonsArea();

        ConstraintPuzzleSpriteSkin spriteSkin = puzzleRootGO.GetComponent<ConstraintPuzzleSpriteSkin>();
        if (spriteSkin == null)
        {
            spriteSkin = puzzleRootGO.AddComponent<ConstraintPuzzleSpriteSkin>();
        }
        spriteSkin.RefreshVisuals();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("ConstraintPuzzle UI built successfully!");
        Debug.Log("  - LettersArea created with 6 letters (A-F)");
        Debug.Log("  - SlotsArea created with 6 numbered slots (1-6)");
        Debug.Log("  - ButtonsArea created with Check and Back buttons");
    }

    private static void CreateLettersArea()
    {
        GameObject lettersAreaGO = new GameObject("LettersArea", typeof(RectTransform));
        lettersAreaGO.transform.SetParent(puzzleRoot, false);

        RectTransform lettersRect = lettersAreaGO.GetComponent<RectTransform>();
        lettersRect.anchorMin = new Vector2(0, 0.6f);
        lettersRect.anchorMax = new Vector2(1, 1);
        lettersRect.offsetMin = new Vector2(20, 20);
        lettersRect.offsetMax = new Vector2(-20, -20);

        // Add background
        Image lettersBg = lettersAreaGO.AddComponent<Image>();
        lettersBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Create letter grid (3 columns x 2 rows for 6 letters)
        string[] letters = { "A", "B", "C", "D", "E", "F" };
        float cellWidth = 120f;
        float cellHeight = 80f;
        float spacingX = 140f;
        float spacingY = 100f;

        for (int i = 0; i < letters.Length; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float xPos = col * spacingX - (spacingX * 1.5f) + spacingX;
            float yPos = -row * spacingY + spacingY * 0.7f;

            CreateLetterElement(lettersAreaGO.transform, letters[i], new Vector2(xPos, yPos));
        }
    }

    private static void CreateLetterElement(Transform parent, string letterText, Vector2 position)
    {
        GameObject letterGO = new GameObject($"Letter_{letterText}", typeof(RectTransform), typeof(Image));
        letterGO.transform.SetParent(parent, false);

        RectTransform letterRect = letterGO.GetComponent<RectTransform>();
        letterRect.anchorMin = Vector2.zero;
        letterRect.anchorMax = Vector2.zero;
        letterRect.pivot = new Vector2(0.5f, 0.5f);
        letterRect.anchoredPosition = position;
        letterRect.sizeDelta = new Vector2(110, 70);

        Image letterImage = letterGO.GetComponent<Image>();
        letterImage.color = new Color(0.2f, 0.3f, 0.5f, 0.9f);

        // Add text label
        GameObject letterTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        letterTextGO.transform.SetParent(letterGO.transform, false);

        RectTransform textRect = letterTextGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = letterTextGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = letterText;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 48;
        textComponent.color = Color.white;
    }

    private static void CreateSlotsArea()
    {
        GameObject slotsAreaGO = new GameObject("SlotsArea", typeof(RectTransform));
        slotsAreaGO.transform.SetParent(puzzleRoot, false);

        RectTransform slotsRect = slotsAreaGO.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0, 0.15f);
        slotsRect.anchorMax = new Vector2(1, 0.6f);
        slotsRect.offsetMin = new Vector2(20, 20);
        slotsRect.offsetMax = new Vector2(-20, -20);

        // Add background
        Image slotsBg = slotsAreaGO.AddComponent<Image>();
        slotsBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Create slot grid (3 columns x 2 rows for 6 slots)
        float cellWidth = 120f;
        float cellHeight = 80f;
        float spacingX = 140f;
        float spacingY = 100f;

        for (int i = 0; i < 6; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float xPos = col * spacingX - (spacingX * 1.5f) + spacingX;
            float yPos = -row * spacingY + spacingY * 0.7f;

            CreateSlotElement(slotsAreaGO.transform, (i + 1).ToString(), new Vector2(xPos, yPos));
        }
    }

    private static void CreateSlotElement(Transform parent, string slotNumber, Vector2 position)
    {
        GameObject slotGO = new GameObject($"Slot_{slotNumber}", typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(parent, false);

        RectTransform slotRect = slotGO.GetComponent<RectTransform>();
        slotRect.anchorMin = Vector2.zero;
        slotRect.anchorMax = Vector2.zero;
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.anchoredPosition = position;
        slotRect.sizeDelta = new Vector2(110, 70);

        Image slotImage = slotGO.GetComponent<Image>();
        slotImage.color = new Color(0.3f, 0.25f, 0.2f, 0.8f);

        // Add slot number label
        GameObject slotTextGO = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotTextGO.transform.SetParent(slotGO.transform, false);

        RectTransform textRect = slotTextGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = slotTextGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = slotNumber;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 40;
        textComponent.color = new Color(0.8f, 0.8f, 0.8f, 1f);
    }

    private static void CreateButtonsArea()
    {
        GameObject buttonsAreaGO = new GameObject("ButtonsArea", typeof(RectTransform));
        buttonsAreaGO.transform.SetParent(puzzleRoot, false);

        RectTransform buttonsRect = buttonsAreaGO.GetComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0, 0);
        buttonsRect.anchorMax = new Vector2(1, 0.15f);
        buttonsRect.offsetMin = new Vector2(20, 20);
        buttonsRect.offsetMax = new Vector2(-20, -20);

        // Add background
        Image buttonsBg = buttonsAreaGO.AddComponent<Image>();
        buttonsBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Create Check and Back buttons
        CreateButton(buttonsAreaGO.transform, "CheckButton", "Check", new Vector2(-140, 0));
        CreateButton(buttonsAreaGO.transform, "BackButton", "Back", new Vector2(140, 0));
    }

    private static void CreateButton(Transform parent, string buttonName, string labelText, Vector2 position)
    {
        GameObject buttonGO = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(240, 60);

        Image buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.4f, 0.6f, 0.9f);

        Button buttonComponent = buttonGO.GetComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;

        // Add button label
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTextComponent = labelGO.GetComponent<TextMeshProUGUI>();
        labelTextComponent.text = labelText;
        labelTextComponent.alignment = TextAlignmentOptions.Center;
        labelTextComponent.fontSize = 36;
        labelTextComponent.color = Color.white;
    }
}
