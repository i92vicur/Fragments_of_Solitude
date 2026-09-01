/*
Phase2DeathController.cs

Purpose: Detect when the player falls behind the descending camera or out of bounds in Phase 2.

Design decisions:
- Monitors player position relative to camera.
- Triggers death when player is below the camera's kill threshold.
- Uses SceneLoader to reload the scene (consistent with Phase 1 restart).
- Works with Phase2CameraScroller to determine the death boundary.
- One-shot guard prevents repeated reload requests.
*/

using UnityEngine;

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
        Time.timeScale = 1f;

        // Play death sound if PlatformerAudioHub is available
        PlatformerAudioHub.Instance?.PlayDeath();

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
