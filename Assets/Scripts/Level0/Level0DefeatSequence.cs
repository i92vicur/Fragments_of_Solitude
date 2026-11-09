/*
Level0DefeatSequence.cs

Purpose:
- Controls the level0 defeat sequence flow for the opening defeat and intro sequence logic.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Level0DefeatSequence : MonoBehaviour
{
    [SerializeField] private Level5BattleController battleController;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private string gameOverTitle = "GAME OVER";
    [SerializeField, TextArea] private string gameOverText = "This was never the end. Something has already begun beneath the silence, waiting to be uncovered. The path ahead will not be easy, and not everything lost is truly gone. What seemed like defeat was only the first step of a journey that has yet to reveal itself.";

    private bool sequenceStarted;
    private CanvasGroup fadeCanvasGroup;
    private GameObject gameOverPanel;
    private Text titleText;
    private Text bodyText;
    private Button startButton;

    private static readonly int DeadStateHash = Animator.StringToHash("Dead");

    // Safety limit so a missing or stalled Dead state can never block the sequence.
    private const float DeathAnimationTimeout = 3f;

    private void Awake()
    {
        if (battleController == null)
            battleController = FindAnyObjectByType<Level5BattleController>();

        EnsureUiExists();
        SetUiHiddenImmediate();
    }

    private void Start()
    {
        if (battleController == null)
            battleController = FindAnyObjectByType<Level5BattleController>();

        SetUiHiddenImmediate();
    }

    private void OnEnable()
    {
        if (battleController == null)
            battleController = FindAnyObjectByType<Level5BattleController>();

        if (battleController != null)
        {
            battleController.OnBattleEnded += HandleBattleEnded;
            Debug.Log("Level0DefeatSequence subscribed to Level5BattleController.OnBattleEnded");
        }
    }

    private void OnDisable()
    {
        if (battleController != null)
            battleController.OnBattleEnded -= HandleBattleEnded;
    }

    private void HandleBattleEnded(string result)
    {
        Debug.Log($"Level0DefeatSequence received battle result: {result}");

        if (sequenceStarted || battleController == null || !battleController.IsLevel0BossTurnModeActive)
        {
            Debug.Log($"Level0DefeatSequence ignored result '{result}' because sequenceStarted={sequenceStarted}, battleControllerNull={battleController == null}, level0Mode={battleController != null && battleController.IsLevel0BossTurnModeActive}");
            return;
        }

        if (!string.Equals(result, "DEFEAT", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Level0DefeatSequence rejected result '{result}' because it was not DEFEAT.");
            return;
        }

        sequenceStarted = true;
        Debug.Log("Level0DefeatSequence starting Game Over fade");
        StartCoroutine(RunDefeatSequence());
    }

    private static Font GetFont()
    {
        Font font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private IEnumerator RunDefeatSequence()
    {
        yield return WaitForDeathAnimations();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        if (gameOverPanel != null)
        {
            var panelImage = gameOverPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 1f);

            gameOverPanel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = gameOverTitle;
            titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, 1f);
            titleText.gameObject.SetActive(true);
        }

        if (bodyText != null)
        {
            bodyText.text = gameOverText;
            bodyText.color = new Color(bodyText.color.r, bodyText.color.g, bodyText.color.b, 1f);
            bodyText.gameObject.SetActive(true);
        }

        if (startButton != null)
        {
            var buttonImage = startButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);

            var buttonLabel = startButton.transform.Find("Label")?.GetComponent<Text>();
            if (buttonLabel != null)
                buttonLabel.color = new Color(buttonLabel.color.r, buttonLabel.color.g, buttonLabel.color.b, 1f);

            startButton.gameObject.SetActive(true);
        }
    }

    // Dead is triggered per character by Combatant.OnDied; this only waits for those animations to finish.
    private IEnumerator WaitForDeathAnimations()
    {
        List<Animator> animators = ResolvePlayerAnimators();
        if (animators.Count == 0)
        {
            Debug.LogWarning("Level0DefeatSequence: no valid player Animators found; skipping death animation wait.", this);
            yield break;
        }

        float elapsed = 0f;
        while (true)
        {
            bool allFinished = true;
            foreach (Animator animator in animators)
            {
                if (!IsDeathFinished(animator))
                {
                    allFinished = false;
                    break;
                }
            }

            if (allFinished)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= DeathAnimationTimeout)
            {
                Debug.LogWarning($"Level0DefeatSequence: death animations did not finish within {DeathAnimationTimeout}s; continuing.", this);
                yield break;
            }

            yield return null;
        }
    }

    private static List<Animator> ResolvePlayerAnimators()
    {
        var animators = new List<Animator>();

        foreach (Combatant combatant in FindObjectsByType<Combatant>(FindObjectsSortMode.None))
        {
            if (combatant.IsEnemy)
                continue;

            Animator animator = combatant.Animator;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"Level0DefeatSequence: '{combatant.CombatantName}' has no Animator with a controller.", combatant);
                continue;
            }

            animators.Add(animator);
        }

        return animators;
    }

    // A disabled Animator or one that never reached Dead cannot finish, so it must not block the sequence.
    private static bool IsDeathFinished(Animator animator)
    {
        if (!animator.isActiveAndEnabled)
            return true;

        if (!IsInDeadState(animator))
            return false;

        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
    }

    private static bool IsInDeadState(Animator animator)
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash == DeadStateHash && !animator.IsInTransition(0);
    }

    private void EnsureUiExists()
    {
        Canvas targetCanvas = GameObject.Find("BattleCanvas")?.GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("BattleCanvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (targetCanvas.transform.Find("Level0_Defeat_Fade") == null)
        {
            GameObject fadeGO = new GameObject("Level0_Defeat_Fade");
            fadeGO.transform.SetParent(targetCanvas.transform, false);
            RectTransform fadeRT = fadeGO.AddComponent<RectTransform>();
            fadeRT.anchorMin = Vector2.zero;
            fadeRT.anchorMax = Vector2.one;
            fadeRT.offsetMin = Vector2.zero;
            fadeRT.offsetMax = Vector2.zero;

            Image fadeImage = fadeGO.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);

            fadeCanvasGroup = fadeGO.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
        else
        {
            fadeCanvasGroup = targetCanvas.transform.Find("Level0_Defeat_Fade").GetComponent<CanvasGroup>();
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
                fadeCanvasGroup.interactable = false;
            }
        }

        if (targetCanvas.transform.Find("Level0_GameOver_Panel") == null)
        {
            gameOverPanel = new GameObject("Level0_GameOver_Panel");
            gameOverPanel.transform.SetParent(targetCanvas.transform, false);
            RectTransform panelRT = gameOverPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(1400f, 660f);
            panelRT.anchoredPosition = Vector2.zero;

            Image panelImage = gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0f);

            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(gameOverPanel.transform, false);
            RectTransform titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -50f);
            titleRT.sizeDelta = new Vector2(500f, 120f);
            titleText = titleGO.AddComponent<Text>();
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.2f, 0.2f, 0f);
            titleText.font = GetFont();
            titleText.fontSize = 54;
            titleText.text = gameOverTitle;

            GameObject bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(gameOverPanel.transform, false);
            RectTransform bodyRT = bodyGO.AddComponent<RectTransform>();
            bodyRT.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRT.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRT.pivot = new Vector2(0.5f, 0.5f);
            bodyRT.anchoredPosition = new Vector2(0f, 0f);
            bodyRT.sizeDelta = new Vector2(980f, 220f);
            bodyText = bodyGO.AddComponent<Text>();
            bodyText.alignment = TextAnchor.MiddleCenter;
            bodyText.color = new Color(1f, 1f, 1f, 0f);
            bodyText.font = GetFont();
            bodyText.fontSize = 26;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.text = gameOverText;

            GameObject buttonGO = new GameObject("StartButton");
            buttonGO.transform.SetParent(gameOverPanel.transform, false);
            RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0.5f, 0f);
            buttonRT.anchorMax = new Vector2(0.5f, 0f);
            buttonRT.pivot = new Vector2(0.5f, 0f);
            buttonRT.anchoredPosition = new Vector2(0f, 30f);
            buttonRT.sizeDelta = new Vector2(220f, 64f);

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.18f, 0.18f, 0f);
            startButton = buttonGO.AddComponent<Button>();
            startButton.onClick.AddListener(OnStartPressed);

            GameObject buttonLabel = new GameObject("Label");
            buttonLabel.transform.SetParent(buttonGO.transform, false);
            RectTransform labelRT = buttonLabel.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            Text labelText = buttonLabel.AddComponent<Text>();
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(1f, 1f, 1f, 0f);
            labelText.font = GetFont();
            labelText.fontSize = 28;
            labelText.text = "START";

            gameOverPanel.SetActive(false);
        }
        else
        {
            gameOverPanel = targetCanvas.transform.Find("Level0_GameOver_Panel").gameObject;
            var panelImage = gameOverPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0f, 0f, 0f, 0f);

            titleText = gameOverPanel.transform.Find("Title")?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.color = new Color(1f, 0.2f, 0.2f, 0f);
                titleText.text = gameOverTitle;
            }

            bodyText = gameOverPanel.transform.Find("Body")?.GetComponent<Text>();
            if (bodyText != null)
            {
                bodyText.color = new Color(1f, 1f, 1f, 0f);
                bodyText.text = gameOverText;
            }

            startButton = gameOverPanel.transform.Find("StartButton")?.GetComponent<Button>();
            if (startButton != null)
                startButton.gameObject.SetActive(false);

            gameOverPanel.SetActive(false);
        }
    }

    private void SetUiHiddenImmediate()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (titleText != null)
            titleText.gameObject.SetActive(false);

        if (bodyText != null)
            bodyText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(false);
    }

    private void OnStartPressed()
    {
        var loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadMainMenu();
            return;
        }

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
