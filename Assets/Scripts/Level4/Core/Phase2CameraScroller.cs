/*
Phase2CameraScroller.cs

Purpose: Implement an autonomous descending camera for Phase 2 platformer.

Design decisions:
- The camera descends independently from the player's position.
- Horizontal position is fixed (like Phase 1's camera but independent).
- The camera descends at a configured speed to maintain challenge (player must keep up).
- No dependency on Phase 1's PlatformerCameraFollow; completely independent system.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class Phase2CameraScroller : MonoBehaviour
{
    [SerializeField, Tooltip("Minimum camera descent speed (units per second) to force player to keep moving.")]
    private float minimumDescentSpeed = 1.8f;

    [SerializeField, Tooltip("Smoothing factor for camera movement (0 = instant, higher = smoother).")]
    private float smoothing = 0.08f;

    [SerializeField, Tooltip("If true, prevent camera from rising above its starting Y position.")]
    private bool clampToStartY = true;

    [Header("Initial Hold")]
    [SerializeField, Tooltip("Player transform used to detect when the descent should begin. Auto-found by tag if left empty.")]
    private Transform player;

    [SerializeField, Tooltip("Camera descent stays paused until the player's Y position drops below this value (e.g. bottom of a starting platform). Leave at a very low value to start descending immediately (default behavior).")]
    private float holdUntilPlayerBelowY = float.NegativeInfinity;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float fixedX;
    private float fixedZ;
    private float startY;
    private float targetCameraY;
    private float elapsedTime;
    private bool descentActive;
    private bool descentStopped;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        fixedX = transform.position.x;
        fixedZ = transform.position.z;
        startY = transform.position.y;
        targetCameraY = startY;
        elapsedTime = 0f;

        if (player == null)
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
        }

        descentActive = player == null || float.IsNegativeInfinity(holdUntilPlayerBelowY);
    }

    private void LateUpdate()
    {
        if (descentStopped)
            return;

        if (!descentActive)
        {
            if (player.position.y < holdUntilPlayerBelowY)
            {
                descentActive = true;
                elapsedTime = 0f;
                startY = transform.position.y;
            }
            else
            {
                return;
            }
        }

        elapsedTime += Time.deltaTime;
        targetCameraY = startY - (elapsedTime * minimumDescentSpeed);
        if (clampToStartY)
        {
            targetCameraY = Mathf.Min(targetCameraY, startY);
        }

        // Apply smooth damping to avoid jerky camera movement
        Vector3 desiredPosition = new Vector3(fixedX, targetCameraY, fixedZ);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothing);
    }

    /// <summary>
    /// Permanently halts the descent (e.g. when the player reaches the goal platform).
    /// </summary>
    public void StopDescent()
    {
        descentStopped = true;
        velocity = Vector3.zero;
    }

    /// <summary>
    /// Get the current camera center Y position.
    /// Useful for death detection.
    /// </summary>
    public float GetCameraCenterY()
    {
        return transform.position.y;
    }
}
