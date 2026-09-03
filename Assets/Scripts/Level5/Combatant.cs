/*
Combatant.cs

Purpose:
- Minimal battle data holder for the Level 5 turn-based prototype. One component represents either a
  playable character or the final enemy.

Design decisions:
- Self-contained: deliberately does NOT reuse Level3's Health.cs. That component is wired into the
  roguelite's real-time damage, audio and VFX systems; keeping Level 5 separate avoids coupling the
  prototype to gameplay that will never run in a battle scene.
- Plain MonoBehaviour with [SerializeField] values, matching the project's existing data-component style.
  No ScriptableObject asset: the only ScriptableObject in the project (LevelMapData) is level geometry,
  not stats, so there is no existing pattern that fits here.
- Integer stats only. No experience, levels, equipment, mana, crits, status effects or elements.
- Defense is stored but not applied here. Damage mitigation is a combat-system decision and belongs to
  the (not yet written) turn system, so TakeDamage applies the amount it is given.

Functionality:
- CurrentHP starts at MaxHP and is always clamped to [0, MaxHP].
- TakeDamage(int) returns the amount actually removed, so the future combat system can report it.
- IsAlive is false once CurrentHP reaches 0.

Usage:
- Attach to each character placeholder and to the enemy placeholder in Level5.unity, then set
  Combatant Name / Max HP / Attack / Defense in the Inspector.
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

    // Temporary bonus from DEFEND. Never stacks: a new DEFEND replaces it, and it is consumed by the next hit taken.
    private int temporaryDefenseBonus;

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
        return applied;
    }

    public void ResetToFullHP()
    {
        currentHP = maxHP;
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
