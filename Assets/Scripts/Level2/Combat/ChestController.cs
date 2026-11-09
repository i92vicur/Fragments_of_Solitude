/*
ChestController.cs

Purpose:
- Controls the chest controller flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

public enum ChestReward
{
    HealthRecovery,
    BossAttackPiece
}

public class ChestImpactCounter
{
    private readonly int hitsToOpen;
    private int hitsTaken;

    public ChestImpactCounter(int hitsToOpen)
    {
        this.hitsToOpen = hitsToOpen;
    }

    public bool IsOpened { get; private set; }

    // Returns true only for the impact that opens the chest.
    public bool RegisterHit()
    {
        if (IsOpened) return false;

        hitsTaken++;
        if (hitsTaken < hitsToOpen) return false;

        IsOpened = true;
        return true;
    }
}

[DisallowMultipleComponent]
public class ChestController : MonoBehaviour
{
    [SerializeField, Tooltip("Amount of HP restored when the HealthRecovery reward is selected (must be > 0).")]
    private int healthHealAmount = 3;

    private const int HitsToOpen = 3;

    private readonly ChestImpactCounter impacts = new ChestImpactCounter(HitsToOpen);
    private ChestReward? assignedReward;

    // Set by ChestSpawner so the chests of a run contain a fixed mix of rewards.
    public void SetReward(ChestReward reward)
    {
        assignedReward = reward;
    }

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
        if (impacts.IsOpened) return;
        if (other == null) return;
        if (!other.TryGetComponent<PlayerProjectile>(out _)) return;

        Destroy(other.gameObject);
        if (!impacts.RegisterHit()) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("ChestController: no GameObject tagged 'Player' found; reward not applied.", this);
            Destroy(gameObject);
            return;
        }

        ChestReward reward = assignedReward ?? (Random.value < 0.5f ? ChestReward.HealthRecovery : ChestReward.BossAttackPiece);
        ApplyReward(reward, player);

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
