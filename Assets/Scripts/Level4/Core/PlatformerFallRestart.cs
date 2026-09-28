/*
PlatformerFallRestart.cs

Purpose:
- Controls the platformer fall restart flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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