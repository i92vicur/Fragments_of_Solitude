/*
Phase2VictoryMenu.cs

Purpose: Show the Phase 2 level-complete panel (Restart / Main Menu) once Phase2GoalController reaches the goal.

Design decisions:
- Mirrors PlatformerPauseMenu's runtime-built UI approach so no extra prefabs/assets are required.
- Reuses PauseManager's existing Victory reason to freeze gameplay instead of a bespoke pause flag.
- Delegates scene navigation to SceneLoader/RestartController, matching every other menu in the project.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class Phase2VictoryMenu : MonoBehaviour
{
    private GameObject panel;

    private void Awake()
    {
        BuildUi();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        PauseManager.Instance?.Unpause(PauseManager.PauseReason.Victory);
    }

    /// <summary>
    /// Pauses gameplay and reveals the level-complete panel. Called by Phase2GoalController.
    /// </summary>
    public void Show()
    {
        PauseManager.Instance.Pause(PauseManager.PauseReason.Victory);
        SetVisible(true);
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Phase2VictoryCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        panel = new GameObject("Phase2VictoryPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(550f, 330f);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);

        CreateText("Title", panel.transform, "LEVEL COMPLETE", new Vector2(0f, 110f), 34, FontStyle.Bold);
        CreateButton("Restart", panel.transform, "RESTART", new Vector2(0f, -5f), Restart);
        CreateButton("MainMenu", panel.transform, "MAIN MENU", new Vector2(0f, -95f), GoToMainMenu);
    }

    private void Restart()
    {
        RestartController restartController = FindAnyObjectByType<RestartController>();
        if (restartController != null)
        {
            restartController.RestartNow();
            return;
        }

        SceneLoader sceneLoader = FindAnyObjectByType<SceneLoader>();
        if (sceneLoader != null)
            sceneLoader.ReloadCurrentScene();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    private void GoToMainMenu()
    {
        SceneLoader sceneLoader = FindAnyObjectByType<SceneLoader>();
        if (sceneLoader != null)
            sceneLoader.LoadMainMenu();
        else
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private void SetVisible(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Font GetVictoryMenuFont()
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
        rect.sizeDelta = new Vector2(280f, 42f);

        Text text = textObject.AddComponent<Text>();
        text.font = GetVictoryMenuFont();
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
