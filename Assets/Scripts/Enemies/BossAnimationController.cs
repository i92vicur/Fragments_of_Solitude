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
