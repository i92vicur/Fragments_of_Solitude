using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlatformerPlayerController))]
[RequireComponent(typeof(PlatformerJumpController))]
public class PlatformerCharacterAnimationController : MonoBehaviour
{
    private enum FacingDirection
    {
        Front,
        Left,
        Right
    }

    [SerializeField, Tooltip("Horizontal velocity below this value keeps the current facing direction.")]
    private float facingDeadZone = 0.01f;

    [SerializeField, Tooltip("How long the Dash animation should be held after a dash starts.")]
    private float dashAnimationDuration = 0.6666667f;

    private Animator animator;
    private Rigidbody2D rb;
    private PlatformerPlayerController playerState;
    private PlatformerJumpController jumpController;

    private FacingDirection facingDirection = FacingDirection.Front;
    private string currentStateName = string.Empty;
    private bool isDashing;
    private float dashTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<PlatformerPlayerController>();
        jumpController = GetComponent<PlatformerJumpController>();

        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void OnEnable()
    {
        if (jumpController != null)
            jumpController.DashAvailabilityChanged += HandleDashAvailabilityChanged;
    }

    private void Start()
    {
        RefreshAnimation(true);
    }

    private void OnDisable()
    {
        if (jumpController != null)
            jumpController.DashAvailabilityChanged -= HandleDashAvailabilityChanged;
    }

    private void Update()
    {
        UpdateFacing();
        UpdateDashTimer();
        RefreshAnimation(false);
    }

    private void UpdateFacing()
    {
        if (rb == null)
            return;

        float horizontalVelocity = rb.linearVelocity.x;
        if (horizontalVelocity > facingDeadZone)
        {
            facingDirection = FacingDirection.Right;
            return;
        }

        if (horizontalVelocity < -facingDeadZone)
        {
            facingDirection = FacingDirection.Left;
            return;
        }

        if (playerState != null && playerState.IsGrounded)
            facingDirection = FacingDirection.Front;
    }

    private void UpdateDashTimer()
    {
        if (!isDashing)
            return;

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f)
            isDashing = false;
    }

    private void HandleDashAvailabilityChanged(bool dashAvailable)
    {
        if (dashAvailable)
            return;

        UpdateFacing();
        isDashing = true;
        dashTimer = Mathf.Max(0f, dashAnimationDuration);
        RefreshAnimation(true);
    }

    private void RefreshAnimation(bool force)
    {
        if (animator == null)
            return;

        string desiredState = GetDesiredStateName();
        if (!force && desiredState == currentStateName)
            return;

        currentStateName = desiredState;
        animator.Play("Base Layer." + desiredState, 0, 0f);
        animator.Update(0f);
    }

    private string GetDesiredStateName()
    {
        string actionName;
        if (isDashing)
            actionName = "Dash";
        else if (playerState != null && !playerState.IsGrounded)
            actionName = "Jump";
        else
            actionName = "Idle";

        return actionName + "_" + GetDirectionSuffix(facingDirection);
    }

    private static string GetDirectionSuffix(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Left:
                return "Left";
            case FacingDirection.Right:
                return "Right";
            default:
                return "Front";
        }
    }
}