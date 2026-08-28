using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PuzzleMenuController : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;

    private const string TitleText = "PUZZLE MENU";

    private void Awake()
    {
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        BuildMenu();
    }

    private void BuildMenu()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        Transform existingRoot = transform.Find("PuzzleMenuRoot");
        if (existingRoot != null)
            Destroy(existingRoot.gameObject);

        GameObject rootGO = new GameObject("PuzzleMenuRoot", typeof(RectTransform));
        rootGO.transform.SetParent(transform, false);

        RectTransform rootRect = rootGO.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CreateTitle(rootGO.transform);
        CreateButton(rootGO.transform, "SlidingPuzzleButton", "Sliding Puzzle 4x4", new Vector2(0f, 40f), LoadSlidingPuzzleScene);
        CreateButton(rootGO.transform, "LongestPathButton", "Longest Path Puzzle", new Vector2(0f, -60f), LoadLongestPathScene);
        CreateButton(rootGO.transform, "BackToMainMenuButton", "Back to Main Menu", new Vector2(0f, -160f), LoadMainMenuScene);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(parent, false);

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(600f, 80f);

        Text titleText = titleGO.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = TitleText;
    }

    private void CreateButton(Transform parent, string buttonName, string labelText, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonGO = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 72f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(callback);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGO.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = labelText;
        label.raycastTarget = false;
    }

    private void LoadSlidingPuzzleScene()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadSlidingPuzzle4x4();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("SlidingPuzzle4x4", LoadSceneMode.Single);
    }

    private void LoadLongestPathScene()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadLongestPathPuzzle();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("LongestPathPuzzle", LoadSceneMode.Single);
    }

    private void LoadMainMenuScene()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadMainMenu();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
