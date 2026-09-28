/*
CameraFollow.cs

Purpose:
- Controls the camera follow flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
