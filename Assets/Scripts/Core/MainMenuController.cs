/*
MainMenuController.cs

Purpose:
- Handle main menu UI interactions and delegate scene loading to SceneLoader.
- Exists only as a thin UI bridge; no loading logic lives here.

Design decisions:
- Delegates all scene loading to SceneLoader to avoid duplicated SceneManager calls.
- SceneLoader is expected to be on the same GameObject or anywhere in the MainMenu scene.
- No singleton, no persistence, no save data.

Usage:
- Attach to Canvas or any menu root.
- Assign SceneLoader reference in Inspector (auto-found on Start if not assigned).
- Wire button onClick to PlayLevel3().

Future levels:
- Add PlayLevel4() etc. here and wire to new buttons. Each calls sceneLoader.LoadScene with the appropriate name.
  Alternatively move to a general LoadLevel(string name) approach with buttons passing the name via UnityEvent.
*/

using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField, Tooltip("SceneLoader in this scene. Auto-found if not assigned.")]
    private SceneLoader sceneLoader = null;

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackGameplaySceneName = "Level3";

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackSlidingPuzzleSceneName = "SlidingPuzzle4x4";

    [SerializeField, Tooltip("Fallback scene name used if SceneLoader is not present.")]
    private string fallbackLongestPathPuzzleSceneName = "LongestPathPuzzle";

    private void Start()
    {
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();
        // Not an error if missing — PlayLevel3 has a direct fallback.
    }

    /// <summary>
    /// Called by the Play button. Loads the gameplay scene.
    /// Uses SceneLoader if available; falls back to direct SceneManager call.
    /// </summary>
    public void PlayLevel3()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadGameplay();
        }
        else
        {
            // Direct fallback so the button works even without SceneLoader in scene.
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackGameplaySceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the platformer button. Loads the platformer gameplay scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadPlatformer()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadPlatformer();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Level4", LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the sliding puzzle button. Loads the 4x4 sliding puzzle scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadSlidingPuzzle4x4()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadSlidingPuzzle4x4();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackSlidingPuzzleSceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Called by the longest-path button. Loads the road puzzle scene.
    /// Uses SceneLoader if available; falls back to a direct SceneManager call.
    /// </summary>
    public void LoadLongestPathPuzzle()
    {
        if (sceneLoader != null)
        {
            sceneLoader.LoadLongestPathPuzzle();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(fallbackLongestPathPuzzleSceneName, LoadSceneMode.Single);
        }
    }
}
