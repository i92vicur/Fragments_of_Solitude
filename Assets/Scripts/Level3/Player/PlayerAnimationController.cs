/*
PlayerAnimationController.cs

Purpose: Drive the player's directional animation cycle with Idle, Run, and Dead states.

Design decisions:
- Directional movement only: chooses a facing direction from Rigidbody2D velocity.
- Action + direction are separate: the current action is Idle, Run, or Dead; the current
  facing direction is one of eight directions.
- Death is final: once Health reaches zero, the controller stays in Dead and never returns
  to Idle or Run.
- Automatic attack is ignored by design so animation does not depend on combat state.
- Simple Animator usage: plays one of the 24 state names directly; no complex transition graph.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private enum FacingDirection
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }

    [SerializeField, Tooltip("Minimum squared velocity considered to be moving.")]
    private float moveThresholdSqr = 0.0001f;

    private Animator animator;
    private Rigidbody2D rb;
    private Health health;

    private bool isDead;
    private FacingDirection facingDirection = FacingDirection.Down;
    private string currentStateName = string.Empty;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        if (animator == null)
            Debug.LogError("PlayerAnimationController requires an Animator.", this);
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnZeroHealth += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnZeroHealth -= HandleDeath;
    }

    private void Start()
    {
        if (health != null && health.IsDead)
            HandleDeath();

        RefreshAnimation(true);
    }

    private void Update()
    {
        if (isDead)
            return;

        RefreshFacingFromVelocity();
        RefreshAnimation(false);
    }

    private void RefreshFacingFromVelocity()
    {
        if (rb == null)
            return;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude <= moveThresholdSqr)
            return;

        facingDirection = DetermineFacingDirection(velocity);
    }

    private void RefreshAnimation(bool force)
    {
        if (animator == null)
            return;

        string desiredState = BuildStateName(isDead ? "Dead" : (IsMoving() ? "Run" : "Idle"), facingDirection);
        if (!force && desiredState == currentStateName)
            return;

        currentStateName = desiredState;
        animator.Play(desiredState, 0, 0f);
    }

    private bool IsMoving()
    {
        if (rb == null)
            return false;

        return rb.linearVelocity.sqrMagnitude > moveThresholdSqr;
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        RefreshAnimation(true);
    }

    private static FacingDirection DetermineFacingDirection(Vector2 velocity)
    {
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        if (angle >= 337.5f || angle < 22.5f)
            return FacingDirection.Right;
        if (angle < 67.5f)
            return FacingDirection.UpRight;
        if (angle < 112.5f)
            return FacingDirection.Up;
        if (angle < 157.5f)
            return FacingDirection.UpLeft;
        if (angle < 202.5f)
            return FacingDirection.Left;
        if (angle < 247.5f)
            return FacingDirection.DownLeft;
        if (angle < 292.5f)
            return FacingDirection.Down;
        return FacingDirection.DownRight;
    }

    private static string BuildStateName(string action, FacingDirection direction)
    {
        return action + "_" + GetDirectionSuffix(direction);
    }

    private static string GetDirectionSuffix(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Up: return "Up";
            case FacingDirection.Down: return "Down";
            case FacingDirection.Left: return "Left";
            case FacingDirection.Right: return "Right";
            case FacingDirection.UpLeft: return "UpLeft";
            case FacingDirection.UpRight: return "UpRight";
            case FacingDirection.DownLeft: return "DownLeft";
            case FacingDirection.DownRight: return "DownRight";
            default: return "Down";
        }
    }
}