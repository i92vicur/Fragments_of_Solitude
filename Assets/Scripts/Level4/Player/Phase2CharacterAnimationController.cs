/*
Phase2CharacterAnimationController.cs

Purpose:
- Controls the phase2 character animation controller flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

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

    private Animator animator;
    private Rigidbody2D rb;
    private Phase2MovementController movementController;
    private FacingDirection facingDirection = FacingDirection.Front;
    private string currentStateName = string.Empty;
    private bool dashWasAvailable;
    private bool isDashing;
    private float dashTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movementController = GetComponent<Phase2MovementController>();
        dashWasAvailable = movementController.IsDashAvailable;
    }

    private void Start()
    {
        RefreshAnimation();
    }

    private void Update()
    {
        UpdateFacing();

        bool dashAvailable = movementController.IsDashAvailable;
        if (dashWasAvailable && !dashAvailable)
        {
            isDashing = true;
            dashTimer = Mathf.Max(0f, dashAnimationDuration);
        }
        dashWasAvailable = dashAvailable;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }

        RefreshAnimation();
    }

    private void UpdateFacing()
    {
        if (rb.linearVelocity.x > facingDeadZone)
            facingDirection = FacingDirection.Right;
        else if (rb.linearVelocity.x < -facingDeadZone)
            facingDirection = FacingDirection.Left;
    }

    private void RefreshAnimation()
    {
        string actionName;
        if (isDashing)
            actionName = "Dash";
        else if (!movementController.IsGrounded)
            actionName = "Jump";
        else if (Mathf.Abs(rb.linearVelocity.x) > facingDeadZone)
            actionName = "Run";
        else
            actionName = "Idle";

        string desiredStateName = actionName + "_" + GetDirectionSuffix(facingDirection);
        if (desiredStateName == currentStateName)
            return;

        currentStateName = desiredStateName;
        animator.Play("Base Layer." + desiredStateName, 0, 0f);
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