/*
Level5ResultPanel.cs

Purpose:
- Shows the Level 5 battle result panel (VICTORY or DEFEAT) with its navigation buttons.

Design decisions:
- Reacts to Level5BattleController.OnBattleEnded and only accepts the exact "VICTORY!" and "DEFEAT" values.
- Panels and buttons are scene objects; this script only switches them and wires button listeners.
- Does not pause the game or change Time.timeScale.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Level5ResultPanel : MonoBehaviour
{
    private const string VictoryResult = "VICTORY!";
    private const string DefeatResult = "DEFEAT";

    [SerializeField] private Level5BattleController battleController;
    [SerializeField] private RestartController restartController;
    [SerializeField] private SceneLoader sceneLoader;

    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Button victoryMainMenuButton;
    [SerializeField] private Button defeatRetryButton;
    [SerializeField] private Button defeatMainMenuButton;

    private bool shown;
    private bool leaving;
    private bool navLatched;
    private readonly List<Button> activeButtons = new List<Button>();

    private void Awake()
    {
        if (battleController == null || restartController == null || sceneLoader == null ||
            victoryPanel == null || defeatPanel == null ||
            victoryMainMenuButton == null || defeatRetryButton == null || defeatMainMenuButton == null)
        {
            Debug.LogError("[Level5ResultPanel] Missing references. Assign them in the Inspector.", this);
            enabled = false;
            return;
        }

        GamepadMenuNavigation.EnsureGameInput();

        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);

        victoryMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        defeatRetryButton.onClick.AddListener(Retry);
        defeatMainMenuButton.onClick.AddListener(ReturnToMainMenu);

        GamepadMenuNavigation.ApplySelectionStyle(victoryMainMenuButton);
        GamepadMenuNavigation.ApplySelectionStyle(defeatRetryButton);
        GamepadMenuNavigation.ApplySelectionStyle(defeatMainMenuButton);
    }

    private void OnEnable()
    {
        if (battleController == null) return;

        battleController.OnBattleEnded -= HandleBattleEnded;
        battleController.OnBattleEnded += HandleBattleEnded;
    }

    private void OnDisable()
    {
        if (battleController != null)
            battleController.OnBattleEnded -= HandleBattleEnded;
    }

    private void HandleBattleEnded(string result)
    {
        if (shown) return;

        switch (result)
        {
            case VictoryResult:
                ShowResult(victory: true);
                break;
            case DefeatResult:
                ShowResult(victory: false);
                break;
            default:
                Debug.LogWarning($"[Level5ResultPanel] Unexpected battle result '{result}'.", this);
                break;
        }
    }

    private void ShowResult(bool victory)
    {
        shown = true;

        GameObject panel = victory ? victoryPanel : defeatPanel;
        // Draws above sibling UI, including the runtime PAUSE button.
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);

        navLatched = false;
        GamepadMenuNavigation.ClearSelection(EventSystem.current);
        if (GameInput.IsInitialized)
            GameInput.SetContext(GameInputContext.Menu);
    }

    private void Update()
    {
        if (!shown || leaving) return;

        activeButtons.Clear();
        if (victoryPanel.activeSelf)
        {
            activeButtons.Add(victoryMainMenuButton);
        }
        else if (defeatPanel.activeSelf)
        {
            activeButtons.Add(defeatRetryButton);
            activeButtons.Add(defeatMainMenuButton);
        }

        EventSystem es = EventSystem.current;
        Button selected = GamepadMenuNavigation.UpdateSelection(es, activeButtons, ref navLatched);
        if (GameInput.IsInitialized && GameInput.Submit.Pressed && selected != null)
            GamepadMenuNavigation.Submit(es, selected);
    }

    private void Retry()
    {
        if (leaving) return;
        leaving = true;
        restartController.RestartNow();
    }

    private void ReturnToMainMenu()
    {
        if (leaving) return;
        leaving = true;
        sceneLoader.LoadMainMenu();
    }

}
