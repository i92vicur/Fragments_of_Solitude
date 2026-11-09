/*
EnemyContactDamage.cs

Purpose:
- Controls the enemy contact damage flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
