/*
EnemyDeath.cs

Design decisions and functionality:
- Purpose: Handle enemy death in a minimal, decoupled way suitable for a prototype.
- Integration with Health: Subscribes to Health.OnZeroHealth and reacts once when health reaches zero.
- Responsibilities:
  - Notify listeners via a new OnDeath event (Action<EnemyDeath>) before destruction. This allows external systems such as the EnemySpawner to track and update living counts without scene-wide searches.
  - Instantiate an optional ExperiencePickup prefab at a valid nearby position (if assigned).
    - First tests the enemy's death position.
    - If the position overlaps Environment obstacles, tries small cardinal + diagonal offsets.
    - Uses the first valid offset or falls back to the original position.
  - Destroy the enemy GameObject.
- Execution guarantees:
  - Uses a hasDied flag so death logic runs only once.
  - Unsubscribes from Health.OnZeroHealth to avoid leaks.
  - Wraps OnDeath invocation in try/catch to avoid cascading errors from subscribers.
- Pickup placement strategy:
  - Validates pickup spawn position using Physics2D.OverlapCircle against Environment LayerMask.
  - Samples up to 8 offset directions (approximately 1.0 unit distance) if death position is blocked.
  - Prioritizes cardinal directions first, then diagonals.
  - Never enters an infinite loop; limited to ~8 attempts max.
  - Fallback: if all offsets blocked, spawns at original position anyway (better visible than missing).
- Decoupling principle: Health remains responsible only for state; EnemyDeath handles spawn/drops and notifications.
- Testing notes:
  - Assign the ExperiencePickup prefab on the Enemy prefab to ensure drops occur. Spawner subscribes to OnDeath for tracking spawned instances.
*/

using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
[DisallowMultipleComponent]
public class EnemyDeath : MonoBehaviour
{
    [SerializeField, Tooltip("Optional ExperiencePickup prefab to spawn when this enemy dies.")]
    private GameObject experiencePickupPrefab;

    [SerializeField, Tooltip("Environment layer mask used to validate pickup spawn position (usually 'Environment').")]
    private LayerMask obstacleLayerMask = 0;

    private Health health;
    private bool hasDied = false;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError($"EnemyDeath requires a Health component on '{gameObject.name}'.", this);
        }

        if (experiencePickupPrefab == null)
        {
            Debug.LogWarning($"EnemyDeath on '{gameObject.name}' has no ExperiencePickup prefab assigned. No pickup will be spawned on death.", this);
        }

        // If obstacleLayerMask not set, try to default to Environment
        if (obstacleLayerMask == 0)
        {
            int env = LayerMask.NameToLayer("Environment");
            if (env != -1) obstacleLayerMask = 1 << env;
        }
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

    public event Action<EnemyDeath> OnDeath;

    private void HandleDeath()
    {
        if (hasDied) return;
        hasDied = true;

        // Unsubscribe immediately to be safe
        if (health != null)
            health.OnZeroHealth -= HandleDeath;

        // Notify listeners that this enemy is dying (before we destroy the GameObject)
        try
        {
            OnDeath?.Invoke(this);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Exception while notifying OnDeath listeners: {ex}", this);
        }

        // Play death sound and VFX. Use boss-specific cues when this GameObject is marked as a Boss.
        if (GetComponent<BossMarker>() != null)
        {
            if (AudioHub.Instance != null) AudioHub.Instance.PlayBossDie();
            if (VFXHub.Instance != null) VFXHub.Instance.SpawnBossDeath(transform.position);
        }
        else
        {
            if (AudioHub.Instance != null) AudioHub.Instance.PlayEnemyDie();
            if (VFXHub.Instance != null) VFXHub.Instance.SpawnEnemyDeath(transform.position);
        }

        // Spawn pickup if assigned
        if (experiencePickupPrefab != null)
        {
            Vector3 pickupPos = FindValidPickupPosition(transform.position);
            Instantiate(experiencePickupPrefab, pickupPos, Quaternion.identity);
        }

        // Destroy the enemy GameObject (immediate destruction not needed; normal Destroy is fine)
        Destroy(gameObject);
    }

    private Vector3 FindValidPickupPosition(Vector3 deathPosition)
    {
        // Get approximate pickup radius from prefab collider
        float pickupRadius = GetPickupRadius();

        // Test death position first
        Collider2D hit = Physics2D.OverlapCircle((Vector2)deathPosition, pickupRadius, obstacleLayerMask);
        if (hit == null)
        {
            // Death position is clear
            return deathPosition;
        }

        // Death position overlaps obstacles; try offset directions (8 cardinal + diagonal)
        Vector2[] offsets = new Vector2[]
        {
            new Vector2(1f, 0f),   // right
            new Vector2(-1f, 0f),  // left
            new Vector2(0f, 1f),   // up
            new Vector2(0f, -1f),  // down
            new Vector2(1f, 1f).normalized,   // up-right
            new Vector2(-1f, 1f).normalized,  // up-left
            new Vector2(1f, -1f).normalized,  // down-right
            new Vector2(-1f, -1f).normalized, // down-left
        };

        float offsetDistance = 1.0f;

        foreach (var offset in offsets)
        {
            Vector2 candidatePos = (Vector2)deathPosition + offset * offsetDistance;
            hit = Physics2D.OverlapCircle(candidatePos, pickupRadius, obstacleLayerMask);
            if (hit == null)
            {
                return (Vector3)candidatePos;
            }
        }

        // All offsets blocked; fallback to original position (visible but may be blocked)
        return deathPosition;
    }

    private float GetPickupRadius()
    {
        if (experiencePickupPrefab == null) return 0.5f;

        Collider2D col = experiencePickupPrefab.GetComponent<Collider2D>() ?? experiencePickupPrefab.GetComponentInChildren<Collider2D>();
        Vector3 scale = experiencePickupPrefab.transform.localScale;

        if (col is BoxCollider2D box)
        {
            Vector2 worldSize = new Vector2(box.size.x * scale.x, box.size.y * scale.y);
            return Mathf.Max(worldSize.x, worldSize.y) * 0.5f;
        }
        else if (col is CircleCollider2D circ)
        {
            return circ.radius * Mathf.Max(scale.x, scale.y);
        }
        else if (col != null)
        {
            // Conservative fallback based on bounds
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
        }

        return 0.5f; // default fallback
    }
}
