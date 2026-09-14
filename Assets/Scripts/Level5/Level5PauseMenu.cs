/*
Level5PauseMenu.cs

Purpose:
- Provide the Level 5 pause toggle and runtime-created Resume/Restart/Main Menu controls.
- Keep pause state, scene reloads and scene transitions delegated to the existing project systems.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class Level5PauseMenu : MonoBehaviour
{
    [SerializeField] private KeyCode pauseKey = KeyCode.P;

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
        CreateButton("Resume", panel.transform, "RESUME", new Vector2(0f, 35f), Resume);
        CreateButton("Restart", panel.transform, "RESTART", new Vector2(0f, -55f), Restart);
        CreateButton("MainMenu", panel.transform, "MAIN MENU", new Vector2(0f, -145f), ReturnToMainMenu);
    }

    private void TogglePause()
    {
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
#if UNITY_EDITOR
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
        if (font != null)
            return font;
#endif

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