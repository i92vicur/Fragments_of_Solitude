/*
EnemyContactDamage.cs

Purpose: Apply periodic damage to the Player when an enemy makes contact.

Design decisions:
- Collision detection: Supports both OnCollision and OnTrigger callbacks so this component works with either collider setup (trigger or physics-based).
- Target filtering: Uses a configurable targetTag (default "Player") to restrict damage to the player only, avoiding friendly fire.
- Health lookup: On first contact, caches the Health component via GetComponent or GetComponentInParent once, then reuses it to avoid repeated searches.
- Damage timing:
  - Immediate damage on contact enter.
  - Subsequent damage only after damageInterval (in seconds) to prevent single-frame instant kills.
  - Timer resets each time damage is applied while in contact.
- Separation of concerns: Keeps damage logic off EnemyMovement; does not handle knockback, invincibility frames, or other side effects.
- Safety: Validates damageInterval in OnValidate; fails gracefully if target lacks Health (logs warning once, does not throw).
- Simplicity: No armor, damage types, or mitigation—just direct periodic damage.

Configuration:
- damageAmount: Damage per hit (typical 1–5, depending on enemy type).
- damageInterval: Seconds between damage applications (typical 1.0).
- targetTag: Tag to identify the Player (default "Player").

Usage:
- Attach to any enemy GameObject.
- Ensure the player has a Health component and the "Player" tag.
- Ensure colliders are set up (trigger or non-trigger) so contact is detected.

Notes:
- If the player is invulnerable or has shields (future features), this component should not apply damage anyway; check Health.IsDead or other guards if needed.
- For multi-hit scenarios (stacked enemies), the damageInterval spacing prevents instant death; each enemy damages separately.
*/

using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField, Tooltip("Damage applied to the target on contact.")]
    private int damageAmount = 1;

    [SerializeField, Tooltip("Minimum seconds between successive damage applications while in contact.")]
    private float damageInterval = 1f;

    [SerializeField, Tooltip("Tag identifying valid targets. Defaults to Player.")]
    private string targetTag = "Player";

    private Collider2D trackedCollider;
    private Health trackedHealth;
    private float nextDamageTime = 0f;

    public bool IsAttacking => trackedCollider != null;

    void OnValidate()
    {
        if (damageAmount <= 0) damageAmount = 1;
        if (damageInterval < 0f) damageInterval = 0f;
        if (string.IsNullOrWhiteSpace(targetTag)) targetTag = "Player";
    }

    void Reset()
    {
        // Ensure there's a collider when added from the Editor
        var col = GetComponent<Collider2D>();
        if (col == null) gameObject.AddComponent<BoxCollider2D>();
    }

    // COLLISION-BASED (non-trigger)
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryBeginContact(collision.collider);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryMaintainContact(collision.collider);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        TryEndContact(collision.collider);
    }

    // TRIGGER-BASED (trigger)
    void OnTriggerEnter2D(Collider2D other)
    {
        TryBeginContact(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryMaintainContact(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        TryEndContact(other);
    }

    private void TryBeginContact(Collider2D other)
    {
        if (DialogueGate.IsDialogueActive) return;
        if (!other || !other.CompareTag(targetTag)) return;

        // If already tracking this collider, ignore
        if (trackedCollider == other) return;

        // Cache collider and try to find a Health component efficiently
        var health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health == null)
        {
            // No health found on this target; do nothing
            return;
        }

        trackedCollider = other;
        trackedHealth = health;

        // Apply immediate damage on first contact
        ApplyDamageImmediate();
    }

    private void TryMaintainContact(Collider2D other)
    {
        if (DialogueGate.IsDialogueActive) return;
        if (trackedCollider != other) return; // ignore other colliders

        if (trackedHealth == null) return;

        if (Time.time >= nextDamageTime)
        {
            ApplyDamage();
        }
    }

    private void TryEndContact(Collider2D other)
    {
        if (trackedCollider != other) return;

        trackedCollider = null;
        trackedHealth = null;
        nextDamageTime = 0f;
    }

    private void ApplyDamageImmediate()
    {
        if (trackedHealth == null) return;


        bool applied = trackedHealth.TakeDamage(damageAmount);
        if (applied)
        {
            if (AudioHub.Instance != null)
                AudioHub.Instance.PlayPlayerHit();

    if (VFXHub.Instance != null)
        VFXHub.Instance.SpawnPlayerHit(trackedHealth.transform.position);
}
nextDamageTime = Time.time + damageInterval;
    }

    private void ApplyDamage()
    {
if (trackedHealth == null) return;

bool applied = trackedHealth.TakeDamage(damageAmount);
if (applied)
{
    if (AudioHub.Instance != null)
        AudioHub.Instance.PlayPlayerHit();

    if (VFXHub.Instance != null)
        VFXHub.Instance.SpawnPlayerHit(trackedHealth.transform.position);
}
nextDamageTime = Time.time + damageInterval;
    }
}
