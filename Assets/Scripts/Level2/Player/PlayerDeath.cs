/*
PlayerDeath.cs

Purpose:
- Controls the player death flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
