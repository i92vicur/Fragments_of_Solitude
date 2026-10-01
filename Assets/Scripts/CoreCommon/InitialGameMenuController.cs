/*
InitialGameMenuController.cs

Purpose:
- Controls the initial game menu controller flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class InitialGameMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private GameObject crystalLeft;
    [SerializeField] private GameObject crystalRight;
    [SerializeField] private Sprite crystalLeftSprite;
    [SerializeField] private Sprite crystalRightSprite;
    [SerializeField] private RuntimeAnimatorController crystalAnimatorController;
    [SerializeField] private float crystalHorizontalOffset = 110f;
    [SerializeField] private float crystalVerticalOffset = 0f;

    private SceneLoader sceneLoader;
    private Canvas canvas;
    private RectTransform crystalLeftRect;
    private RectTransform crystalRightRect;
    private Image crystalLeftImage;
    private Image crystalRightImage;

    private const string Level0SceneName = "Level0";
    private const string MainMenuSceneName = "MainMenu";

    private void Awake()
    {
        CacheReferences();
        EnsureHoverCrystalSet();
        BindHoverCrystalEvents();
        UpdateLoadGameButtonState();
    }

    private void OnEnable()
    {
        CacheReferences();
        EnsureHoverCrystalSet();
        WireButtons();
        BindHoverCrystalEvents();
        UpdateLoadGameButtonState();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    private void CacheReferences()
    {
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>();

        if (newGameButton == null)
            newGameButton = transform.Find("NewGameButton")?.GetComponent<Button>();

        if (loadGameButton == null)
            loadGameButton = transform.Find("LoadGameButton")?.GetComponent<Button>();
    }

    private void EnsureHoverCrystalSet()
    {
        if (crystalLeft != null)
            EnsureUiCrystal(crystalLeft, true);

        if (crystalRight != null)
            EnsureUiCrystal(crystalRight, false);

        if (crystalLeft != null && crystalRight != null)
            return;

        Transform crystalRoot = transform.Find("HoverCrystalRoot");
        if (crystalRoot == null)
        {
            GameObject crystalRootGO = new GameObject("HoverCrystalRoot");
            crystalRootGO.transform.SetParent(transform, false);
            crystalRoot = crystalRootGO.transform;
        }

        crystalLeft = CreateCrystal(crystalRoot, "Crystal_Left", true);
        crystalRight = CreateCrystal(crystalRoot, "Crystal_Right", false);
        crystalLeft.SetActive(false);
        crystalRight.SetActive(false);
    }

    private void EnsureUiCrystal(GameObject crystalObject, bool isLeftCrystal)
    {
        if (crystalObject == null)
            return;

        if (canvas != null && crystalObject.transform.parent != canvas.transform)
            crystalObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = crystalObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = crystalObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(28f, 68f);
        rect.anchoredPosition = Vector2.zero;

        Image image = crystalObject.GetComponent<Image>();
        if (image == null)
            image = crystalObject.AddComponent<Image>();

        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = Color.white;

        SpriteRenderer spriteRenderer = crystalObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (image.sprite == null)
                image.sprite = spriteRenderer.sprite;

            if (Application.isPlaying)
                Destroy(spriteRenderer);
            else
                DestroyImmediate(spriteRenderer);
        }

        Animator animator = crystalObject.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
            animator.runtimeAnimatorController = GetCrystalController(isLeftCrystal);

        if (isLeftCrystal)
            crystalLeftRect = rect;
        else
            crystalRightRect = rect;

        if (isLeftCrystal)
            crystalLeftImage = image;
        else
            crystalRightImage = image;
    }

    private GameObject CreateCrystal(Transform parent, string name, bool isLeftCrystal)
    {
        GameObject crystalGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
        crystalGO.transform.SetParent(parent, false);

        RectTransform rect = crystalGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(28f, 68f);
        rect.anchoredPosition = Vector2.zero;

        Image image = crystalGO.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.sprite = GetCrystalSprite(isLeftCrystal);
        image.color = Color.white;

        Animator animator = crystalGO.GetComponent<Animator>();
        animator.runtimeAnimatorController = GetCrystalController(isLeftCrystal);
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.enabled = animator.runtimeAnimatorController != null;

        if (isLeftCrystal)
            crystalLeftRect = rect;
        else
            crystalRightRect = rect;

        if (isLeftCrystal)
            crystalLeftImage = image;
        else
            crystalRightImage = image;

        return crystalGO;
    }

    private RuntimeAnimatorController GetCrystalController(bool isLeftCrystal)
    {
        if (crystalAnimatorController != null)
            return crystalAnimatorController;

        const string path = "Assets/Art/UI/HUD/Initial/CrystalSpinning.controller";

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
#else
        return null;
#endif
    }

    private Sprite GetCrystalSprite(bool isLeftCrystal)
    {
        if (isLeftCrystal && crystalLeftSprite != null)
            return crystalLeftSprite;

        if (!isLeftCrystal && crystalRightSprite != null)
            return crystalRightSprite;

#if UNITY_EDITOR
        string path = "Assets/Art/UI/HUD/Main Menu/crystal_dark_64x64_24f_20d.png";
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .ToArray();

        if (sprites.Length == 0)
            return null;

        return sprites[0];
#else
        return null;
#endif
    }

    private void BindHoverCrystalEvents()
    {
        List<Button> buttons = new List<Button>();
        if (newGameButton != null)
            buttons.Add(newGameButton);
        if (loadGameButton != null)
            buttons.Add(loadGameButton);

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            RemovePointerTrigger(trigger, EventTriggerType.PointerEnter);
            RemovePointerTrigger(trigger, EventTriggerType.PointerExit);

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowCrystalsForButton(button));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideCrystals());
            trigger.triggers.Add(exit);
        }
    }

    private static void RemovePointerTrigger(EventTrigger trigger, EventTriggerType eventType)
    {
        if (trigger == null)
            return;

        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            if (trigger.triggers[i].eventID == eventType)
                trigger.triggers.RemoveAt(i);
        }
    }

    private void ShowCrystalsForButton(Button button)
    {
        if (button == null || crystalLeft == null || crystalRight == null)
            return;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
            return;

        float horizontalOffset = Mathf.Max(crystalHorizontalOffset, buttonRect.rect.width * 0.75f);
        Vector2 buttonCenter = buttonRect.anchoredPosition;

        if (crystalLeftRect != null)
        {
            crystalLeftRect.anchoredPosition = new Vector2(buttonCenter.x - horizontalOffset, buttonCenter.y + crystalVerticalOffset);
            crystalLeftRect.SetAsLastSibling();
        }

        if (crystalRightRect != null)
        {
            crystalRightRect.anchoredPosition = new Vector2(buttonCenter.x + horizontalOffset, buttonCenter.y + crystalVerticalOffset);
            crystalRightRect.SetAsLastSibling();
        }

        crystalLeft.SetActive(true);
        crystalRight.SetActive(true);
    }

    private void HideCrystals()
    {
        if (crystalLeft != null)
            crystalLeft.SetActive(false);

        if (crystalRight != null)
            crystalRight.SetActive(false);
    }

    private void WireButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(HandleNewGameClicked);
            newGameButton.onClick.AddListener(HandleNewGameClicked);
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.RemoveListener(HandleLoadGameClicked);
            loadGameButton.onClick.AddListener(HandleLoadGameClicked);
        }
    }

    private void UnwireButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(HandleNewGameClicked);

        if (loadGameButton != null)
            loadGameButton.onClick.RemoveListener(HandleLoadGameClicked);
    }

    private void UpdateLoadGameButtonState()
    {
        if (loadGameButton == null)
            return;

        loadGameButton.interactable = HasAnyProgress();
    }

    private static bool HasAnyProgress()
    {
        GameProgressManager progressManager = GameProgressManager.Instance;
        return progressManager.SlidingPuzzleCompleted
            || progressManager.LongestPathCompleted
            || progressManager.ConstraintPuzzleCompleted
            || progressManager.Level3Completed
            || progressManager.Level4Completed
            || progressManager.Level5Completed;
    }

    public void HandleNewGameClicked()
    {
        GameProgressManager.Instance.StartNewGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene(Level0SceneName, LoadSceneMode.Single);
    }

    public void HandleLoadGameClicked()
    {
        Time.timeScale = 1f;

        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (sceneLoader != null)
        {
            sceneLoader.LoadMainMenu();
            return;
        }

        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
    }
}