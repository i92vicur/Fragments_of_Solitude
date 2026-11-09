/*
PuzzleMenuController.cs

Purpose:
- Controls the puzzle menu controller flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

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
public class PuzzleMenuController : MonoBehaviour
{
    // Estático para sobrevivir a la carga de un puzzle y volver a este menú; se reinicia al cerrar el juego.
    private static int lastSelectedIndex;

    [SerializeField] private SceneLoader sceneLoader;

    [Header("Button Colors")]
    [SerializeField] private Color slidingPuzzleButtonColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color longestPathButtonColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color constraintPuzzleButtonColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color backToMainMenuButtonColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Header("Button Border")]
    [SerializeField] private Color buttonBorderColor = Color.white;
    [SerializeField] private float buttonBorderWidth = 3f;

    private const string TitleText = "PUZZLE MENU";

    private readonly List<Button> menuButtons = new List<Button>();
    private readonly GamepadHoverBridge gamepadHover = new GamepadHoverBridge();
    private bool initialSelectionDone;
    private bool navLatched;

    private void Awake()
    {
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        BuildMenu();

        GamepadMenuNavigation.EnsureGameInput();
        GameInput.SetContext(GameInputContext.Menu);

        // El StandaloneInputModule de la escena también leería el stick y dispararía Submit; lo desactivamos como en MainMenuController.
        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es != null)
            es.sendNavigationEvents = false;
    }

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null || menuButtons.Count == 0)
            return;

        if (!initialSelectionDone)
        {
            initialSelectionDone = true;
            int index = Mathf.Clamp(lastSelectedIndex, 0, menuButtons.Count - 1);
            es.SetSelectedGameObject(menuButtons[index].gameObject);
        }

        if (GameInput.Cancel.Pressed)
        {
            LoadMainMenuScene();
            return;
        }

        Button selected = null;
        // Sin selección y sin navegación (p. ej. clic en zona vacía) no se reselecciona automáticamente.
        if (es.currentSelectedGameObject != null || GamepadMenuNavigation.NavigateActive)
            selected = GamepadMenuNavigation.UpdateSelection(es, menuButtons, ref navLatched);

        if (selected != null)
            lastSelectedIndex = menuButtons.IndexOf(selected);

        if (selected != null && GameInput.Submit.Pressed)
            GamepadMenuNavigation.Submit(es, selected);

        gamepadHover.Tick(es, selected);
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

        menuButtons.Clear();

        GameObject rootGO = new GameObject("PuzzleMenuRoot", typeof(RectTransform));
        rootGO.transform.SetParent(transform, false);

        RectTransform rootRect = rootGO.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CreateTitle(rootGO.transform);
        CreateButton(rootGO.transform, "SlidingPuzzleButton", "Sliding Puzzle 4x4", new Vector2(0f, 90f), slidingPuzzleButtonColor, LoadSlidingPuzzleScene);
        CreateButton(rootGO.transform, "LongestPathButton", "Longest Path Puzzle", new Vector2(0f, -10f), longestPathButtonColor, LoadLongestPathScene);
        CreateButton(rootGO.transform, "ConstraintPuzzleButton", "Constraint Puzzle", new Vector2(0f, -110f), constraintPuzzleButtonColor, LoadConstraintPuzzleScene);
        CreateButton(rootGO.transform, "BackToMainMenuButton", "Back to Main Menu", new Vector2(0f, -240f), backToMainMenuButtonColor, LoadMainMenuScene);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(parent, false);

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -100f);
        titleRect.sizeDelta = new Vector2(600f, 80f);

        Text titleText = titleGO.GetComponent<Text>();
        titleText.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        titleText.fontSize = 37;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = TitleText;
    }

    private static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.1f;
        return colors;
    }

    private void CreateButton(Transform parent, string buttonName, string labelText, Vector2 anchoredPosition, Color backgroundColor, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonGO = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 70f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGO.GetComponent<Image>();
        image.sprite = RoundedSpriteFactory.Get();
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;

        Outline outline = buttonGO.AddComponent<Outline>();
        outline.effectColor = buttonBorderColor;
        outline.effectDistance = new Vector2(buttonBorderWidth, buttonBorderWidth);

        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors();
        button.onClick.AddListener(callback);
        menuButtons.Add(button);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(buttonGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGO.GetComponent<Text>();
        label.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        label.fontSize = 17;
        label.fontStyle = FontStyle.Normal;
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

    private void LoadConstraintPuzzleScene()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadConstraintPuzzle();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("ConstraintPuzzle", LoadSceneMode.Single);
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
