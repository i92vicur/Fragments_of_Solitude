/*
PlayerDeath.cs

Purpose: Handle player death by stopping movement, clearing velocity, logging the event, and pausing the game.

Design decisions:
- Event-driven trigger: Subscribes to Health.OnZeroHealth to be notified when health reaches zero. Reacts exactly once via hasDied flag.
- Death sequence:
  1. Disable PlayerMovement component to stop input processing.
  2. Zero Rigidbody2D velocity to halt motion immediately.
  3. Log a debug message for clarity.
  4. Pause via PauseManager.Pause(PauseReason.Death) to stop the game.
- Subscription safety: Uses OnEnable/OnDisable to subscribe/unsubscribe from Health.OnZeroHealth, preventing delegate leaks.
- Pause system integration: Works correctly with PauseManager's multi-reason pause system. If LevelUpController is also paused for LevelUp when player dies, both pause reasons are tracked and the game stays paused.
- Separation of concerns: Keeps death logic separate from Health. Health only signals the event; PlayerDeath handles consequences.
- Simple and focused: No respawn logic, no restart handling, no audio/VFX (handled by EnemyDeath-style systems if needed separately).

Configuration:
- No public fields; automatic detection of Health, PlayerMovement, Rigidbody2D on Awake.

Usage:
- Attach to the Player GameObject.
- Ensure Health component exists on the Player.
- Ensure PlayerMovement and Rigidbody2D are also present.

Notes:
- Time.timeScale is set to 0 by PauseManager; LevelUpController checks Health.IsDead before resuming to avoid resuming during death.
- To test, set Player health to 0 via Inspector, enter Play Mode, and observe pause.
- If Time.timeScale is manually altered, stop Play Mode to reset it to 1.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDeath : MonoBehaviour
{
    private Health health;
    private PlayerMovement movement;
    private Rigidbody2D rb;

    private bool hasDied = false;

    void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnZeroHealth += HandleDeath;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnZeroHealth -= HandleDeath;
    }

    void Start()
    {
        // If health is already zero at start, handle death immediately
        if (health != null && health.IsDead)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (hasDied) return;
        hasDied = true;

        // Disable player movement script if present
        if (movement != null)
            movement.enabled = false;

        // Stop any residual velocity
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Debug.Log("Player has died.");

        // Pause the game via PauseManager so multiple pause reasons are tracked safely
        PauseManager.Instance.Pause(PauseManager.PauseReason.Death);
    }
}
