/*
EnemyMovement.cs

Purpose:
- Controls the enemy movement flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField, Tooltip("Movement speed in units per second.")]
    private float moveSpeed = 3f;

    [SerializeField, Tooltip("Target Transform to follow (assign in Inspector).")]
    private Transform target;

    // --- Obstacle avoidance tuning ---
    [Header("Avoidance")]
    [SerializeField, Tooltip("How far ahead (world units) to probe for obstacles on the direct path.")]
    private float lookaheadDistance = 1.0f;

    [SerializeField, Tooltip("Radius used for the circle cast when detecting obstacles. Increase for larger enemies.")]
    private float detectionRadius = 0.35f;

    [SerializeField, Tooltip("Candidate avoidance angles (degrees). The code samples these in order and picks the first unblocked heading.")]
    private float[] avoidanceAngles = new float[] { 30f, -30f, 60f, -60f, 120f, -120f };

    [SerializeField, Tooltip("Which layers are considered obstacles for avoidance (set to Environment).")]
    private LayerMask obstacleLayerMask = 0;

    [SerializeField, Tooltip("Draw debug rays and circles in the Scene view to visualize avoidance checks.")]
    private bool debugGizmos = false;

    private Rigidbody2D rb;
    private Vector2 moveDirection = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Prevent physics from rotating the enemy in a top-down game
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // If no target was assigned in the Inspector, try to find the Player once.
        if (target == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                target = playerGO.transform;
            }
            else
            {
                Debug.LogWarning($"EnemyMovement on '{gameObject.name}' has no target and no GameObject tagged 'Player' was found. Enemy will remain stationary.", this);
            }
        }

        // If obstacleLayerMask is not set in Inspector, try to default to Environment layer
        if (obstacleLayerMask == 0)
        {
            int env = LayerMask.NameToLayer("Environment");
            if (env != -1) obstacleLayerMask = 1 << env;
        }
    }

    void Update()
    {
        if (DialogueGate.IsDialogueActive)
        {
            moveDirection = Vector2.zero;
            return;
        }

        if (target == null)
        {
            moveDirection = Vector2.zero;
            return; // fail safely without spamming the console
        }

        Vector2 toTarget = (Vector2)(target.position - transform.position);
        if (toTarget.sqrMagnitude > 0.000001f)
            moveDirection = toTarget.normalized;
        else
            moveDirection = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (moveDirection.sqrMagnitude <= 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Quick avoidance: circle-cast forward along moveDirection to detect immediate obstacles
        bool blocked = false;
        Vector2 origin = transform.position;

        // Non-allocating single hit check
        var hit = Physics2D.CircleCast(origin, detectionRadius, moveDirection, lookaheadDistance, obstacleLayerMask);
        if (hit.collider != null)
        {
            blocked = true;
            if (debugGizmos)
            {
                        Debug.DrawLine((Vector3)origin, (Vector3)(origin + moveDirection * lookaheadDistance), Color.red, 0.1f);
                        Debug.DrawLine((Vector3)hit.point, (Vector3)(hit.point + Vector2.up * 0.1f), Color.yellow, 0.1f);
            }
        }

        Vector2 chosenDirection = moveDirection;

        if (blocked)
        {
            // Sample avoidance angles in order, pick the first that is not blocked
            float bestAngle = 0f;
            bool found = false;
            for (int i = 0; i < avoidanceAngles.Length; i++)
            {
                float angle = avoidanceAngles[i];
                float rad = angle * Mathf.Deg2Rad;
                Vector2 alt = new Vector2(
                    moveDirection.x * Mathf.Cos(rad) - moveDirection.y * Mathf.Sin(rad),
                    moveDirection.x * Mathf.Sin(rad) + moveDirection.y * Mathf.Cos(rad)
                );

                var altHit = Physics2D.CircleCast(origin, detectionRadius, alt, lookaheadDistance, obstacleLayerMask);
                if (altHit.collider == null)
                {
                    bestAngle = angle;
                    chosenDirection = alt.normalized;
                    found = true;
                    if (debugGizmos)
                    {
                                        Debug.DrawLine((Vector3)origin, (Vector3)(origin + alt * lookaheadDistance), Color.green, 0.1f);
                    }
                    break;
                }
                else if (debugGizmos)
                {
                                    Debug.DrawLine((Vector3)origin, (Vector3)(origin + alt * lookaheadDistance), Color.yellow, 0.1f);
                }
            }

            if (!found)
            {
                // No clear alternative heading found: attempt a small lateral slide along the obstacle using perpendicular direction
                Vector2 perp = Vector2.Perpendicular(moveDirection).normalized;
                var pHit = Physics2D.CircleCast(origin, detectionRadius, perp, lookaheadDistance * 0.5f, obstacleLayerMask);
                if (pHit.collider == null)
                {
                    chosenDirection = perp;
                }
                else
                {
                    var pmHit = Physics2D.CircleCast(origin, detectionRadius, -perp, lookaheadDistance * 0.5f, obstacleLayerMask);
                    if (pmHit.collider == null)
                        chosenDirection = -perp;
                    else
                        chosenDirection = Vector2.zero; // stuck
                }
            }
        }

        // Apply chosen movement
        rb.linearVelocity = chosenDirection * moveSpeed;
    }

    // Optional helper to assign target from other scripts
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null) moveDirection = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(moveDirection * lookaheadDistance));
    }
}
