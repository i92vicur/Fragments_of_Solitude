/*
BossAnimationController.cs

Purpose: Select the boss Animator state from movement, contact-attack and recent damage state.

Design decisions:
- Health.OnHealthChanged starts a short hurt window; hurt takes priority over attack and run until it expires.
- EnemyContactDamage supplies the attack state, while this component only translates gameplay state into
    animation names and does not apply damage or movement.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyContactDamage))]
public class BossAnimationController : MonoBehaviour
{
    [SerializeField] private float hurtAnimationDuration = 0.25f;

    private Animator animator;
    private Health health;
    private EnemyContactDamage contactDamage;
    private string currentState;
    private float hurtTimer;
    private int lastHealth;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        contactDamage = GetComponent<EnemyContactDamage>();
        hurtAnimationDuration = Mathf.Max(0f, hurtAnimationDuration);
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
        hurtTimer = Mathf.Max(0f, hurtTimer - Time.deltaTime);
        string state = hurtTimer > 0f ? "Boss_Hurt" : contactDamage.IsAttacking ? "Boss_Attack" : "Boss_Run";
        if (state == currentState)
            return;

        currentState = state;
        animator.Play("Base Layer." + state, 0, 0f);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth < lastHealth)
            hurtTimer = hurtAnimationDuration;

        lastHealth = currentHealth;
    }
}
