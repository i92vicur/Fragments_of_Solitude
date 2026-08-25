/*
EnemyMovement.cs

Purpose: Simple Rigidbody2D-based movement for enemies with lightweight local obstacle avoidance.

Design decisions:
- Movement model: Uses a Rigidbody2D with velocity-based locomotion. Direction is computed in Update and applied as velocity in FixedUpdate for frame-rate independence.
- Target tracking: Accepts a target Transform assigned via Inspector or SetTarget(Transform) method. If none assigned, performs a one-time GameObject.FindWithTag("Player") lookup so spawned enemies auto-find the player.
- Direct pursuit: Primary movement is a direct normalized heading toward the target at configurable moveSpeed.
- Obstacle avoidance (lightweight, greedy approach):
  - Probes ahead along the direct path using Physics2D.CircleCast with detectionRadius and lookaheadDistance.
  - If an obstacle is detected, samples avoidanceAngles (cardinal + diagonal) in order, testing each candidate direction via CircleCast.
    - Uses the first unblocked direction found; if none exist, tries a short perpendicular slide and then stops.
  - No pathfinding, no flow-field, no memory of past attempts.
- Physics setup: Freezes Rigidbody2D rotation to prevent spinning due to collisions in top-down view.
- Safety: If target is null, remains stationary without error spam. Missing obstacleLayerMask defaults to auto-detect Environment layer.
- Debug visualization: Optional scene-view gizmos show lookahead rays and collision detection circles (debugGizmos flag).
- Simplicity: Suitable for small prototype; no animation, no complex steering behaviors.

Configuration:
- moveSpeed: Units per second (typical 2–5 for enemies slower than player).
- target: Assign Player Transform in Inspector (optional; auto-found if null).
- Avoidance:
  - lookaheadDistance: How far ahead to probe (~1.0 typical).
  - detectionRadius: Circle radius for collision checks (~0.35 for small enemies, 0.5+ for large enemies or boss).
  - avoidanceAngles: Sample angles in degrees; defaults are [30, -30, 60, -60, 120, -120].
  - obstacleLayerMask: Environment layer (auto-detected if 0).
- debugGizmos: Enable to visualize avoidance checks in Scene view.

Usage:
- Attach to enemy GameObject.
- Assign target Transform in Inspector or call SetTarget(Transform) from spawner.
- Ensure Rigidbody2D and Collider2D are present.
- EnemySpawner handles instantiation and SetTarget call.

Notes:
- Avoidance is greedy and local; it does not guarantee smooth paths through narrow spaces.
- Large obstacles may still cause enemies to jam; adjust detectionRadius per enemy size if needed.
- Avoidance is disabled if obstacleLayerMask is 0 at Awake (no Environment layer found).
- Boss often needs larger detectionRadius (~0.5–0.7) due to larger collider size.
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
