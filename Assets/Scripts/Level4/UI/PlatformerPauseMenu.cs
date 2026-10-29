/*
PlatformerPauseMenu.cs

Purpose:
- Controls the platformer pause menu flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

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
public class PlatformerPauseMenu : MonoBehaviour
{
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private Sprite menuFrame;
    [SerializeField] private GameObject controlHintPanel;

    private GameObject panel;
    private GameObject openButton;
    private readonly List<Button> panelButtons = new List<Button>();
    private readonly GamepadHoverBridge gamepadHover = new GamepadHoverBridge();
    private bool navLatched;

    private void Awake()
    {
        GamepadMenuNavigation.EnsureGameInput();
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
        PauseManager.Instance?.Unpause(PauseManager.PauseReason.PlatformerMenu);
    }

    private Sprite ResolveMenuFrame()
    {
        if (menuFrame != null)
            return menuFrame;

#if UNITY_EDITOR
        menuFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shared/Menus/menu_frame.png");
#endif

        if (menuFrame == null)
        {
            menuFrame = Resources.Load<Sprite>("menu_frame");
            if (menuFrame == null)
                menuFrame = Resources.Load<Sprite>("Art/menu_frame");
        }

        return menuFrame;
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("PlatformerPauseCanvas");
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

        panel = new GameObject("PlatformerPausePanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(750f, 650f);

        Image background = panel.AddComponent<Image>();
        var resolvedMenuFrame = ResolveMenuFrame();
        background.sprite = resolvedMenuFrame != null ? resolvedMenuFrame : menuFrame;
        background.type = Image.Type.Simple;
        background.color = Color.white;

        CreateText("Title", panel.transform, "PAUSE MENU", new Vector2(0f, 100f), 34, FontStyle.Normal);
        panelButtons.Add(CreateButton("Continue", panel.transform, "Resume", new Vector2(0f, -5f), Continue).GetComponent<Button>());
        panelButtons.Add(CreateButton("MainMenu", panel.transform, "Main Menu", new Vector2(0f, -110f), ReturnToMainMenu).GetComponent<Button>());

        foreach (Button button in panelButtons)
            GamepadMenuNavigation.ApplySelectionStyle(button);
    }

    // Arriba/abajo mueve la selección y A activa el botón (mismo esquema que Level5PauseMenu).
    private void HandleMenuInput()
    {
        EventSystem es = EventSystem.current;
        Button selected = GamepadMenuNavigation.UpdateSelection(es, panelButtons, ref navLatched);
        gamepadHover.Tick(es, selected);

        if (selected != null && GameInput.Submit.Pressed)
            GamepadMenuNavigation.Submit(es, selected);
    }

    private void TogglePause()
    {
        if (PauseManager.Instance.IsPausedFor(PauseManager.PauseReason.PlatformerMenu))
            Continue();
        else if (!PauseManager.Instance.IsPaused)
        {
            PauseManager.Instance.Pause(PauseManager.PauseReason.PlatformerMenu);
            SetVisible(true);
        }
    }

    private void Continue()
    {
        PauseManager.Instance.Unpause(PauseManager.PauseReason.PlatformerMenu);
        SetVisible(false);
    }

    private void ReturnToMainMenu()
    {
        PauseManager.Instance.Unpause(PauseManager.PauseReason.PlatformerMenu);
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

        if (controlHintPanel != null)
            controlHintPanel.SetActive(visible);

        navLatched = false;
        GamepadMenuNavigation.ClearSelection(EventSystem.current);

        if (GameInput.IsInitialized)
            GameInput.SetContext(visible || PauseManager.Instance.IsPaused ? GameInputContext.Menu : GameInputContext.Gameplay);
    }

    // Desactiva la navegación y Submit legacy del StandaloneInputModule para no duplicar activaciones con GameInput.
    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        FindAnyObjectByType<EventSystem>().sendNavigationEvents = false;
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
        rect.sizeDelta = new Vector2(280f, 42f);

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
