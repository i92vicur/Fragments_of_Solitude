/*
Phase2MovementController.cs

Purpose:
- Controls the phase2 movement controller flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Phase2MovementController : MonoBehaviour
{
    private const float GroundProbeDistance = 0.08f;

    [Header("Horizontal Movement")]
    [SerializeField, Tooltip("Maximum horizontal speed when moving left/right.")]
    private float horizontalSpeed = 6f;

    [SerializeField, Tooltip("Acceleration rate toward target horizontal speed (units/sec²).")]
    private float horizontalAcceleration = 30f;

    [SerializeField, Tooltip("Deceleration rate when no input is detected (units/sec²).")]
    private float horizontalDeceleration = 25f;

    [Header("Fall Configuration")]
    [SerializeField, Tooltip("Gravity scale for this Rigidbody2D. Higher = faster fall.")]
    private float gravityScale = 3f;

    [SerializeField, Tooltip("If enabled, limits max downward velocity.")]
    private bool clampFallSpeed = true;

    [SerializeField, Tooltip("Maximum downward velocity. Set to 0 to disable.")]
    private float maxFallSpeed = 9f;

    [SerializeField, Tooltip("Minimum distance kept between the player center and the camera's bottom edge.")]
    private float bottomScreenMargin = 1f;

    [Header("Dash")]
    [SerializeField, Tooltip("If true, allows one horizontal dash until the player lands.")]
    private bool dashEnabled = false;

    [SerializeField, Tooltip("Key used to trigger the horizontal dash.")]
    private KeyCode dashKey = KeyCode.LeftShift;

    [SerializeField, Tooltip("Instant horizontal speed applied when dash starts.")]
    private float dashHorizontalSpeed = 12f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    private ContactFilter2D groundFilter;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Camera gameplayCamera;
    private bool dashUsedThisFrame;
    private bool dashAvailable = true;
    private bool dashQueued;
    private bool isGrounded;
    private int lastHorizontalDirection = 1;
    private float currentHorizontalVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        // Configure Rigidbody2D for Phase 2
        if (rb != null)
        {
            rb.gravityScale = Mathf.Max(0f, gravityScale);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        horizontalSpeed = Mathf.Max(0f, horizontalSpeed);
        horizontalAcceleration = Mathf.Max(0f, horizontalAcceleration);
        horizontalDeceleration = Mathf.Max(0f, horizontalDeceleration);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        bottomScreenMargin = Mathf.Max(0f, bottomScreenMargin);
        dashHorizontalSpeed = Mathf.Max(0f, dashHorizontalSpeed);
        currentHorizontalVelocity = 0f;
        gameplayCamera = Camera.main;

        groundFilter = new ContactFilter2D();
        groundFilter.NoFilter();
        groundFilter.useTriggers = false;
        isGrounded = CheckGrounded();
    }

    private void OnValidate()
    {
        horizontalSpeed = Mathf.Max(0f, horizontalSpeed);
        horizontalAcceleration = Mathf.Max(0f, horizontalAcceleration);
        horizontalDeceleration = Mathf.Max(0f, horizontalDeceleration);
        gravityScale = Mathf.Max(0f, gravityScale);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        bottomScreenMargin = Mathf.Max(0f, bottomScreenMargin);
        dashHorizontalSpeed = Mathf.Max(0f, dashHorizontalSpeed);
    }

    private void Update()
    {
        int horizontalDirection = ReadHorizontalDirection();
        if (horizontalDirection != 0)
            lastHorizontalDirection = horizontalDirection;

        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        if (dashEnabled && dashAvailable && (Input.GetKeyDown(dashKey) || (!paused && Level4GamepadInput.DashPressedThisFrame())))
            dashQueued = true;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        bool wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
        if (isGrounded && !wasGrounded)
            dashAvailable = true;

        // Read horizontal input (teclado o mando, solo eje X)
        float horizontalInput = ReadHorizontalAxis(Input.GetAxis("Horizontal"));

        // Calculate target horizontal velocity
        float targetHorizontalVelocity = horizontalInput * horizontalSpeed;

        // Smooth acceleration/deceleration toward target velocity
        if (Mathf.Abs(targetHorizontalVelocity - currentHorizontalVelocity) > 0.01f)
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                // Input detected: accelerate toward target speed
                currentHorizontalVelocity = Mathf.MoveTowards(
                    currentHorizontalVelocity,
                    targetHorizontalVelocity,
                    horizontalAcceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                // No input: decelerate toward zero
                currentHorizontalVelocity = Mathf.MoveTowards(
                    currentHorizontalVelocity,
                    0f,
                    horizontalDeceleration * Time.fixedDeltaTime
                );
            }
        }
        else
        {
            currentHorizontalVelocity = targetHorizontalVelocity;
        }

        // Apply horizontal velocity; gravity is handled by Rigidbody2D
        Vector2 velocity = rb.linearVelocity;
        velocity.x = currentHorizontalVelocity;
        
        // Clamp fall speed if enabled
        if (clampFallSpeed && maxFallSpeed > 0f && velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
        }

        if (gameplayCamera != null && gameplayCamera.orthographic)
        {
            float bottomEdge = gameplayCamera.transform.position.y - gameplayCamera.orthographicSize;
            float playerMinimumY = bottomEdge + bottomScreenMargin;
            float lowestAllowedVelocity = (playerMinimumY - rb.position.y) / Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, lowestAllowedVelocity);
        }

        if (dashQueued)
        {
            dashQueued = false;
            int dashDirection = ReadHorizontalDirection();
            if (dashDirection == 0)
                dashDirection = Mathf.Abs(velocity.x) > 0.01f ? (velocity.x > 0f ? 1 : -1) : lastHorizontalDirection;

            velocity.x = dashDirection * dashHorizontalSpeed;
            currentHorizontalVelocity = velocity.x;
            dashAvailable = false;
            dashUsedThisFrame = true;
        }

        rb.linearVelocity = velocity;

        // Reset dash flag
        dashUsedThisFrame = false;
    }

    private bool CheckGrounded()
    {
        if (bodyCollider == null)
            return false;

        int hitCount = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, GroundProbeDistance);
        for (int i = 0; i < hitCount; i++)
        {
            if (groundHits[i].normal.y > 0.5f)
                return true;
        }

        return false;
    }

    private static int ReadHorizontalDirection()
    {
        float horizontalInput = ReadHorizontalAxis(Input.GetAxisRaw("Horizontal"));
        if (Mathf.Abs(horizontalInput) <= 0.01f)
            return 0;

        return horizontalInput > 0f ? 1 : -1;
    }

    // Usa el mayor valor entre el eje legacy (teclado) y el X del stick izquierdo (mando).
    private static float ReadHorizontalAxis(float legacyAxis)
    {
        float padX = GameInput.IsInitialized ? GameInput.Move.x : 0f;
        return Mathf.Abs(padX) > Mathf.Abs(legacyAxis) ? padX : legacyAxis;
    }

    // === Public API ===

    /// <summary>
    /// Get the current horizontal velocity.
    /// Useful for animation or external systems.
    /// </summary>
    public float GetHorizontalVelocity()
    {
        return rb != null ? rb.linearVelocity.x : 0f;
    }

    /// <summary>
    /// Get the current vertical velocity.
    /// Useful for death detection or state machines.
    /// </summary>
    public float GetVerticalVelocity()
    {
        return rb != null ? rb.linearVelocity.y : 0f;
    }

    /// <summary>
    /// True if the player is falling (downward velocity).
    /// </summary>
    public bool IsFalling => rb != null && rb.linearVelocity.y < 0f;

    public bool IsGrounded => isGrounded;

    public bool IsDashAvailable => dashEnabled && dashAvailable;

    /// <summary>
    /// Reserved for future dash implementation.
    /// </summary>
    public bool WasDashAttemptedThisFrame => dashUsedThisFrame;
}
