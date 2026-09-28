/*
RegularEnemyAnimationController.cs

Purpose:
- Controls the regular enemy animation controller flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyContactDamage))]
public class RegularEnemyAnimationController : MonoBehaviour
{
    [SerializeField, Tooltip("How long the hit animation is shown after taking damage.")]
    private float hitAnimationDuration = 0.25f;

    private Animator animator;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Health health;
    private EnemyContactDamage contactDamage;
    private string currentState;
    private float hitTimer;
    private int lastHealth;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        contactDamage = GetComponent<EnemyContactDamage>();
        hitAnimationDuration = Mathf.Max(0f, hitAnimationDuration);
        lastHealth = health.CurrentHealth;
    }

    private void OnEnable()
    {
        health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        UpdateFacing();

        if (hitTimer > 0f)
            hitTimer = Mathf.Max(0f, hitTimer - Time.deltaTime);

        string state = hitTimer > 0f ? "Hit" : contactDamage.IsAttacking ? "Attack" : "Run";
        if (state == currentState)
            return;

        currentState = state;
        animator.Play("Base Layer." + state, 0, 0f);
    }

    private void UpdateFacing()
    {
        if (Mathf.Abs(body.linearVelocity.x) > 0.01f)
            spriteRenderer.flipX = body.linearVelocity.x < 0f;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth < lastHealth)
            hitTimer = hitAnimationDuration;

        lastHealth = currentHealth;
    }
}
