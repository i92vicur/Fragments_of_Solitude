/*
PlatformerFallRestart.cs

Purpose: Restart the active platformer scene when the monitored player falls below a configurable Y limit.

Design decisions:
- A threshold check works for gaps and for falls outside the camera without requiring a trigger collider.
- A one-shot guard prevents repeated reload requests while the scene is being replaced.
- SceneLoader owns the normal reload path; time is restored and a direct scene reload is used only as fallback
    when no loader is available.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PlatformerFallRestart : MonoBehaviour
{
    [SerializeField, Tooltip("Player transform to monitor. If empty, this object will be used.")]
    private Transform player;

    [SerializeField, Tooltip("Y position below which the current jump fails and the scene restarts.")]
    private float fallThresholdY = -6f;

    [SerializeField, Tooltip("SceneLoader used to reload the current platformer scene.")]
    private SceneLoader sceneLoader;

    private bool isRestarting;

    public bool IsRestarting => isRestarting;

    private void Awake()
    {
        if (player == null)
            player = transform;
    }

    private void Update()
    {
        if (isRestarting || player == null)
            return;

        if (player.position.y < fallThresholdY)
            RestartPlatformerScene();
    }

    private void RestartPlatformerScene()
    {
        if (isRestarting)
            return;

        isRestarting = true;
        Time.timeScale = 1f;
        PlatformerAudioHub.Instance?.PlayDeath();

        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (sceneLoader != null)
            sceneLoader.ReloadCurrentScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}