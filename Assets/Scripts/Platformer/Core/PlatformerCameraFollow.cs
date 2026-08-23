using UnityEngine;

[DisallowMultipleComponent]
public class PlatformerCameraFollow : MonoBehaviour
{
    [SerializeField, Tooltip("Target transform for the camera to follow.")]
    private Transform target;

    [SerializeField, Tooltip("Seconds used by SmoothDamp to reach the target position. Lower = snappier.")]
    private float followSmoothing = 0.12f;

    [SerializeField, Tooltip("Vertical buffer around the camera center before the camera starts moving.")]
    private float verticalDeadZone = 0.5f;

    [SerializeField, Tooltip("If true, the camera will not move below its starting Y position.")]
    private bool clampBelowStartY = true;

    private Vector3 velocity;
    private float fixedX;
    private float fixedZ;
    private float startY;
    private float cameraCenterY;

    private void Awake()
    {
        fixedX = transform.position.x;
        fixedZ = transform.position.z;
        startY = transform.position.y;
        cameraCenterY = startY;
    }

    private void Start()
    {
        if (target != null)
        {
            cameraCenterY = GetClampedTargetY(target.position.y);
            transform.position = new Vector3(fixedX, cameraCenterY, fixedZ);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float targetY = GetClampedTargetY(target.position.y);
        float upperBoundary = cameraCenterY + verticalDeadZone;
        float lowerBoundary = cameraCenterY - verticalDeadZone;

        if (targetY > upperBoundary)
            cameraCenterY = targetY - verticalDeadZone;
        else if (targetY < lowerBoundary)
            cameraCenterY = targetY + verticalDeadZone;

        cameraCenterY = GetClampedCameraY(cameraCenterY);

        Vector3 desiredPosition = new Vector3(fixedX, cameraCenterY, fixedZ);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothing);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            cameraCenterY = GetClampedTargetY(target.position.y);
            transform.position = new Vector3(fixedX, cameraCenterY, fixedZ);
        }
    }

    private float GetClampedTargetY(float targetY)
    {
        if (clampBelowStartY)
            return Mathf.Max(targetY, startY);

        return targetY;
    }

    private float GetClampedCameraY(float cameraY)
    {
        if (clampBelowStartY)
            return Mathf.Max(cameraY, startY);

        return cameraY;
    }
}