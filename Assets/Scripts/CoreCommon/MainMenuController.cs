/*
MainMenuController.cs

Purpose:
- Controls the main menu controller flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField, Tooltip("Confirmation panel shown before starting a new game. Auto-found if not assigned.")]
    private GameObject newGameConfirmationPanel = null;

    [SerializeField, Tooltip("New Game button in the scene. Auto-found if not assigned.")]
    private Button newGameButton = null;

    private const string ButtonsPanelPath = "Buttons_Panel";
    private const string Level3ButtonPath = "Buttons_Panel/Level3Button";
    private const string Level4ButtonPath = "Buttons_Panel/Level4Button";
    private const string Level5ButtonPath = "Buttons_Panel/Level5Button";
    private const string NewGameButtonPath = "Buttons_Panel/NewGameButton";
    private const string QuitButtonName = "BackToDesktopButton";

    private const string Level3ButtonBaseLabel = "Level 2 (RogueLite)";
    private const string Level4ButtonBaseLabel = "Level 4 (Platformer)";
    private const string Level5ButtonBaseLabel = "Level 5 (Turn-Based Combat) ";
    private const string NewGameButtonBaseLabel = "New Game";
    private const string CompletionText = "\u2713 COMPLETE";
    private const string CompletionColor = "#3CCF4E";
    private const float CompletionLabelOffsetX = 0f;
    private const float CompletionLabelOffsetY = 58f;
    private const float CompletionLabelWidth = 280f;
    private const float CompletionLabelHeight = 40f;

    [SerializeField, Tooltip("SceneLoader in this scene. Auto-found if not assigned.")]
    private SceneLoader sceneLoader = null;

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackGameplaySceneName = "Level2";

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackSlidingPuzzleSceneName = "PuzzleMenu";

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackLongestPathPuzzleSceneName = "LongestPathPuzzle";

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackLevel5SceneName = "Level5";

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackPuzzleMenuSceneName = "PuzzleMenu";

    private readonly GamepadHoverBridge gamepadHover = new GamepadHoverBridge();
    private readonly List<Button> activeButtons = new List<Button>();
    private GameObject activeListKey;
    private bool pendingDefaultSelection;
    private bool navLatched;
    private GameObject settingsPanel;

    private void Awake()
    {
        // La navegación legacy del StandaloneInputModule no lee el D-pad ni el stick; la sustituye GameInput.
        // Sin esto, el módulo también dispararía Submit por su cuenta y se duplicarían activaciones.
        GamepadMenuNavigation.EnsureGameInput();
        GameInput.SetContext(GameInputContext.Menu);

        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es != null)
            es.sendNavigationEvents = false;
    }

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        BuildActiveButtonList();
        GameObject listKey = GetActiveListKey();
        if (listKey != activeListKey)
        {
            activeListKey = listKey;
            pendingDefaultSelection = true;
        }

        Button selected = null;
        if (activeButtons.Count > 0)
        {
            if (pendingDefaultSelection)
            {
                pendingDefaultSelection = false;
                es.SetSelectedGameObject(activeButtons[0].gameObject);
            }

            // Sin selección y sin navegación (p. ej. clic en zona vacía) no se reselecciona automáticamente.
            if (es.currentSelectedGameObject != null || GamepadMenuNavigation.NavigateActive)
                selected = GamepadMenuNavigation.UpdateSelection(es, activeButtons, ref navLatched);
        }
        else
        {
            navLatched = false;
        }

        if (selected != null && GameInput.Submit.Pressed)
            GamepadMenuNavigation.Submit(es, selected);

        gamepadHover.Tick(es, selected);
    }

    // Prioridad: confirmación de nueva partida > panel de ajustes > botones principales.
    private GameObject GetActiveListKey()
    {
        if (newGameConfirmationPanel != null && newGameConfirmationPanel.activeInHierarchy)
            return newGameConfirmationPanel;

        if (settingsPanel == null)
            settingsPanel = FindInactiveChild("SettingsPanel");

        if (settingsPanel != null && settingsPanel.activeInHierarchy)
            return settingsPanel;

        Transform buttonsPanel = transform.Find(ButtonsPanelPath);
        return buttonsPanel != null ? buttonsPanel.gameObject : null;
    }

    private void BuildActiveButtonList()
    {
        activeButtons.Clear();

        GameObject key = GetActiveListKey();
        if (key == null)
            return;

        Button[] buttons = key.GetComponentsInChildren<Button>();

        foreach (Button button in buttons)
        {
            if (button.interactable)
                activeButtons.Add(button);
        }

        // Cancel primero: una pulsación accidental de A tras abrir la confirmación no inicia la partida.
        if (key == newGameConfirmationPanel)
            activeButtons.Sort((a, b) => (b.name.Contains("Cancel") ? 1 : 0) - (a.name.Contains("Cancel") ? 1 : 0));
    }

    private GameObject FindInactiveChild(string objectName)
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private void Start()
    {
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (newGameConfirmationPanel == null)
            newGameConfirmationPanel = transform.Find("NewGameConfirmationPanel")?.gameObject;

        if (newGameButton == null)
            newGameButton = transform.Find(NewGameButtonPath)?.GetComponent<Button>();

        EnsureQuitButton();
        EnsureCompletionLabels();
        RefreshCompletionIndicators();

        if (newGameConfirmationPanel != null)
            newGameConfirmationPanel.SetActive(false);

    }

    private void OnEnable()
    {
        if (newGameButton == null)
            newGameButton = transform.Find(NewGameButtonPath)?.GetComponent<Button>();

        EnsureQuitButton();
        EnsureCompletionLabels();
        RefreshCompletionIndicators();

        if (newGameConfirmationPanel != null)
            newGameConfirmationPanel.SetActive(false);

    }

    private void EnsureQuitButton()
    {
        Transform buttonPanel = transform.Find("Buttons_Panel");
        if (buttonPanel == null)
            return;

        if (buttonPanel.Find(QuitButtonName) != null)
            return;

        GameObject quitButtonObject = new GameObject(QuitButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        quitButtonObject.transform.SetParent(buttonPanel, false);

        RectTransform buttonRect = quitButtonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(315f, 67f);
        buttonRect.anchoredPosition = new Vector2(0f, -270f);

        Image buttonImage = quitButtonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);

        Button button = quitButtonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 1f);
        colors.highlightedColor = new Color(0.82f, 0.05f, 0.07f, 1f);
        colors.pressedColor = new Color(0.247f, 0.247f, 0.247f, 1f);
        colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f);
        button.colors = colors;
        button.onClick.AddListener(QuitToDesktop);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(quitButtonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = "BACK TO DESKTOP";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }


    /// <summary>
    /// Called by the Play button. Loads the gameplay scene.
    /// Uses SceneLoader if available; falls back to direct SceneManager call.
    /// </summary>
    public void PlayLevel3()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadGameplay();
        }
        else
        {
            // Direct fallback so the button works even without SceneLoader in scene.
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackGameplaySceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the platformer button. Loads the platformer gameplay scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadPlatformer()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadPlatformer();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Level4", LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the Level 5 button. Loads the turn-based battle scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadLevel5()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadLevel5();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackLevel5SceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the sliding puzzle button. Loads the 4x4 sliding puzzle scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadSlidingPuzzle4x4()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadSlidingPuzzle4x4();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackSlidingPuzzleSceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Load the puzzle selector menu from the main menu.
    /// </summary>
    public void LoadPuzzleMenu()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadPuzzleMenu();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackPuzzleMenuSceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the longest-path button. Loads the road puzzle scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadLongestPathPuzzle()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadLongestPathPuzzle();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackLongestPathPuzzleSceneName, LoadSceneMode.Single);
        }
    }

    public void QuitToDesktop()
    {
        if (Application.isEditor)
        {
            Debug.Log("QuitToDesktop requested from editor; application exit suppressed in editor.");
            return;
        }

        Application.Quit();
    }

    public void ShowNewGameConfirmation()
    {
        if (newGameConfirmationPanel != null)
            newGameConfirmationPanel.SetActive(true);
    }

    public void CancelNewGame()
    {
        if (newGameConfirmationPanel != null)
            newGameConfirmationPanel.SetActive(false);
    }

    public void ConfirmNewGame()
    {
        GameProgressManager.Instance.StartNewGame();
        RefreshCompletionIndicators();

        if (newGameConfirmationPanel != null)
            newGameConfirmationPanel.SetActive(false);
    }

    private void RefreshCompletionIndicators()
    {
        UpdateCompletionLabel(Level3ButtonPath, Level3ButtonBaseLabel, GameProgressManager.Instance.Level3Completed);
        UpdateCompletionLabel(Level4ButtonPath, Level4ButtonBaseLabel, GameProgressManager.Instance.Level4Completed);
        UpdateCompletionLabel(Level5ButtonPath, Level5ButtonBaseLabel, GameProgressManager.Instance.Level5Completed);
    }

    private void EnsureCompletionLabels()
    {
        CreateOrCacheCompletionLabel(Level3ButtonPath, "Level3CompletionLabel");
        CreateOrCacheCompletionLabel(Level4ButtonPath, "Level4CompletionLabel");
        CreateOrCacheCompletionLabel(Level5ButtonPath, "Level5CompletionLabel");
    }

    private void UpdateCompletionLabel(string buttonPath, string baseLabel, bool completed)
    {
        Text label = GetCompletionLabel(buttonPath);
        if (label == null)
            return;

        label.gameObject.SetActive(completed);
        if (!completed)
            return;

        label.text = CompletionText;
    }

    private Text GetCompletionLabel(string buttonPath)
    {
        Transform buttonTransform = transform.Find(buttonPath);
        if (buttonTransform == null)
            return null;

        string labelName = buttonTransform.name switch
        {
            "Level3Button" => "Level3CompletionLabel",
            "Level4Button" => "Level4CompletionLabel",
            "Level5Button" => "Level5CompletionLabel",
            _ => null
        };

        if (string.IsNullOrEmpty(labelName))
            return null;

        Transform labelTransform = transform.Find(ButtonsPanelPath + "/" + labelName);
        return labelTransform != null ? labelTransform.GetComponent<Text>() : null;
    }

    private void CreateOrCacheCompletionLabel(string buttonPath, string labelName)
    {
        Transform buttonTransform = transform.Find(buttonPath);
        if (buttonTransform == null)
            return;

        Transform existingLabel = transform.Find(ButtonsPanelPath + "/" + labelName);
        if (existingLabel != null)
            return;

        RectTransform buttonRect = buttonTransform as RectTransform;
        if (buttonRect == null)
            return;

        Transform buttonsPanel = transform.Find(ButtonsPanelPath);
        if (buttonsPanel == null)
            return;

        GameObject labelObject = new GameObject(labelName);
        labelObject.transform.SetParent(buttonsPanel, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = buttonRect.anchorMin;
        labelRect.anchorMax = buttonRect.anchorMax;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x + CompletionLabelOffsetX, buttonRect.anchoredPosition.y + CompletionLabelOffsetY);
        labelRect.sizeDelta = new Vector2(CompletionLabelWidth, CompletionLabelHeight);

        Text label = labelObject.AddComponent<Text>();
        Font completionFont = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        label.font = completionFont != null ? completionFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = ParseCompletionColor();
        label.raycastTarget = false;
        label.supportRichText = true;
        label.text = CompletionText;

        labelObject.SetActive(false);
    }

    private static Color ParseCompletionColor()
    {
        if (ColorUtility.TryParseHtmlString(CompletionColor, out Color color))
            return color;

        return new Color(0.235f, 0.811f, 0.306f, 1f);

    }
}
