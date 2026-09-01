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

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float fixedX;
    private float fixedZ;
    private float startY;
    private float targetCameraY;
    private float elapsedTime;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        fixedX = transform.position.x;
        fixedZ = transform.position.z;
        startY = transform.position.y;
        targetCameraY = startY;
        elapsedTime = 0f;
    }

    private void LateUpdate()
    {
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
    /// Get the current camera center Y position.
    /// Useful for death detection.
    /// </summary>
    public float GetCameraCenterY()
    {
        return transform.position.y;
    }
}
