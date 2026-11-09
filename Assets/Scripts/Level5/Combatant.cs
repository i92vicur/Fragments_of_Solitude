/*
Combatant.cs

Purpose:
- Controls the combatant flow for the turn-based battle loop, combatants, battle UI, and encounter rules.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class Combatant : MonoBehaviour
{
    [SerializeField, Tooltip("Unique display name, e.g. WARRIOR or FINAL ENEMY.")]
    private string combatantName = "COMBATANT";

    [SerializeField, Tooltip("True for the enemy side. Player action UI is only usable on player turns.")]
    private bool isEnemy = false;

    [SerializeField, Tooltip("Maximum HP. Must be > 0. Current HP starts at this value.")]
    private int maxHP = 100;

    [SerializeField, Tooltip("Attack stat used by the future combat system.")]
    private int attack = 10;

    [SerializeField, Tooltip("Defense stat used by the future combat system.")]
    private int defense = 10;

    [SerializeField, Tooltip("Read-only at runtime. Shown so HP can be inspected during play.")]
    private int currentHP;

    [SerializeField, Tooltip("True only when the Animator has a working Attack state. Keeps Attack off for characters without a clip.")]
    private bool hasAttackAnimation = false;

    // Temporary bonus from DEFEND. Never stacks: a new DEFEND replaces it, and it is consumed by the next hit taken.
    private int temporaryDefenseBonus;

    private Animator animator;
    private bool deathEmitted;

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int AttackStateHash = Animator.StringToHash("Attack");
    private static readonly int DeadTriggerHash = Animator.StringToHash("Dead");

    /// <summary>Raised exactly once when HP drops from positive to zero (reset by <see cref="ResetToFullHP"/>).</summary>
    public event System.Action<Combatant> OnDied;

    public Animator Animator => animator;
    public string CombatantName => combatantName;
    public bool IsEnemy => isEnemy;
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Attack => attack;
    public int Defense => defense;

    /// <summary>Base Defense plus any active DEFEND bonus. Used by the damage formula instead of Defense.</summary>
    public int EffectiveDefense => defense + temporaryDefenseBonus;
    public bool IsAlive => currentHP > 0;

    private void Awake()
    {
        ResetToFullHP();

        // Enemies keep their own animation setup, so only players play the Dead trigger.
        if (!isEnemy)
            animator = GetComponent<Animator>();

        OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        OnDied -= HandleDied;
    }

    private void OnValidate()
    {
        if (maxHP < 1) maxHP = 1;
        if (attack < 0) attack = 0;
        if (defense < 0) defense = 0;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    /// <summary>
    /// Removes HP and returns how much was actually applied (0 if already dead or amount was not positive).
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
            return 0;

        int applied = Mathf.Min(amount, currentHP);
        currentHP -= applied;

        if (!deathEmitted && !IsAlive)
        {
            deathEmitted = true;
            OnDied?.Invoke(this);
        }

        return applied;
    }

    public void ResetToFullHP()
    {
        currentHP = maxHP;
        deathEmitted = false;
    }

    /// <summary>
    /// Plays the visual Attack animation. Visual only: it does not change damage, misses or turn order.
    /// No-ops for enemies, dead combatants, characters without an Attack clip, or while Attack is already playing.
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (!hasAttackAnimation || isEnemy || !IsAlive || animator == null)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash == AttackStateHash && !animator.IsInTransition(0))
            return;

        animator.SetTrigger(AttackTriggerHash);
    }

    private void HandleDied(Combatant _)
    {
        if (animator == null)
            return;

        // Drop any Attack still queued so it cannot play after death.
        animator.ResetTrigger(AttackTriggerHash);
        animator.SetTrigger(DeadTriggerHash);
    }

    /// <summary>
    /// Restores HP and returns how much was actually applied (0 if already at MaxHP or amount was not positive).
    /// </summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || currentHP >= maxHP)
            return 0;

        int applied = Mathf.Min(amount, maxHP - currentHP);
        currentHP += applied;
        return applied;
    }

    /// <summary>
    /// Sets (not adds) the temporary DEFEND bonus, so repeated DEFEND presses never stack.
    /// </summary>
    public void ApplyDefendBonus(int bonus)
    {
        temporaryDefenseBonus = bonus;
    }

    /// <summary>Clears the temporary DEFEND bonus. Called once it has protected against a hit.</summary>
    public void ClearDefendBonus()
    {
        temporaryDefenseBonus = 0;
    }
}
