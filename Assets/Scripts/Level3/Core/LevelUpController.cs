/*
LevelUpController.cs

Purpose:
- Controls the level up controller flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
