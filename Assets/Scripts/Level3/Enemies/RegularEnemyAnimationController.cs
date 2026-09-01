/*
RegularEnemyAnimationController.cs

Purpose: Drive regular enemy Animator state, horizontal facing and temporary hit feedback.

Design decisions:
- Rigidbody2D velocity determines whether the enemy is running; EnemyContactDamage determines attack.
- Damage events start a hit timer that takes priority over attack and run, and SpriteRenderer.flipX mirrors
    the horizontal movement without changing the transform.
- The controller is presentation-only: it observes Health and never changes combat state.
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
