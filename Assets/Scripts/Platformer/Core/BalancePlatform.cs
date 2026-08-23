using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BalancePlatform : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField, Tooltip("Maximum tilt angle in degrees to either side.")]
    private float maximumRotationAngle = 90f;

    [SerializeField, Tooltip("If true, the platform can keep rotating while the player stays on it (no angle clamp while occupied).")]
    private bool allowFullRotationWhileOccupied = true;

    [SerializeField, Tooltip("How quickly the platform rotates toward the target angle (degrees/second).")]
    private float rotationResponseSpeed = 125f;

    [SerializeField, Tooltip("Maximum angular speed while the player is on the platform (degrees/second).")]
    private float maximumAngularSpeed = 185f;

    [SerializeField, Tooltip("If true, returns toward horizontal when no player is standing on top.")]
    private bool returnToHorizontalWhenEmpty = true;

    [SerializeField, Tooltip("How quickly the platform returns to horizontal when empty (degrees/second).")]
    private float emptyReturnSpeed = 45f;

    [SerializeField, Tooltip("Small grace window to avoid stand-state flicker when contact points briefly drop for a frame.")]
    private float standingGraceDuration = 0.12f;

    private Rigidbody2D rb;
    private BoxCollider2D platformCollider;
    private Rigidbody2D standingPlayer;
    private PlatformerPlayerController playerController;
    private Collider2D playerCollider;
    private Rigidbody2D playerBody;
    private float standingGraceTimer;
    private float angularVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<BoxCollider2D>();

        maximumRotationAngle = Mathf.Clamp(maximumRotationAngle, 0f, 179f);
        rotationResponseSpeed = Mathf.Max(0f, rotationResponseSpeed);
        maximumAngularSpeed = Mathf.Max(0f, maximumAngularSpeed);
        emptyReturnSpeed = Mathf.Max(0f, emptyReturnSpeed);
        standingGraceDuration = Mathf.Max(0f, standingGraceDuration);

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;
        rb.simulated = true;
        rb.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation;

        platformCollider.isTrigger = false;
        ResolvePlayerReferences();
    }

    private void OnValidate()
    {
        maximumRotationAngle = Mathf.Clamp(maximumRotationAngle, 0f, 179f);
        rotationResponseSpeed = Mathf.Max(0f, rotationResponseSpeed);
        maximumAngularSpeed = Mathf.Max(0f, maximumAngularSpeed);
        emptyReturnSpeed = Mathf.Max(0f, emptyReturnSpeed);
        standingGraceDuration = Mathf.Max(0f, standingGraceDuration);
    }

    private void FixedUpdate()
    {
        RefreshStandingPlayerFromTouching();

        if (standingPlayer != null && !standingPlayer.simulated)
            standingPlayer = null;

        float currentAngle = rb.rotation;
        float nextAngle;
        if (standingPlayer != null)
        {
            float normalizedOffset = ComputePlayerOffsetNormalized(standingPlayer);
            float angularAcceleration = -normalizedOffset * rotationResponseSpeed;
            angularVelocity += angularAcceleration * Time.fixedDeltaTime;
            angularVelocity = Mathf.Clamp(angularVelocity, -maximumAngularSpeed, maximumAngularSpeed);

            nextAngle = currentAngle + (angularVelocity * Time.fixedDeltaTime);

            if (!allowFullRotationWhileOccupied)
            {
                float clampedAngle = Mathf.Clamp(NormalizeAngle(nextAngle), -maximumRotationAngle, maximumRotationAngle);
                if (!Mathf.Approximately(clampedAngle, NormalizeAngle(nextAngle)))
                    angularVelocity = 0f;

                nextAngle = clampedAngle;
            }
        }
        else if (returnToHorizontalWhenEmpty)
        {
            angularVelocity = Mathf.MoveTowards(angularVelocity, 0f, emptyReturnSpeed * Time.fixedDeltaTime);
            nextAngle = Mathf.MoveTowardsAngle(currentAngle, 0f, emptyReturnSpeed * Time.fixedDeltaTime);
        }
        else
        {
            angularVelocity = Mathf.MoveTowards(angularVelocity, 0f, emptyReturnSpeed * Time.fixedDeltaTime);
            nextAngle = currentAngle + (angularVelocity * Time.fixedDeltaTime);
        }

        rb.MoveRotation(nextAngle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateStandingPlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateStandingPlayer(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (standingPlayer != null && collision.rigidbody == standingPlayer)
            standingPlayer = null;
    }

    private void UpdateStandingPlayer(Collision2D collision)
    {
        Rigidbody2D otherBody = collision.rigidbody;
        if (otherBody == null)
            return;

        bool isPlayerBody = IsPlayerBody(collision.collider)
            || IsPlayerBody(collision.otherCollider)
            || (collision.gameObject != null && collision.gameObject.CompareTag("Player"))
            || (collision.transform != null && collision.transform.GetComponent<PlatformerPlayerController>() != null);

        if (!isPlayerBody)
            return;

        if (!IsStandingContact(collision))
            return;

        standingPlayer = otherBody;
    }

    private bool IsPlayerBody(Collider2D other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponent<PlatformerPlayerController>() != null;
    }

    private bool IsStandingContact(Collision2D collision)
    {
        if (collision.rigidbody == null)
            return false;

        float platformTop = platformCollider.bounds.max.y - 0.06f;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.point.y >= platformTop)
                return true;
        }

        if (collision.rigidbody.worldCenterOfMass.y >= platformCollider.bounds.center.y)
            return true;

        return false;
    }

    private float ComputePlayerOffsetNormalized(Rigidbody2D playerBody)
    {
        Vector2 worldSamplePoint = playerCollider != null ? (Vector2)playerCollider.bounds.center : playerBody.worldCenterOfMass;
        Vector2 localPlayer = transform.InverseTransformPoint(worldSamplePoint);
        float halfWidth = Mathf.Max(0.0001f, platformCollider.size.x * 0.5f);
        float centeredOffset = localPlayer.x - platformCollider.offset.x;
        return Mathf.Clamp(centeredOffset / halfWidth, -1f, 1f);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        if (angle < -180f)
            angle += 360f;
        return angle;
    }

    private void RefreshStandingPlayerFromTouching()
    {
        ResolvePlayerReferences();

        if (playerCollider == null || platformCollider == null || !playerCollider.isActiveAndEnabled)
        {
            standingPlayer = null;
            standingGraceTimer = 0f;
            return;
        }

        if (IsPlayerOnPlatformTop())
        {
            standingPlayer = playerBody;
            standingGraceTimer = standingGraceDuration;
            return;
        }

        if (standingGraceTimer > 0f)
        {
            standingGraceTimer = Mathf.Max(0f, standingGraceTimer - Time.fixedDeltaTime);
            if (standingPlayer != null)
                return;
        }

        if (!IsPlayerOnPlatformTop())
        {
            standingPlayer = null;
            return;
        }

        standingPlayer = playerBody;
    }

    private void ResolvePlayerReferences()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlatformerPlayerController>();

        if (playerController == null)
        {
            playerCollider = null;
            playerBody = null;
            return;
        }

        if (playerCollider == null)
            playerCollider = playerController.GetComponent<Collider2D>();

        if (playerBody == null)
            playerBody = playerController.GetComponent<Rigidbody2D>();
    }

    private bool IsPlayerOnPlatformTop()
    {
        if (playerBody == null || playerCollider == null || platformCollider == null)
            return false;

        Bounds platformBounds = platformCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;

        bool overlapsX = playerBounds.max.x > platformBounds.min.x && playerBounds.min.x < platformBounds.max.x;
        if (!overlapsX)
            return false;

        float playerFeet = playerBounds.min.y;
        float platformTop = platformBounds.max.y;
        bool nearTop = playerFeet >= platformTop - 0.2f && playerFeet <= platformTop + 0.35f;
        if (!nearTop)
            return false;

        return playerBody.linearVelocity.y <= 0.6f;
    }
}
