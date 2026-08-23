using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlatformerPlayerController : MonoBehaviour
{
    public enum MotionState
    {
        Grounded,
        Charging,
        Ascending,
        Descending,
        Restarting
    }

    private const float GroundProbeDistance = 0.08f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    private ContactFilter2D groundFilter;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlatformerFallRestart fallRestart;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isJumpCharging;

    public bool IsGrounded => isGrounded;
    public bool WasGroundedLastFixedUpdate => wasGrounded;
    public bool IsJumpCharging => isJumpCharging;
    public bool IsRestarting => fallRestart != null && fallRestart.IsRestarting;
    public bool IsAirborne => !IsGrounded && !IsRestarting;
    public bool IsAscending => IsAirborne && rb != null && rb.linearVelocity.y > 0f;
    public bool IsDescending => IsAirborne && rb != null && rb.linearVelocity.y < 0f;
    public bool CanStartJumpCharge => isGrounded && !isJumpCharging && !IsRestarting;
    public bool JustLanded => isGrounded && !wasGrounded && !IsRestarting;
    public MotionState CurrentMotionState
    {
        get
        {
            if (IsRestarting)
                return MotionState.Restarting;

            if (IsJumpCharging)
                return MotionState.Charging;

            if (IsGrounded)
                return MotionState.Grounded;

            return rb != null && rb.linearVelocity.y > 0f ? MotionState.Ascending : MotionState.Descending;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        fallRestart = GetComponent<PlatformerFallRestart>();

        rb.gravityScale = Mathf.Max(rb.gravityScale, 1f);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        groundFilter = new ContactFilter2D();
        groundFilter.NoFilter();
        groundFilter.useTriggers = false;

        isGrounded = CheckGrounded();
        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
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

    public void SetJumpCharging(bool charging)
    {
        isJumpCharging = charging && CanStartJumpCharge;
    }

    public void BeginJumpCharge()
    {
        if (CanStartJumpCharge)
            isJumpCharging = true;
    }

    public void EndJumpCharge()
    {
        isJumpCharging = false;
    }
}
