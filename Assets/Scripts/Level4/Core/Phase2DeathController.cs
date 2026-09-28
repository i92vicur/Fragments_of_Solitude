/*
Phase2DeathController.cs

Purpose:
- Controls the phase2 death controller flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class Phase2DeathController : MonoBehaviour
{
    [SerializeField, Tooltip("Player transform to monitor. If empty, auto-finds Phase2MovementController.")]
    private Transform player;

    [SerializeField, Tooltip("Camera transform to use as reference for kill boundary.")]
    private Transform cameraTransform;

    [SerializeField, Tooltip("Distance inside the camera's upper edge where the player dies (units).")]
    private float cameraDeathMargin = 1f;

    [SerializeField, Tooltip("SceneLoader used to reload the current scene on death.")]
    private SceneLoader sceneLoader;

    [SerializeField, Tooltip("Realtime delay that allows the death animation to finish before reloading.")]
    private float deathAnimationDuration = 0.75f;

    private Camera gameplayCamera;
    private bool isRestarting;

    public bool IsRestarting => isRestarting;

    private void Awake()
    {
        if (player == null)
        {
            Phase2MovementController phase2Controller = FindAnyObjectByType<Phase2MovementController>();
            if (phase2Controller != null)
                player = phase2Controller.transform;
        }

        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }

        if (cameraTransform != null)
            gameplayCamera = cameraTransform.GetComponent<Camera>();
    }

    private void Update()
    {
        if (isRestarting || player == null || gameplayCamera == null)
            return;

        float cameraTop = gameplayCamera.transform.position.y + gameplayCamera.orthographicSize;
        float killThreshold = cameraTop - cameraDeathMargin;
        if (player.position.y >= killThreshold)
        {
            RestartScene();
        }
    }

    private void RestartScene()
    {
        if (isRestarting)
            return;

        isRestarting = true;
        Time.timeScale = 0f;

        // Play death sound if PlatformerAudioHub is available
        PlatformerAudioHub.Instance?.PlayDeath();

        StartCoroutine(ReloadAfterDeathAnimation());
    }

    private IEnumerator ReloadAfterDeathAnimation()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, deathAnimationDuration));

        // Reload the scene via SceneLoader
        if (sceneLoader == null)
            sceneLoader = FindAnyObjectByType<SceneLoader>();

        if (sceneLoader != null)
            sceneLoader.ReloadCurrentScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
