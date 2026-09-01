/*
LevelUpController.cs

Purpose: Coordinate the level-up flow (pause, queue pending level-ups, select upgrades, apply buffs).

Design decisions:
- Event-driven queuing: Subscribes to PlayerExperience.OnLevelUp(newLevel) events. Each level-up increments pendingLevelUps counter so multiple level-ups from a single experience award are preserved.
- Pause coordination: When a level-up is pending, pauses the game via PauseManager.Pause(PauseReason.LevelUp).
  - Time.timeScale set to 0 so physics, animations, and enemies pause.
  - Only resumes when all pending level-ups are consumed AND the player is still alive (checks Health.IsDead).
- Upgrade selection: Three simple upgrade types (defined in UpgradeType enum):
  1. MovementSpeed: Increases player moveSpeed by configurable amount.
  2. AttackSpeed: Reduces attack interval by configurable amount (faster attacks).
  3. ProjectileDamage: Increases projectile damage by configurable amount.
- Upgrade application: Modifies target components via explicit public methods (e.g., PlayerMovement.SetMoveSpeed, PlayerAutoAttack.SetAttackInterval) rather than exposing fields.
- Temporary testing input: Keyboard 1/2/3 to select upgrades while paused. Explicitly marked temporary; should be removed when UI is added.
- Component caching: Finds PlayerExperience, PlayerMovement, PlayerAutoAttack, and Health on Awake/Start so selections are fast.
- Safety: Disables itself if PlayerExperience is missing; defensive null checks before applying upgrades.
- Event for UI: OnPendingLevelUpsChanged(count) notifies UI of pending level-ups for display.

Configuration:
- playerExperience: Optional (auto-finds on Player if not assigned).
- movementSpeedIncrease: Bonus per upgrade (default 1.0 unit/sec).
- attackIntervalReduction: Reduction in attack cooldown per upgrade (default 0.1 sec).
- projectileDamageIncrease: Damage bonus per upgrade (default 1).

Upgrade flow example:
1. Player gains 15 XP and levels up → pendingLevelUps = 1.
2. LevelUpController pauses and waits for SelectUpgrade call.
3. Player presses 1 (or UI button) → SelectUpgrade(UpgradeType.MovementSpeed).
4. Movement speed increased, pendingLevelUps decremented to 0.
5. All pending resolved and player alive → game unpauses.

Notes:
- Keep this controller minimal. Complex upgrade effects belong in separate upgrade systems later.
- Replace keyboard input with UI event handlers when HUD upgrades UI is added.
- The ContextMenu test helper is temporary and should be removed.
*/

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelUpController : MonoBehaviour
{
    [SerializeField, Tooltip("PlayerExperience component to observe. If left empty and this component is on the Player, it will try GetComponent<PlayerExperience>().")]
    private PlayerExperience playerExperience;

    private Health playerHealth;
    private PlayerMovement playerMovement;
    private PlayerAutoAttack playerAutoAttack;
    private int pendingLevelUps = 0;

    public bool IsLevelUpPending => pendingLevelUps > 0;
    public int PendingCount => pendingLevelUps;

    // Event for UI or editor hooks
    public event Action<int> OnPendingLevelUpsChanged; // pending count

    public enum UpgradeType
    {
        MovementSpeed = 1,
        AttackSpeed = 2,
        ProjectileDamage = 3
    }

    // Upgrade tuning values (small, sensible defaults)
    [SerializeField, Tooltip("How much movement speed to add when selecting MovementSpeed upgrade.")]
    private float movementSpeedIncrease = 1f;

    [SerializeField, Tooltip("How much to reduce attack interval (seconds) when selecting AttackSpeed upgrade.")]
    private float attackIntervalReduction = 0.1f;

    [SerializeField, Tooltip("How much projectile damage to add when selecting ProjectileDamage upgrade.")]
    private int projectileDamageIncrease = 1;

    void Awake()
    {
        if (playerExperience == null)
        {
            // If this controller is attached to the Player, try to find the component locally
            playerExperience = GetComponent<PlayerExperience>();
        }

        if (playerExperience == null)
        {
            Debug.LogWarning("LevelUpController: playerExperience is not assigned and was not found on this GameObject. LevelUpController will be disabled.", this);
            enabled = false;
            return;
        }

        // Try to cache player's Health if present (used to avoid resuming after death)
        playerHealth = playerExperience.GetComponent<Health>();
        playerMovement = playerExperience.GetComponent<PlayerMovement>();
        playerAutoAttack = playerExperience.GetComponent<PlayerAutoAttack>();
    }

    void OnEnable()
    {
        if (playerExperience != null)
            playerExperience.OnLevelUp += HandleLevelUp;
    }

    void OnDisable()
    {
        if (playerExperience != null)
            playerExperience.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        // Queue one pending level-up per event invocation
        pendingLevelUps++;
        OnPendingLevelUpsChanged?.Invoke(pendingLevelUps);

        Debug.Log($"Level up! New level: {newLevel}. Pending level-ups: {pendingLevelUps}");

        // Play optional level-up audio
        if (AudioHub.Instance != null)
            AudioHub.Instance.PlayLevelUp();

        // Pause the game when first pending level-up is queued using PauseManager
        if (pendingLevelUps == 1)
        {
            PauseManager.Instance.Pause(PauseManager.PauseReason.LevelUp);
        }
    }

    /// <summary>
    /// Select an upgrade to apply for one pending level-up. Returns true if selection succeeded and consumed one pending.
    /// </summary>
    public bool SelectUpgrade(UpgradeType choice)
    {
        if (pendingLevelUps <= 0)
        {
            Debug.LogWarning("No pending level-ups to select an upgrade for.", this);
            return false;
        }

        switch (choice)
        {
            case UpgradeType.MovementSpeed:
                if (playerMovement == null)
                {
                    Debug.LogWarning("MovementSpeed upgrade selected but PlayerMovement component not found.", this);
                    return false;
                }
                playerMovement.IncreaseMoveSpeed(movementSpeedIncrease);
                Debug.Log($"Applied MovementSpeed upgrade: +{movementSpeedIncrease} speed.");
                break;

            case UpgradeType.AttackSpeed:
                if (playerAutoAttack == null)
                {
                    Debug.LogWarning("AttackSpeed upgrade selected but PlayerAutoAttack component not found.", this);
                    return false;
                }
                playerAutoAttack.ReduceAttackInterval(attackIntervalReduction);
                Debug.Log($"Applied AttackSpeed upgrade: -{attackIntervalReduction}s interval.");
                break;

            case UpgradeType.ProjectileDamage:
                if (playerAutoAttack == null)
                {
                    Debug.LogWarning("ProjectileDamage upgrade selected but PlayerAutoAttack component not found.", this);
                    return false;
                }
                playerAutoAttack.IncreaseProjectileDamage(projectileDamageIncrease);
                Debug.Log($"Applied ProjectileDamage upgrade: +{projectileDamageIncrease} damage to future projectiles.");
                break;

            default:
                Debug.LogWarning("Unknown upgrade selected.", this);
                return false;
        }

        // Consume one pending level-up and possibly resume
        CompleteOneLevelUp();
        return true;
    }

    // --- Public API for UI to call ---

    /// <summary>
    /// UI-callable method to select Movement Speed upgrade. Returns true if the pending level-up was consumed.
    /// </summary>
    public bool SelectMovementSpeedUpgrade()
    {
        return SelectUpgrade(UpgradeType.MovementSpeed);
    }

    /// <summary>
    /// UI-callable method to select Attack Speed upgrade. Returns true if the pending level-up was consumed.
    /// </summary>
    public bool SelectAttackSpeedUpgrade()
    {
        return SelectUpgrade(UpgradeType.AttackSpeed);
    }

    /// <summary>
    /// UI-callable method to select Projectile Damage upgrade. Returns true if the pending level-up was consumed.
    /// </summary>
    public bool SelectProjectileDamageUpgrade()
    {
        return SelectUpgrade(UpgradeType.ProjectileDamage);
    }

    /// <summary>
    /// Complete one pending level-up and resume time if no more pending and player is alive.
    /// </summary>
    public void CompleteOneLevelUp()
    {
        if (pendingLevelUps <= 0) return;

        pendingLevelUps--;
        OnPendingLevelUpsChanged?.Invoke(pendingLevelUps);

        Debug.Log($"Completed one level-up. Remaining pending: {pendingLevelUps}");

        // When no pending level-ups remain, remove the LevelUp pause reason.
        // PauseManager will keep the game paused if other reasons (e.g., Death) are active.
        if (pendingLevelUps == 0)
        {
            PauseManager.Instance.Unpause(PauseManager.PauseReason.LevelUp);
        }
    }

    [ContextMenu("Complete One Pending Level-Up (Editor)")]
    private void ContextCompleteOneLevelUp()
    {
        CompleteOneLevelUp();
    }

}
