/*
RestartController.cs

Purpose and design (minimal restart foundation):
- Provide a small, explicit way to return the game to a clean starting state during Play Mode for prototype testing.
- Delegates the actual scene reload to SceneLoader so there is no duplicated loading logic.
- This controller is responsible only for reading the keyboard shortcut and calling SceneLoader.ReloadCurrentScene().
- This input logic is explicitly temporary and should be removed when a proper UI button is implemented.

Usage:
- Attach this component to the same GameObject as SceneLoader (or any scene object).
- During Play Mode press the configured key (default: R) to restart the active scene.
- The implementation is intentionally small and safe to remove or replace later.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class RestartController : MonoBehaviour
{
    [Header("Temporary testing settings (remove when UI exists)")]
    [SerializeField, Tooltip("Key used to trigger an immediate restart in Play Mode (temporary).")]
    private KeyCode restartKey = KeyCode.R;

    [SerializeField, Tooltip("If true, pressing the key will restart even while Time.timeScale == 0.")]
    private bool allowWhilePaused = true;

    [SerializeField, Tooltip("SceneLoader used to perform the reload. Must be in the same scene.")]
    private SceneLoader sceneLoader = null;

    private bool isRestarting = false;

    private void OnValidate()
    {
        if (restartKey == KeyCode.None) restartKey = KeyCode.R;
    }

    private void Start()
    {
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (sceneLoader == null)
            Debug.LogError("[RestartController] SceneLoader not found. Assign it in the Inspector.", this);
    }

    private void Update()
    {
        if (isRestarting) return;

        bool shouldTrigger = allowWhilePaused
            ? Input.GetKeyDown(restartKey)
            : Time.timeScale > 0f && Input.GetKeyDown(restartKey);

        if (shouldTrigger)
            RestartNow();
    }

    /// <summary>
    /// Delegate reload to SceneLoader. Called by keyboard shortcut or UI button.
    /// </summary>
    public void RestartNow()
    {
        if (isRestarting) return;
        isRestarting = true;

        if (sceneLoader != null)
            sceneLoader.ReloadCurrentScene();
        else
        {
            // Minimal fallback if SceneLoader is missing.
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    [ContextMenu("Restart Now (Editor)")]
    private void ContextRestart() => RestartNow();
}
