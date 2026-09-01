/*
ExperiencePickup.cs

Purpose: A simple collectible that awards experience to the Player when touched, then self-destructs.

Design decisions:
- Trigger-based collection: Uses OnTriggerEnter2D for collision detection. Expects the Player to have a Rigidbody2D; the pickup itself typically uses a kinematic Rigidbody2D.
- One-time award: Tracks collection state with a collected flag so experience is awarded only once per pickup.
- Player detection: On trigger enter, checks if collider is tagged "Player" to avoid accidental awards to enemies or other objects.
- Experience lookup: Finds PlayerExperience via GetComponent or GetComponentInParent to handle both direct attachment and parent-based patterns.
- Safety:
  - Validates amount is > 0 in OnValidate (clamped to 1 if invalid).
  - Logs a clear warning if PlayerExperience is not found but does not throw repeated errors.
  - Returns early if collected flag is set, preventing multi-trigger awards.
- Simplicity: No magnetism (player must touch to collect), no animation, no pooling—just collision and destruction.
- Extensibility: Amount is configurable per pickup instance, allowing for difficulty scaling or bonus drops later.

Configuration:
- amount: Experience awarded (default 5). Set per-instance or per-prefab.

Prefab setup:
- Create a GameObject with a small sprite (visual representation).
- Add BoxCollider2D or CircleCollider2D, set isTrigger = true.
- Attach a Rigidbody2D (Body Type: Kinematic, no gravity).
- Attach this ExperiencePickup component, configure amount.
- Save as prefab.

Usage:
- EnemyDeath spawns this prefab when enemies die.
- Player moves into pickup collision → OnTriggerEnter2D → AddExperience called → pickup destroyed.

Notes:
- Pickup spawning position is validated by EnemyDeath to avoid placing pickups inside walls (see EnemyDeath for details).
- No visual feedback on collection; future versions can add particles or sound via hubs.
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
