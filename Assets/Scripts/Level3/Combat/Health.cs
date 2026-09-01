/*
Health.cs

Purpose: Centralized health state and damage system for any character (player, enemy, boss).

Design decisions:
- State model: Tracks currentHealth and maxHealth as integers. Health is clamped to [0, maxHealth].
- Events: Exposes two events:
  - OnZeroHealth: Fired once when health reaches zero (useful for death triggers).
  - OnHealthChanged(int, int): Fired whenever health changes, transmitting (currentHealth, maxHealth) for UI updates.
- Event safety: OnZeroHealth only fires once per death state transition (hasInvokedZero flag) to prevent repeated invocations.
- Damage application: TakeDamage(amount) decrements health by amount. Negative/zero amounts are rejected with a warning. Damage cannot be applied if already dead.
- Healing capability: Heal(amount) is not exposed in this minimal version; future versions can add it if needed for pickups or buffs.
- Initialization: Health is set to maxHealth at Awake and OnHealthChanged is immediately notified so UI can initialize.
- Simplicity: No damage types, armor, or special logic; pure numerical subtraction suitable for a prototype.

Configuration:
- maxHealth: Must be > 0. Clamped to 1 if set to <= 0.

Usage:
- Attach to any GameObject (Player, Enemy, Boss).
- Subscribe to OnZeroHealth for death logic (EnemyDeath, PlayerDeath).
- Subscribe to OnHealthChanged for HUD or health bar UI.

Notes:
- Requires all damage sources to find and call GetComponent<Health>().TakeDamage().
- No healing, shield, or invulnerability mechanics (can be added as upgrade systems later).
*/

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField, Tooltip("Maximum health. Must be > 0.")]
    private int maxHealth = 10;

    private int currentHealth;

    // Read-only public properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    // Event invoked when health reaches zero (only invoked once per death state transition)
    public event Action OnZeroHealth;

    // Event invoked whenever current health or max health changes (current, max)
    public event Action<int, int> OnHealthChanged;

    private bool hasInvokedZero = false;

    void OnValidate()
    {
        if (maxHealth <= 0)
        {
            Debug.LogWarning($"Health: maxHealth was {maxHealth}. Clamping to 1.", this);
            maxHealth = 1;
        }
    }

    void Awake()
    {
        // Initialize current health early so other components can read a valid value in Start/OnEnable.
        currentHealth = maxHealth;
        hasInvokedZero = currentHealth <= 0;

        // Notify listeners of initial health state
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Apply positive damage. Returns true if damage was applied.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Health.TakeDamage called with non-positive amount: {amount}", this);
            return false;
        }

        if (currentHealth <= 0) return false; // already dead

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (!hasInvokedZero)
            {
                hasInvokedZero = true;
                OnZeroHealth?.Invoke();
            }
        }

        // Notify listeners that health changed
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        return true;
    }

    /// <summary>
    /// Restore positive health. Returns true if healing was applied.
    /// </summary>
    public bool Heal(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Health.Heal called with non-positive amount: {amount}", this);
            return false;
        }

        if (currentHealth <= 0)
        {
            // allow healing from zero, but ensure OnZeroHealth state resets if revived
            currentHealth += amount;
        }
        else
        {
            currentHealth += amount;
        }

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (currentHealth > 0 && hasInvokedZero)
            hasInvokedZero = false;

        // Notify listeners that health changed
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        return true;
    }

    // Optional: allow setting current health directly within range (not public set)
    public void SetToMax()
    {
        currentHealth = maxHealth;
        hasInvokedZero = currentHealth <= 0;

        // Notify listeners that health changed
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
