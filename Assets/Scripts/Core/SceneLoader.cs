/*
SceneLoader.cs

Purpose:
- Centralise all scene-transition calls in one small, reusable component.
- Expose public methods suitable for direct wiring to UI Button onClick events.
- Replace the scene-loading code scattered across MainMenuController and RestartController.

Design decisions:
- No singleton. The component is placed on any convenient scene object and referenced by UI or
  other components in the same scene. This avoids global state and keeps the prototype simple.
- Scene names are configurable Inspector fields (not hard-coded strings in multiple scripts).
  Changing a scene name therefore only requires updating the Inspector, not hunting across files.
- No async loading or loading screens: a single synchronous LoadScene call is sufficient for
  a small prototype where levels are tiny and load in milliseconds.
- Time.timeScale is always reset to 1 before loading. This prevents the game staying paused
  if the player restarts from a paused or death state.

Functionality:
- LoadGameplay()           – Loads the gameplay scene (Main). Called by the Play button in MainMenu.
- LoadMainMenu()           – Loads the main menu scene. Called by UI buttons on death, victory, or pause.
- ReloadCurrentScene()     – Reloads the active scene. Called by the restart shortcut (RestartController)
                             or a retry button.

Configuration:
- Gameplay Scene Name      – Must match the scene name exactly (default: "Main").
- Main Menu Scene Name     – Must match the scene name exactly (default: "MainMenu").

Usage:
- Add this component to a scene root object (e.g. "SceneLoader" or "Core").
- Wire UI Button onClick events to call the desired method.
- RestartController calls ReloadCurrentScene() instead of duplicating reload logic.

Future levels:
- To add Level 4, add a new [SerializeField] string level4SceneName and a LoadLevel4() method.
  Wire a new button to it. No other changes needed.

Known limitations:
- Scene names must be registered in Build Settings or loading will silently fail with an error.
- No transition effects. Add a crossfade or fade-to-black layer here if desired later.
*/

using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names (must match Build Settings exactly)")]
    [SerializeField] private string gameplaySceneName = "Level3";
    [SerializeField] private string platformerSceneName = "Level4";
    [SerializeField] private string phase2SceneName = "Level3_Phase2";
    [SerializeField] private string puzzleMenuSceneName = "PuzzleMenu";
    [SerializeField] private string slidingPuzzle4x4SceneName = "SlidingPuzzle4x4";
    [SerializeField] private string longestPathPuzzleSceneName = "LongestPathPuzzle";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // -------------------------------------------------------------------------
    // Public methods - wire directly to UI Button onClick events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Load the main gameplay scene.
    /// Called by: Play button in MainMenu.
    /// </summary>
    public void LoadGameplay()
    {
        LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Load the Phase 2 roguelite continuation scene.
    /// Used by the Level 3 staircase trigger when the player reaches level 6.
    /// </summary>
    public void LoadPhase2()
    {
        LoadScene(phase2SceneName);
    }

    /// <summary>
    /// Allows a scene object to set the phase-2 target without duplicating the scene name elsewhere.
    /// </summary>
    public void SetPhase2SceneName(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            phase2SceneName = sceneName;
        }
    }

    /// <summary>
    /// Backwards-compatible name used by the staircase trigger.
    /// </summary>
    public void SetSceneNameForPhase2(string sceneName)
    {
        SetPhase2SceneName(sceneName);
    }

    /// <summary>
    /// Load the platformer gameplay scene.
    /// Called by: Platformer button in MainMenu.
    /// </summary>
    public void LoadPlatformer()
    {
        LoadScene(platformerSceneName);
    }

    /// <summary>
    /// Load the puzzle-selector scene.
    /// Called by: Main menu or puzzle exits when the player should choose another minigame.
    /// </summary>
    public void LoadPuzzleMenu()
    {
        LoadScene(puzzleMenuSceneName);
    }

    /// <summary>
    /// Load the first sliding puzzle minigame scene.
    /// Called by: Sliding Puzzle 4x4 button in MainMenu.
    /// </summary>
    public void LoadSlidingPuzzle4x4()
    {
        LoadScene(slidingPuzzle4x4SceneName);
    }

    /// <summary>
    /// Load the longest-path road puzzle scene.
    /// Called by: Longest Path button in MainMenu.
    /// </summary>
    public void LoadLongestPathPuzzle()
    {
      LoadScene(longestPathPuzzleSceneName);
    }

    /// <summary>
    /// Load the main menu scene.
    /// Called by: Pause menu "Quit to Menu" button, death/victory screen "Back to Menu" button.
    /// </summary>
    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Reload the currently active scene.
    /// Called by: Restart button, RestartController keyboard shortcut.
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene name is empty. Assign it in the Inspector.", this);
            return;
        }

        // Always restore timescale before transitioning so the next scene starts unpaused.
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void LoadScene(int buildIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }
}
