/*
RestartController.cs

Purpose:
- Controls the restart controller flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
