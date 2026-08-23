/*
PlayerProjectile.cs

Purpose: A simple projectile that travels in a straight line, detects collisions, and applies
player damage to enemies.

Design decisions:
- Kinematic Rigidbody2D + trigger Collider2D for deterministic 2D projectile behaviour.
- Spawn-time configuration: damage can already be modified after instantiation; this script now
  also supports optional speed/lifetime overrides for special attacks like the boss mega attack.
- Lifetime cleanup: Destroy(gameObject, lifetime) is scheduled in Start. Call Initialize(...) or
  Initialize(..., overrideSpeed, overrideLifetime) immediately after Instantiate so special values
  are applied before Start runs.
- Simplicity: No piercing, no splash damage, no ricochet, no pooling.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [SerializeField, Tooltip("Speed in units/sec.")]
    private float speed = 10f;

    [SerializeField, Tooltip("Damage dealt to an enemy on hit.")]
    private int damage = 1;

    [SerializeField, Tooltip("Lifetime in seconds before self-destruction if nothing hit.")]
    private float lifetime = 5f;

    [SerializeField, Tooltip("Tag used to identify enemies.")]
    private string enemyTag = "Enemy";

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerProjectile requires a Rigidbody2D.", this);
            enabled = false;
            return;
        }

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("PlayerProjectile requires a Collider2D.", this);
            enabled = false;
            return;
        }

        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);

        if (!initialized)
        {
            direction = transform.right;
            rb.linearVelocity = direction.normalized * speed;
            initialized = true;
        }
    }

    public int GetDamage()
    {
        return damage;
    }

    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(0, newDamage);
    }

    public void Initialize(Vector2 travelDirection)
    {
        if (travelDirection.sqrMagnitude <= 0f)
        {
            Debug.LogWarning("PlayerProjectile.Initialize called with zero direction. Defaulting to transform.right.", this);
            direction = transform.right;
        }
        else
        {
            direction = travelDirection.normalized;
        }

        rb.linearVelocity = direction * speed;
        initialized = true;
    }

    public void Initialize(Vector2 travelDirection, float overrideSpeed, float overrideLifetime)
    {
        if (overrideSpeed > 0f)
            speed = overrideSpeed;

        if (overrideLifetime > 0f)
            lifetime = overrideLifetime;

        Initialize(travelDirection);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag("Player")) return;

        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer != -1 && other.gameObject.layer == envLayer)
        {
            if (VFXHub.Instance != null)
                VFXHub.Instance.SpawnProjectileImpact(other.ClosestPoint(transform.position));

            if (AudioHub.Instance != null)
                AudioHub.Instance.PlayImpact();

            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag(enemyTag)) return;

        var health = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health == null)
        {
            Debug.LogWarning($"PlayerProjectile hit object tagged '{enemyTag}' but no Health component was found.", other);
            return;
        }

        bool applied = health.TakeDamage(damage);
        if (applied)
        {
            if (AudioHub.Instance != null)
                AudioHub.Instance.PlayEnemyHit();

            if (VFXHub.Instance != null)
                VFXHub.Instance.SpawnProjectileImpact(other.transform.position);
        }

        Destroy(gameObject);
    }
}