using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InitialGameMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;

    private SceneLoader sceneLoader;

    private const string Level0SceneName = "Level0";
    private const string MainMenuSceneName = "MainMenu";

    private void Awake()
    {
        CacheReferences();
        UpdateLoadGameButtonState();
    }

    private void OnEnable()
    {
        CacheReferences();
        WireButtons();
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

        if (newGameButton == null)
            newGameButton = transform.Find("NewGameButton")?.GetComponent<Button>();

        if (loadGameButton == null)
            loadGameButton = transform.Find("LoadGameButton")?.GetComponent<Button>();
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