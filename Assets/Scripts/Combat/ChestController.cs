/*
ChestController.cs

Purpose: Reusable chest that gives the player a random reward when touched, then removes itself.

Design decisions:
- Trigger-based: Uses OnTriggerEnter2D to detect the Player (tag "Player").
- One-time activation: A 'consumed' flag prevents re-triggering if physics fires the callback twice.
- 50/50 reward: Random.value < 0.5f selects between HealthRecovery and BossAttackPiece.
  Additional reward types can be added to ChestReward and a new case in ApplyReward without
  touching the detection or destruction logic.
- Reward isolation: ApplyReward is separate from detection so rewards can be tested independently.
- No unique scripts per chest: One prefab, one script; every chest instance is independent.
- Self-destruction: gameObject is destroyed after reward is applied so the chest cannot trigger again
  even if the collected flag somehow failed.

Configuration:
- healthHealAmount: HP restored for the HealthRecovery reward (default 3, configurable in Inspector).

Prefab setup (do manually in Unity):
1. Create a new 2D GameObject with a visible sprite.
2. Add BoxCollider2D, set isTrigger = true.
3. Add Rigidbody2D, Body Type: Kinematic, Gravity Scale: 0.
4. Attach this ChestController component.
5. Save as Assets/Prefabs/Chest.prefab.

Usage:
- Place Chest.prefab instances in the scene.
- Player must have Health and PlayerBossAttackPieces components.
*/

using UnityEngine;

public enum ChestReward
{
    HealthRecovery,
    BossAttackPiece
}

[DisallowMultipleComponent]
public class ChestController : MonoBehaviour
{
    [SerializeField, Tooltip("Amount of HP restored when the HealthRecovery reward is selected (must be > 0).")]
    private int healthHealAmount = 3;

    private bool consumed = false;

    void OnValidate()
    {
        if (healthHealAmount <= 0)
        {
            Debug.LogWarning($"ChestController: healthHealAmount was {healthHealAmount}. Clamping to 1.", this);
            healthHealAmount = 1;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!other.CompareTag("Player")) return;

        consumed = true;

        ChestReward reward = Random.value < 0.5f ? ChestReward.HealthRecovery : ChestReward.BossAttackPiece;
        ApplyReward(reward, other.gameObject);

        Destroy(gameObject);
    }

    private void ApplyReward(ChestReward reward, GameObject player)
    {
        switch (reward)
        {
            case ChestReward.HealthRecovery:
                var health = player.GetComponent<Health>() ?? player.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.Heal(healthHealAmount);
                    Debug.Log($"Chest reward: HealthRecovery (+{healthHealAmount} HP).");
                }
                else
                {
                    Debug.LogWarning("ChestController: HealthRecovery reward selected but no Health component found on Player.", this);
                }
                break;

            case ChestReward.BossAttackPiece:
                var pieces = player.GetComponent<PlayerBossAttackPieces>()
                             ?? player.GetComponentInParent<PlayerBossAttackPieces>();
                if (pieces != null)
                {
                    pieces.AddPiece();
                    Debug.Log(
                        $"Chest reward: BossAttackPiece awarded ({pieces.PiecesCollected}/{PlayerBossAttackPieces.RequiredPiecesForMegaAttack}).");
                }
                else
                {
                    Debug.LogWarning("ChestController: BossAttackPiece reward selected but no PlayerBossAttackPieces component found on Player.", this);
                }
                break;
        }
    }
}
