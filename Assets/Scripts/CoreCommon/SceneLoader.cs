/*
SceneLoader.cs

Purpose:
- Controls the scene loader flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names (must match Build Settings exactly)")]
    [SerializeField] private string gameplaySceneName = "Level2";
    [SerializeField] private string platformerSceneName = "Level4";
    [SerializeField] private string level5SceneName = "Level5";
    [SerializeField] private string phase2SceneName = "Level2_Phase2";
    [SerializeField] private string puzzleMenuSceneName = "PuzzleMenu";
    [SerializeField] private string slidingPuzzle4x4SceneName = "SlidingPuzzle4x4";
    [SerializeField] private string longestPathPuzzleSceneName = "LongestPathPuzzle";
    [SerializeField] private string constraintPuzzleSceneName = "ConstraintPuzzle_New";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

        // -------------------------------------------------------------------------
        // Public methods - wire directly to UI Button onClick events
        // -------------------------------------------------------------------------
        // Level 2 Phase 2 transition is handled here.

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
    /// Used by the Level 2 staircase trigger when the player reaches level 6.
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
    /// Load the Level 5 turn-based battle scene.
    /// Called by: Level 5 button in MainMenu.
    /// </summary>
    public void LoadLevel5()
    {
        LoadScene(level5SceneName);
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
    /// Load the constraint-placement puzzle scene.
    /// </summary>
    public void LoadConstraintPuzzle()
    {
        LoadScene(constraintPuzzleSceneName);
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
