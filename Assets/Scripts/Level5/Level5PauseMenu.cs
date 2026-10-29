/*
Level5PauseMenu.cs

Purpose:
- Controls the level5 pause menu flow for the turn-based battle loop, combatants, battle UI, and encounter rules.

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
public class Level5PauseMenu : MonoBehaviour
{
    [SerializeField] private KeyCode pauseKey = KeyCode.P;

    private GameObject panel;
    private GameObject openButton;
    private Level5BattleController battleController;
    private readonly List<Button> panelButtons = new List<Button>();
    private bool navLatched;

    private void Awake()
    {
        GamepadMenuNavigation.EnsureGameInput();
        battleController = FindAnyObjectByType<Level5BattleController>();
        BuildUi();
        SetVisible(false);
    }

    private void Update()
    {
        bool togglePressed = Input.GetKeyDown(pauseKey)
            || (GameInput.IsInitialized && GameInput.Pause.Pressed);

        if (togglePressed)
            TogglePause();

        if (panel != null && panel.activeSelf)
            HandleMenuInput();
    }

    private void OnDestroy()
    {
        PauseManager.Instance?.Unpause(PauseManager.PauseReason.Level5Menu);
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Level5PauseCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        openButton = CreateButton("Pause", canvas.transform, "PAUSE", new Vector2(-40f, -30f), TogglePause);
        RectTransform openButtonRect = openButton.GetComponent<RectTransform>();
        openButtonRect.anchorMin = new Vector2(1f, 1f);
        openButtonRect.anchorMax = new Vector2(1f, 1f);
        openButtonRect.pivot = new Vector2(1f, 1f);
        openButtonRect.sizeDelta = new Vector2(200f, 65f);

        panel = new GameObject("Level5PausePanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(550f, 430f);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);

        CreateText("Title", panel.transform, "PAUSE MENU", new Vector2(0f, 130f), 34, FontStyle.Bold);
        panelButtons.Add(CreateButton("Resume", panel.transform, "RESUME", new Vector2(0f, 35f), Resume).GetComponent<Button>());
        panelButtons.Add(CreateButton("Restart", panel.transform, "RESTART", new Vector2(0f, -55f), Restart).GetComponent<Button>());
        panelButtons.Add(CreateButton("MainMenu", panel.transform, "MAIN MENU", new Vector2(0f, -145f), ReturnToMainMenu).GetComponent<Button>());

        foreach (Button button in panelButtons)
            GamepadMenuNavigation.ApplySelectionStyle(button);
    }

    // Arriba/abajo mueve la selección, A la activa y B reanuda (como en el nivel 2).
    private void HandleMenuInput()
    {
        EventSystem es = EventSystem.current;
        Button selected = GamepadMenuNavigation.UpdateSelection(es, panelButtons, ref navLatched);

        if (GameInput.Submit.Pressed && selected != null)
            GamepadMenuNavigation.Submit(es, selected);
        else if (GameInput.Cancel.Pressed)
            Resume();
    }

    private void TogglePause()
    {
        if (battleController != null && battleController.State != Level5BattleController.BattleState.InProgress)
            return;

        if (PauseManager.Instance.IsPausedFor(PauseManager.PauseReason.Level5Menu))
            Resume();
        else if (!PauseManager.Instance.IsPaused)
        {
            PauseManager.Instance.Pause(PauseManager.PauseReason.Level5Menu);
            SetVisible(true);
        }
    }

    private void Resume()
    {
        PauseManager.Instance.Unpause(PauseManager.PauseReason.Level5Menu);
        SetVisible(false);
    }

    private void Restart()
    {
        PauseManager.Instance.Unpause(PauseManager.PauseReason.Level5Menu);
        Time.timeScale = 1f;

        RestartController restartController = FindAnyObjectByType<RestartController>();
        if (restartController != null)
        {
            restartController.RestartNow();
            return;
        }

        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
            loader.ReloadCurrentScene();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    private void ReturnToMainMenu()
    {
        PauseManager.Instance.Unpause(PauseManager.PauseReason.Level5Menu);
        Time.timeScale = 1f;

        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
            loader.LoadMainMenu();
        else
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private void SetVisible(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);

        if (openButton != null)
            openButton.SetActive(!visible);

        navLatched = false;
        GamepadMenuNavigation.ClearSelection(EventSystem.current);

        if (GameInput.IsInitialized)
            GameInput.SetContext(visible || PauseManager.Instance.IsPaused ? GameInputContext.Menu : GameInputContext.Gameplay);
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Font GetPauseMenuFont()
    {
        Font font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void CreateText(string name, Transform parent, string value, Vector2 position, int size, FontStyle style)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(360f, 48f);

        Text text = textObject.AddComponent<Text>();
        text.font = GetPauseMenuFont();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(320f, 80f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateText("Label", buttonObject.transform, label, Vector2.zero, 24, FontStyle.Bold);
        return buttonObject;
    }
}