using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Phase2MovementController))]
public class Phase2CharacterAnimationController : MonoBehaviour
{
    private enum FacingDirection
    {
        Front,
        Left,
        Right
    }

    [SerializeField] private float facingDeadZone = 0.01f;
    [SerializeField] private float dashAnimationDuration = 0.6666667f;
    [SerializeField] private AnimationClip deathFrontClip;
    [SerializeField] private AnimationClip deathLeftClip;
    [SerializeField] private AnimationClip deathRightClip;

    private Animator animator;
    private Rigidbody2D rb;
    private Phase2MovementController movementController;
    private Phase2DeathController deathController;
    private FacingDirection facingDirection = FacingDirection.Front;
    private string currentStateName = string.Empty;
    private bool dashWasAvailable;
    private bool isDashing;
    private bool deathAnimationStarted;
    private float dashTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movementController = GetComponent<Phase2MovementController>();
        deathController = FindAnyObjectByType<Phase2DeathController>();
        dashWasAvailable = movementController != null && movementController.IsDashAvailable;

        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void Start()
    {
        RefreshAnimation(true);
    }

    private void Update()
    {
        UpdateFacing();

        if (deathController != null && deathController.IsRestarting)
        {
            PlayDeathAnimation();
            return;
        }

        bool dashAvailable = movementController != null && movementController.IsDashAvailable;
        if (dashWasAvailable && !dashAvailable)
        {
            isDashing = true;
            dashTimer = Mathf.Max(0f, dashAnimationDuration);
            RefreshAnimation(true);
        }
        dashWasAvailable = dashAvailable;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }

        RefreshAnimation(false);
    }

    private void UpdateFacing()
    {
        if (rb == null)
            return;

        if (rb.linearVelocity.x > facingDeadZone)
            facingDirection = FacingDirection.Right;
        else if (rb.linearVelocity.x < -facingDeadZone)
            facingDirection = FacingDirection.Left;
    }

    private void RefreshAnimation(bool force)
    {
        if (animator == null || movementController == null)
            return;

        string actionName = isDashing ? "Dash" : movementController.IsGrounded ? "Idle" : "Jump";
        string desiredStateName = actionName + "_" + GetDirectionSuffix(facingDirection);
        if (!force && desiredStateName == currentStateName)
            return;

        currentStateName = desiredStateName;
        animator.Play("Base Layer." + desiredStateName, 0, 0f);
    }

    private void PlayDeathAnimation()
    {
        if (deathAnimationStarted || animator == null)
            return;

        deathAnimationStarted = true;
        AnimationClip deathClip = GetDeathClip();
        if (deathClip == null)
            return;

        AnimatorOverrideController overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        overrideController["Idle_Front"] = deathClip;
        animator.runtimeAnimatorController = overrideController;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.Play("Base Layer.Idle_Front", 0, 0f);
    }

    private AnimationClip GetDeathClip()
    {
        switch (facingDirection)
        {
            case FacingDirection.Left:
                return deathLeftClip;
            case FacingDirection.Right:
                return deathRightClip;
            default:
                return deathFrontClip;
        }
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