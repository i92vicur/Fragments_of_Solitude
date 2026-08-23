/*
CameraFollow.cs

Purpose: Smoothly track a target (usually the Player) in a top-down arena game.

Design decisions:
- Smooth tracking: Uses Vector3.SmoothDamp for fluid, non-jerky camera motion instead of instant snapping.
- Fixed depth: Keeps camera Z-position constant (typical for orthographic 2D games) while tracking XY of target.
- Initialization: Snaps camera to target's initial position on Start to avoid an initial slide animation.
- Late update: Runs in LateUpdate so camera tracks after all movement physics have been applied.
- No camera bounds: For a small prototype arena, the simple follow is sufficient; no need for confinement logic yet.

Functionality:
- Assigns target Transform via Inspector or SetTarget(Transform) method.
- Each LateUpdate, computes desired position (target's XY at fixed Z) and smoothly moves toward it.
- If target is null, camera remains stationary (safe fallback).

Configuration:
- target: Assign the Player Transform.
- smoothTime: Seconds for the smooth damp curve (0.05–0.15 typical). Lower = more responsive, higher = more lag.

Notes:
- Requires the Camera to be on a separate GameObject or as a component that can be moved.
- Works with either orthographic or perspective cameras (though orthographic is standard for 2D).
- No zoom or rotation; purely positional tracking.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [SerializeField, Tooltip("Transform the camera will follow (assign in Inspector).")]
    private Transform target;

    [SerializeField, Tooltip("Smaller = more responsive. Seconds to smooth to target.")]
    private float smoothTime = 0.08f;

    private Vector3 velocity = Vector3.zero;
    private float targetZ;

    void Start()
    {
        targetZ = transform.position.z;
        if (target != null)
        {
            // Snap to target initially to avoid an initial slide
            transform.position = new Vector3(target.position.x, target.position.y, targetZ);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, targetZ);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    // Allow other scripts to set the target via code if needed later
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            transform.position = new Vector3(target.position.x, target.position.y, targetZ);
    }
}
