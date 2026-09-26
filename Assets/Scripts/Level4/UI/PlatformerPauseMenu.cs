/*
PlatformerPauseMenu.cs

Purpose: Provide the platformer's pause toggle and runtime-created Resume/Main Menu controls.

Design decisions:
- PauseManager remains the owner of time-scale and pause reasons; this UI requests the PlatformerMenu reason
    and mirrors the resulting state instead of changing time directly.
- The menu ensures an EventSystem exists so generated buttons work in a minimal scene, and delegates scene
    navigation to SceneLoader when returning to the main menu.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlatformerPauseMenu : MonoBehaviour
{
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private Sprite menuFrame;

    private GameObject panel;
    private GameObject openButton;

    private void Awake()
    {
        BuildUi();
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
            TogglePause();
    }

    private void OnDestroy()
    {
        PauseManager.Instance?.Unpause(PauseManager.PauseReason.PlatformerMenu);
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
        background.sprite = menuFrame;
        background.type = Image.Type.Simple;
        background.color = Color.white;

        CreateText("Title", panel.transform, "PAUSE MENU", new Vector2(0f, 100f), 34, FontStyle.Normal);
        CreateButton("Continue", panel.transform, "Resume", new Vector2(0f, -5f), Continue);
        CreateButton("MainMenu", panel.transform, "Main Menu", new Vector2(0f, -110f), ReturnToMainMenu);
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
