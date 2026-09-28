/*
ExperiencePickup.cs

Purpose:
- Controls the experience pickup flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class ExperiencePickup : MonoBehaviour
{
    [SerializeField, Tooltip("Amount of experience awarded when collected (must be > 0).")]
    private int amount = 5;

    private bool collected = false;

    void OnValidate()
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"ExperiencePickup: amount was {amount}. Clamping to 1.", this);
            amount = 1;
        }
    }

    void Awake()
    {
        // nothing to initialize
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other == null) return;

        // Only respond to the Player
        if (!other.CompareTag("Player")) return;

        // Find PlayerExperience on the player (or its parents)
        var xp = other.GetComponent<PlayerExperience>() ?? other.GetComponentInParent<PlayerExperience>();
        if (xp == null)
        {
            Debug.LogWarning($"ExperiencePickup: Player touched pickup but no PlayerExperience component was found on '{other.gameObject.name}'.", this);
            return;
        }

        // Award experience and destroy pickup on success
        bool applied = xp.AddExperience(amount);
        if (applied)
        {
            collected = true;
            Destroy(gameObject);
        }
    }
}
