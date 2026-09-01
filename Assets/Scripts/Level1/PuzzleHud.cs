/*
PuzzleHud.cs

Purpose: Build and update the sliding-puzzle HUD, including moves, restart and completion controls.

Design decisions:
- The HUD can create its Canvas and controls at runtime, so the puzzle scene needs minimal manual wiring.
- PuzzleBoard remains the source of truth; this class only reflects its move count and completion event.
- Navigation delegates to SceneLoader when available, keeping scene names and time-scale restoration out of
    the puzzle UI.
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PuzzleHud : MonoBehaviour
{
    [SerializeField] private PuzzleBoard puzzleBoard;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string movesLabelPrefix = "Moves: ";
    [SerializeField] private string restartButtonLabel = "Restart Puzzle";
    [SerializeField] private string nextPuzzleButtonLabel = "Next Puzzle →";
    [SerializeField] private string completionMessageText = "PUZZLE COMPLETE!";

    private Text moveCounterText;
    private Button restartButton;
    private Button nextPuzzleButton;
    private GameObject completionPanel;
    private Text completionText;
    private RectTransform hudRoot;

    private void Awake()
    {
        if (puzzleBoard == null)
            puzzleBoard = FindAnyObjectByType<PuzzleBoard>();

        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        BuildHud();
    }

    private void OnEnable()
    {
        if (puzzleBoard == null)
            puzzleBoard = FindAnyObjectByType<PuzzleBoard>();

        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (puzzleBoard != null)
            puzzleBoard.PuzzleCompleted += HandlePuzzleCompleted;
    }

    private void Start()
    {
        RefreshHud();
    }

    private void Update()
    {
        RefreshHud();
    }

    private void OnDisable()
    {
        if (puzzleBoard != null)
            puzzleBoard.PuzzleCompleted -= HandlePuzzleCompleted;
    }

    private void HandlePuzzleCompleted()
    {
        SetCompletionVisible(true);
    }

    private void HandleRestartClicked()
    {
        if (puzzleBoard == null)
            return;

        puzzleBoard.RestartPuzzle();
        SetCompletionVisible(false);
        RefreshHud();
    }

    private void HandleNextPuzzleClicked()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadPuzzleMenu();
            return;
        }

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("PuzzleMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void RefreshHud()
    {
        if (puzzleBoard == null)
            return;

        if (moveCounterText != null)
            moveCounterText.text = $"{movesLabelPrefix}{puzzleBoard.MoveCount}";

        SetCompletionVisible(puzzleBoard.IsPuzzleCompleted);
    }

    private void BuildHud()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PuzzleHud] No Canvas found in scene.", this);
            return;
        }

        GameObject rootGO = new GameObject("PuzzleHUDRoot", typeof(RectTransform));
        rootGO.transform.SetParent(canvas.transform, false);
        hudRoot = rootGO.GetComponent<RectTransform>();
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.offsetMin = Vector2.zero;
        hudRoot.offsetMax = Vector2.zero;

        moveCounterText = CreateText("MoveCounter", hudRoot, new Vector2(20f, -20f), new Vector2(260f, 40f), 28, TextAnchor.UpperLeft, movesLabelPrefix + "0", Color.white);

        restartButton = CreateButton("RestartButton", hudRoot, new Vector2(-20f, -20f), new Vector2(220f, 48f), restartButtonLabel);
        restartButton.onClick.AddListener(HandleRestartClicked);

        nextPuzzleButton = CreateButton("NextPuzzleButton", hudRoot, new Vector2(-20f, -78f), new Vector2(220f, 48f), nextPuzzleButtonLabel);
        nextPuzzleButton.onClick.AddListener(HandleNextPuzzleClicked);

        completionPanel = new GameObject("CompletionPanel", typeof(RectTransform), typeof(Image));
        completionPanel.transform.SetParent(hudRoot, false);

        RectTransform panelRect = completionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 0f);
        panelRect.sizeDelta = new Vector2(420f, 140f);

        Image panelImage = completionPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        completionText = CreateText("CompletionText", completionPanel.transform, Vector2.zero, new Vector2(400f, 100f), 36, TextAnchor.MiddleCenter, completionMessageText, Color.white);
        completionPanel.SetActive(false);
    }

    private Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, string text, Color color)
    {
        GameObject textGO = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(parent, false);

        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text label = textGO.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, string labelText)
    {
        GameObject buttonGO = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = image;

        CreateText("Label", buttonGO.transform, Vector2.zero, size, 24, TextAnchor.MiddleCenter, labelText, Color.white);
        return button;
    }

    private void SetCompletionVisible(bool visible)
    {
        if (completionPanel != null)
            completionPanel.SetActive(visible);
    }
}
