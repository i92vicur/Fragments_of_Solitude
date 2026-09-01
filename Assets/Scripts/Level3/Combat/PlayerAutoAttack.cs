/*
PlayerAutoAttack.cs

Purpose: Minimal automatic ranged attack system that periodically finds the nearest enemy and
fires a projectile.

Design decisions:
- Uses Physics2D.OverlapCircleAll to locate nearby enemy candidates efficiently for a small prototype.
- Keeps regular fire simple: one projectile toward the nearest valid target per cooldown.
- Reuse-friendly API: exposes the currently configured projectile prefab and current projectile
  damage bonus so other player combat systems (such as the boss mega attack) can reuse the same
  projectile content without duplicating prefab references or upgrade state.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField, Tooltip("Projectile prefab to instantiate (assign in Inspector).")]
    private GameObject projectilePrefab;

    [SerializeField, Tooltip("Optional spawn point for projectiles. If null, player's position will be used.")]
    private Transform projectileSpawnPoint;

    [SerializeField, Tooltip("Attack cooldown in seconds between shots.")]
    private float attackInterval = 0.5f;

    [SerializeField, Tooltip("Attack range in world units.")]
    private float attackRange = 8f;

    [SerializeField, Tooltip("Layer mask used to select candidate targets (enemies).")]
    private LayerMask targetLayerMask = 0;

    [SerializeField, Tooltip("Optional tag to require on targets. Leave empty to ignore tag.")]
    private string requiredTargetTag = "";

    private float cooldownTimer = 0f;
    private int extraProjectileDamage = 0;

    [SerializeField, Tooltip("Minimum allowed attack interval (seconds).")]
    private float minAttackInterval = 0.1f;

    private bool fatalErrorDisabled = false;

    void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("PlayerAutoAttack: projectilePrefab is not assigned. Disabling auto-attack.", this);
            enabled = false;
            fatalErrorDisabled = true;
            return;
        }

        if (attackInterval <= 0f) attackInterval = 0.1f;
        if (attackRange <= 0f) attackRange = 1f;

        if (targetLayerMask == 0)
        {
            Debug.LogWarning("PlayerAutoAttack: targetLayerMask is empty. No targets will be found until you select the Enemy layer in the mask.", this);
        }
    }

    void Update()
    {
        if (fatalErrorDisabled) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        Vector2 origin = (projectileSpawnPoint != null) ? (Vector2)projectileSpawnPoint.position : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange, targetLayerMask);
        if (hits == null || hits.Length == 0) return;

        Collider2D nearest = null;
        float bestSqr = float.MaxValue;

        foreach (var c in hits)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (!string.IsNullOrEmpty(requiredTargetTag) && !c.CompareTag(requiredTargetTag)) continue;

            var health = c.GetComponent<Health>() ?? c.GetComponentInParent<Health>();
            if (health == null || health.IsDead) continue;

            float sqr = ((Vector2)c.transform.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = c;
            }
        }

        if (nearest == null) return;

        var targetHealth = nearest.GetComponent<Health>() ?? nearest.GetComponentInParent<Health>();
        if (targetHealth == null || targetHealth.IsDead) return;

        Vector2 spawnPos = (projectileSpawnPoint != null) ? (Vector2)projectileSpawnPoint.position : (Vector2)transform.position;
        Vector2 dir = (Vector2)nearest.transform.position - spawnPos;
        if (dir.sqrMagnitude <= 0f) dir = transform.right;
        dir.Normalize();

        var projGO = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        if (projGO == null)
        {
            Debug.LogError("PlayerAutoAttack: Failed to instantiate projectile prefab.", this);
            return;
        }

        var projComp = projGO.GetComponent<PlayerProjectile>();
        if (projComp == null)
        {
            Debug.LogError("PlayerAutoAttack: Instantiated projectile prefab does not contain PlayerProjectile component. Disabling auto-attack.", this);
            Destroy(projGO);
            enabled = false;
            fatalErrorDisabled = true;
            return;
        }

        projComp.Initialize(dir);
        if (extraProjectileDamage != 0)
            projComp.SetDamage(projComp.GetDamage() + extraProjectileDamage);

        if (AudioHub.Instance != null)
            AudioHub.Instance.PlayPlayerShoot();

        cooldownTimer = attackInterval;
    }

    public void IncreaseProjectileDamage(int amount)
    {
        if (amount <= 0) return;
        extraProjectileDamage += amount;
    }

    public void ReduceAttackInterval(float delta)
    {
        if (delta <= 0f) return;
        attackInterval = Mathf.Max(minAttackInterval, attackInterval - delta);
    }

    public GameObject GetProjectilePrefab()
    {
        return projectilePrefab;
    }

    public int GetAdditionalProjectileDamage()
    {
        return extraProjectileDamage;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Vector3 origin = (projectileSpawnPoint != null) ? projectileSpawnPoint.position : transform.position;
        Gizmos.DrawSphere(origin, 0.05f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.12f);
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}