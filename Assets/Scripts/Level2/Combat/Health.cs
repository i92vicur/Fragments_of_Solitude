/*
Health.cs

Purpose:
- Controls the health flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
