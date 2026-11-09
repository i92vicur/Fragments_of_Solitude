/*
PlayerExperience.cs

Purpose:
- Controls the player experience flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    [SerializeField, Tooltip("Base experience required for level 1 -> 2. Actual requirement scales with level using the formula: baseRequirement * level.")]
    private int baseRequirement = 10;

    private int level = 1;
    private int currentExp = 0;
    private int expToNext;

    // Read-only public properties
    public int Level => level;
    public int CurrentExperience => currentExp;
    public int ExperienceToNext => expToNext;

    // Events for UI or other systems
    public event Action<int> OnLevelUp; // new level
    public event Action<int, int> OnExperienceChanged; // currentExp, expToNext

    void OnValidate()
    {
        if (baseRequirement <= 0)
        {
            Debug.LogWarning($"PlayerExperience: baseRequirement was {baseRequirement}. Clamping to 1.", this);
            baseRequirement = 1;
        }
    }

    void Awake()
    {
        if (baseRequirement <= 0) baseRequirement = 1;
        level = 1;
        currentExp = 0;
        expToNext = ComputeExpToNext(level);
    }

    private int ComputeExpToNext(int atLevel)
    {
        // Simple linear scaling: required = baseRequirement * level
        // Level 1 -> requires baseRequirement * 1
        long req = (long)baseRequirement * (long)atLevel;
        if (req > int.MaxValue) return int.MaxValue;
        return (int)req;
    }

    /// <summary>
    /// Add a positive amount of experience. Returns true if experience was applied.
    /// Levels up as many times as necessary, carrying excess experience.
    /// </summary>
    public bool AddExperience(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"PlayerExperience.AddExperience called with non-positive amount: {amount}", this);
            return false;
        }

        currentExp += amount;

        // Loop to handle multiple level-ups from a single award
        while (currentExp >= expToNext)
        {
            currentExp -= expToNext;
            level++;
            expToNext = ComputeExpToNext(level);
            Debug.Log($"Player leveled up to {level}.");
            OnLevelUp?.Invoke(level);
        }

        OnExperienceChanged?.Invoke(currentExp, expToNext);

        return true;
    }

    /// <summary>
    /// Helper to set experience to zero and level to 1.
    /// </summary>
    public void ResetExperience()
    {
        level = 1;
        currentExp = 0;
        expToNext = ComputeExpToNext(level);
        OnExperienceChanged?.Invoke(currentExp, expToNext);
    }
}
