/*
PlatformerJumpController.cs

Purpose:
- Controls the platformer jump controller flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlatformerPlayerController))]
public class PlatformerJumpController : MonoBehaviour
{
    [Header("Jump Charge")]
    [SerializeField, Tooltip("Smallest jump force applied when Space is released immediately.")]
    private float minimumJumpForce = 6f;

    [SerializeField, Tooltip("Largest jump force applied after a full charge.")]
    private float maximumJumpForce = 14f;

    [SerializeField, Tooltip("Maximum horizontal launch strength applied at full charge.")]
    private float maximumHorizontalLaunchStrength = 6f;

    [SerializeField, Tooltip("How quickly horizontal air steering can adjust velocity while falling.")]
    private float airControlStrength = 14f;

    [SerializeField, Tooltip("Maximum horizontal speed allowed during air steering. Set to 0 to disable clamping.")]
    private float maximumAirHorizontalSpeed = 7f;

    [SerializeField, Tooltip("Maximum time in seconds required to reach full jump charge.")]
    private float maximumChargeDuration = 3f;

    [Header("Dash")]
    [SerializeField, Tooltip("Key used to trigger the one air dash. Defaults to Left Shift.")]
    private KeyCode dashKey = KeyCode.LeftShift;

    [SerializeField, Tooltip("Instant horizontal speed applied when dash starts.")]
    private float dashHorizontalSpeed = 12f;

    [Header("Landing Bounce")]
    [SerializeField, Tooltip("Downward impact speed below which landing bounce is ignored.")]
    private float minimumImpactSpeed = 4f;

    [SerializeField, Tooltip("How strongly downward impact speed is converted into upward bounce velocity.")]
    private float bounceStrengthMultiplier = 0.25f;

    [SerializeField, Tooltip("Maximum upward bounce velocity applied on landing.")]
    private float maximumBounceVelocity = 2.5f;

    private Rigidbody2D rb;
    private PlatformerPlayerController playerState;

    private bool spaceHeldLastFrame;
    private bool chargeReleaseQueued;
    private float chargeTimer;
    private bool charging;
    private int selectedHorizontalDirection;
    private bool dashUsedThisJump;
    private bool dashQueued;
    private bool landingRecoveryLocked;
    private float lastFixedVerticalVelocity;
    private int lastLaunchHorizontalDirection = 1;

    public bool IsGrounded => playerState != null && playerState.IsGrounded;
    public bool IsCharging => charging;
    public bool IsAirborne => playerState != null && playerState.IsAirborne;
    public bool IsAscending => playerState != null && playerState.IsAscending;
    public bool IsDescending => playerState != null && playerState.IsDescending;
    public bool IsRestarting => playerState != null && playerState.IsRestarting;
    public bool IsDashAvailableThisJump => !dashUsedThisJump;
    public bool CanDash => playerState != null && playerState.IsAirborne && !charging && !dashUsedThisJump && !IsRestarting && !landingRecoveryLocked;
    public float NormalizedJumpCharge => maximumChargeDuration <= 0f ? 0f : Mathf.Clamp01(chargeTimer / maximumChargeDuration);
    public int SelectedHorizontalDirection => selectedHorizontalDirection;

    public event System.Action<bool> DashAvailabilityChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<PlatformerPlayerController>();

        if (maximumChargeDuration <= 0f)
            maximumChargeDuration = 3f;

        if (maximumJumpForce < minimumJumpForce)
            maximumJumpForce = minimumJumpForce;

        if (maximumHorizontalLaunchStrength < 0f)
            maximumHorizontalLaunchStrength = 0f;

        if (airControlStrength < 0f)
            airControlStrength = 0f;

        if (maximumAirHorizontalSpeed < 0f)
            maximumAirHorizontalSpeed = 0f;

        if (minimumImpactSpeed < 0f)
            minimumImpactSpeed = 0f;

        if (bounceStrengthMultiplier < 0f)
            bounceStrengthMultiplier = 0f;

        if (maximumBounceVelocity < 0f)
            maximumBounceVelocity = 0f;
    }

    private void Update()
    {
        if (IsRestarting)
        {
            spaceHeldLastFrame = Input.GetKey(KeyCode.Space);
            charging = false;
            chargeReleaseQueued = false;
            dashQueued = false;
            landingRecoveryLocked = false;
            SetDashUsedThisJump(false);
            chargeTimer = 0f;
            selectedHorizontalDirection = 0;
            lastFixedVerticalVelocity = 0f;

            if (playerState != null)
                playerState.EndJumpCharge();

            return;
        }

        if (landingRecoveryLocked && (playerState == null || !playerState.IsGrounded || rb.linearVelocity.y <= 0f))
            landingRecoveryLocked = false;

        if (playerState != null && playerState.JustLanded)
            SetDashUsedThisJump(false);

        bool spaceHeld = Input.GetKey(KeyCode.Space);
        if (Input.GetKeyDown(dashKey) && CanDash)
            dashQueued = true;

        int currentHorizontalDirection = ReadHorizontalDirection();

        if (spaceHeld && !spaceHeldLastFrame)
            TryBeginCharge(currentHorizontalDirection);

        if (!spaceHeld && spaceHeldLastFrame && charging)
            chargeReleaseQueued = true;

        if (charging)
        {
            chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maximumChargeDuration);
            selectedHorizontalDirection = currentHorizontalDirection;
        }

        if (spaceHeld && !charging && !IsGrounded)
            chargeTimer = 0f;

        spaceHeldLastFrame = spaceHeld;
    }

    private void FixedUpdate()
    {
        if (IsRestarting)
            return;

        if (playerState != null && playerState.JustLanded)
        {
            landingRecoveryLocked = true;
            PlatformerAudioHub.Instance?.PlayLand();
            ApplyLandingBounce(lastFixedVerticalVelocity);
        }

        if (dashQueued)
        {
            dashQueued = false;
            PerformDash();
            return;
        }

        if (chargeReleaseQueued)
        {
            chargeReleaseQueued = false;
            LaunchJump();
        }

        ApplyDescendingAirControl();

        lastFixedVerticalVelocity = rb.linearVelocity.y;
    }

    private void TryBeginCharge(int horizontalDirection)
    {
        if (playerState == null || !playerState.CanStartJumpCharge || charging)
            return;

        charging = true;
        chargeTimer = 0f;
        selectedHorizontalDirection = horizontalDirection;

        if (playerState != null)
            playerState.BeginJumpCharge();
    }

    private void LaunchJump()
    {
        if (!charging)
            return;

        int launchDirection = selectedHorizontalDirection;
        float normalizedCharge = NormalizedJumpCharge;
        float jumpForce = Mathf.Lerp(minimumJumpForce, maximumJumpForce, normalizedCharge);
        float horizontalForce = maximumHorizontalLaunchStrength * normalizedCharge;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = selectedHorizontalDirection * horizontalForce;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        if (launchDirection != 0)
            lastLaunchHorizontalDirection = launchDirection;

        charging = false;
        chargeTimer = 0f;
        selectedHorizontalDirection = 0;

        if (playerState != null)
            playerState.EndJumpCharge();

        PlatformerAudioHub.Instance?.PlayJump();
    }

    private void PerformDash()
    {
        if (!CanDash)
            return;

        int dashDirection = GetDashDirection();
        if (dashDirection == 0)
            return;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = dashDirection * dashHorizontalSpeed;
        rb.linearVelocity = velocity;

        SetDashUsedThisJump(true);
        PlatformerAudioHub.Instance?.PlayDash();
    }

    private void ApplyLandingBounce(float previousVerticalVelocity)
    {
        float impactSpeed = Mathf.Max(0f, -previousVerticalVelocity);
        if (impactSpeed < minimumImpactSpeed)
            return;

        float bounceVelocity = Mathf.Clamp((impactSpeed - minimumImpactSpeed) * bounceStrengthMultiplier, 0f, maximumBounceVelocity);
        if (bounceVelocity <= 0f)
            return;

        Vector2 velocity = rb.linearVelocity;
        velocity.y = bounceVelocity;
        rb.linearVelocity = velocity;
    }

    private void ApplyDescendingAirControl()
    {
        if (charging || !IsDescending)
            return;

        int horizontalInput = ReadHorizontalDirection();
        if (horizontalInput == 0)
            return;

        Vector2 velocity = rb.linearVelocity;
        float targetHorizontalSpeed = horizontalInput * maximumAirHorizontalSpeed;

        if (maximumAirHorizontalSpeed > 0f)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, targetHorizontalSpeed, airControlStrength * Time.fixedDeltaTime);
            velocity.x = Mathf.Clamp(velocity.x, -maximumAirHorizontalSpeed, maximumAirHorizontalSpeed);
        }
        else
        {
            velocity.x = Mathf.MoveTowards(velocity.x, targetHorizontalSpeed, airControlStrength * Time.fixedDeltaTime);
        }

        rb.linearVelocity = velocity;
    }

    private int ReadHorizontalDirection()
    {
        bool leftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (leftHeld == rightHeld)
            return 0;

        return leftHeld ? -1 : 1;
    }

    private int GetDashDirection()
    {
        int inputDirection = ReadHorizontalDirection();
        if (inputDirection != 0)
            return inputDirection;

        float horizontalVelocity = rb.linearVelocity.x;
        if (Mathf.Abs(horizontalVelocity) > 0.01f)
            return horizontalVelocity > 0f ? 1 : -1;

        return lastLaunchHorizontalDirection;
    }

    private void SetDashUsedThisJump(bool used)
    {
        if (dashUsedThisJump == used)
            return;

        dashUsedThisJump = used;
        DashAvailabilityChanged?.Invoke(IsDashAvailableThisJump);
    }
}