/*
PlayerExperience.cs

Design decisions and functionality:
- Purpose: Minimal player experience and leveling tracker for the prototype.
- Data model: Tracks integer Level, CurrentExperience, and ExperienceToNext (ints).
- Initialization: Starts at Level 1 with zero experience. Base requirement (baseRequirement) is configurable and validated.
- Level up formula: Uses simple linear scaling: required = baseRequirement * currentLevel. This is predictable and easy to test.
- Experience application: AddExperience(amount) validates input, adds experience, and loops to handle multiple level-ups in one award. Excess experience carries into the next level.
- Notifications: Exposes OnLevelUp(newLevel) and OnExperienceChanged(currentExp, expToNext) for UI or other systems to react.
- Encapsulation: State is exposed through read-only properties; methods are explicit rather than public field writes.
- Testing: Call AddExperience from Console or pickups to verify level-ups and events.
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
        bool leveled = false;

        // Loop to handle multiple level-ups from a single award
        while (currentExp >= expToNext)
        {
            currentExp -= expToNext;
            level++;
            expToNext = ComputeExpToNext(level);
            leveled = true;
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
