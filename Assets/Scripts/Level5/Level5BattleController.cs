/*
Level5BattleController.cs

Purpose:
- Controls the level5 battle controller flow for the turn-based battle loop, combatants, battle UI, and encounter rules.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class Level5BattleController : MonoBehaviour
{
    [SerializeField, Tooltip("Fixed turn order: Warrior, Tank, Mage, Support, Final Enemy.")]
    private Combatant[] turnOrder = new Combatant[0];

    [SerializeField, Tooltip("When enabled, the Final Enemy in Level 0 removes one living ally per turn without normal damage resolution.")]
    private bool enableLevel0BossTurnMode = false;

    private int currentIndex = 0;

    /// <summary>Overall battle state. Once Victory or Defeat, no more attacks or turns are processed.</summary>
    public enum BattleState { InProgress, Victory, Defeat }

    private BattleState state = BattleState.InProgress;

    /// <summary>Raised whenever the acting combatant changes, including the first turn.</summary>
    public event System.Action<Combatant> OnTurnChanged;

    /// <summary>Raised exactly once, when the battle ends, carrying "VICTORY!" or "DEFEAT".</summary>
    public event System.Action<string> OnBattleEnded;

    /// <summary>Raised each time the Final Enemy attacks, carrying a display message such as "Final Enemy attacks Warrior for 15 damage!".</summary>
    public event System.Action<string> OnEnemyAttack;

    public BattleState State => state;

    public Combatant CurrentCombatant =>
        (turnOrder != null && currentIndex >= 0 && currentIndex < turnOrder.Length) ? turnOrder[currentIndex] : null;

    public int CurrentTurnIndex => currentIndex;

    public bool IsLevel0BossTurnModeActive => enableLevel0BossTurnMode;

    /// <summary>True while a living player-side combatant is acting.</summary>
    public bool IsPlayerTurn
    {
        get
        {
            Combatant combatant = CurrentCombatant;
            return combatant != null && !combatant.IsEnemy && combatant.IsAlive;
        }
    }

    /// <summary>The enemy-side combatant in the turn order, or null if none is assigned.</summary>
    public Combatant EnemyCombatant
    {
        get
        {
            if (turnOrder == null) return null;
            foreach (var combatant in turnOrder)
            {
                if (combatant != null && combatant.IsEnemy)
                    return combatant;
            }
            return null;
        }
    }

    private void Start()
    {
        if (turnOrder == null || turnOrder.Length == 0)
        {
            Debug.LogError("[Level5Battle] Turn order is empty. Assign the five combatants in the Inspector.", this);
            enabled = false;
            return;
        }

        currentIndex = 0;
        if (CurrentCombatant == null || !CurrentCombatant.IsAlive)
            currentIndex = FindNextLivingIndex(currentIndex);

        LogCurrentTurn();
    }

    /// <summary>
    /// Deterministic prototype damage: attacker Attack minus target Defense, never below 1.
    /// Returns the damage actually applied (0 on a miss). Does not end the turn. No-ops once the battle
    /// has ended or against a target that is already dead. <paramref name="canMiss"/> gates both the miss
    /// chance and the critical-hit chance, so the enemy's own attack (which calls this with
    /// canMiss: false) is unaffected by either.
    /// </summary>
    public int PerformAttack(Combatant attacker, Combatant target, out bool missed, out bool critical, bool canMiss = true, float damageMultiplier = 1f)
    {
        missed = false;
        critical = false;

        if (state != BattleState.InProgress)
            return 0;

        if (attacker == null || target == null)
        {
            Debug.LogWarning("[Level5Battle] PerformAttack called with a missing attacker or target.", this);
            return 0;
        }

        if (!target.IsAlive)
            return 0;

        // Visual only, before the miss roll so a missed attack still animates.
        attacker.PlayAttackAnimation();

        if (canMiss && RollMiss())
        {
            missed = true;
            return 0;
        }

        int baseDamage = Mathf.Max(1, attacker.Attack - target.EffectiveDefense);
        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier));
        target.ClearDefendBonus();

        if (canMiss && RollCritical())
        {
            critical = true;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * CriticalMultiplier));
        }

        return ApplyDamage(target, damage);
    }

    /// <summary>
    /// Fixed-damage special attack (currently the Mage's MAGIC action): deals <see cref="MagicDamage"/>
    /// straight to the target, ignoring Defense entirely. Returns the damage actually applied (0 on a
    /// miss). No-ops once the battle has ended or against a target that is already dead.
    /// </summary>
    public int PerformMagicAttack(Combatant caster, Combatant target, out bool missed, out bool critical)
    {
        missed = false;
        critical = false;

        if (state != BattleState.InProgress)
            return 0;

        if (caster == null || target == null)
        {
            Debug.LogWarning("[Level5Battle] PerformMagicAttack called with a missing caster or target.", this);
            return 0;
        }

        if (!target.IsAlive)
            return 0;

        if (RollMiss())
        {
            missed = true;
            return 0;
        }

        int damage = MagicDamage;
        if (RollCritical())
        {
            critical = true;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * CriticalMultiplier));
        }

        return ApplyDamage(target, damage);
    }

    /// <summary>Fixed damage dealt by MAGIC, ignoring Defense.</summary>
    public const int MagicDamage = 45;

    /// <summary>Chance, per player offensive action (ATTACK/MAGIC), that it misses and deals no damage.</summary>
    private const float MissChance = 0.10f;

    /// <summary>Chance, among hits, that a player offensive action becomes a critical hit.</summary>
    private const float CriticalChance = 0.20f;

    /// <summary>Damage multiplier applied on a critical hit.</summary>
    private const float CriticalMultiplier = 1.5f;

    private bool RollMiss() => Random.value < MissChance;

    private bool RollCritical() => Random.value < CriticalChance;

    /// <summary>
    /// Applies damage to a target and runs the shared victory/defeat checks. Used by every damage source
    /// so no action can bypass the existing battle-end logic.
    /// </summary>
    private int ApplyDamage(Combatant target, int damage)
    {
        int applied = target.TakeDamage(damage);

        if (target.IsEnemy && !target.IsAlive)
            EndBattle(target);
        else if (!target.IsEnemy && !target.IsAlive && AllPlayersDead())
            EndDefeat();

        return applied;
    }

    /// <summary>
    /// Resolves the Final Enemy's turn: one attack against a randomly-chosen living player, then a
    /// <see cref="SecondAttackChance"/> chance of a second attack (also randomly targeted), then ends the
    /// enemy's turn. Does nothing if it is not currently the (living) enemy's turn, or if the battle has
    /// already ended. No AI, damage-based targeting or special attacks: prototype scope only.
    /// </summary>
    public void TriggerEnemyAttack()
    {
        if (state != BattleState.InProgress)
            return;

        Combatant enemy = CurrentCombatant;
        if (enemy == null || !enemy.IsEnemy || !enemy.IsAlive)
            return;

        if (enableLevel0BossTurnMode)
        {
            ExecuteLevel0BossTurn();
            return;
        }

        ResolveEnemyHit(enemy);

        // At most one extra attack, and only if the first one didn't already end the battle.
        if (state == BattleState.InProgress)
        {
            float enemyHpPercent = enemy.MaxHP > 0 ? ((float)enemy.CurrentHP / enemy.MaxHP) * 100f : 0f;
            float secondAttackChance = SecondAttackChance;

            if (enemyHpPercent <= 70f && enemyHpPercent >= 30f)
                secondAttackChance = 0.65f;

            if (Random.value < secondAttackChance)
                ResolveEnemyHit(enemy);
        }

        EndCurrentTurn();
    }

    /// <summary>Chance, per enemy turn, of a second attack after the first.</summary>
    private const float SecondAttackChance = 0.5f;

    /// <summary>
    /// Resolves a single enemy attack against a randomly-chosen living player and reports it via
    /// OnEnemyAttack. No-ops if the battle has already ended or no living player remains.
    /// </summary>
    private void ResolveEnemyHit(Combatant enemy)
    {
        if (state != BattleState.InProgress)
            return;

        Combatant target = FindRandomLivingPlayer();
        if (target == null)
        {
            Debug.LogWarning("[Level5Battle] Final Enemy has no living player target.", this);
            return;
        }

        float enemyHpPercent = enemy.MaxHP > 0 ? ((float)enemy.CurrentHP / enemy.MaxHP) * 100f : 0f;
        float damageMultiplier = enemyHpPercent < 50f ? 1.2f : 1f;

        int damage = PerformAttack(enemy, target, out _, out _, canMiss: false, damageMultiplier: damageMultiplier);
        string message = $"{enemy.CombatantName} attacks {target.CombatantName} for {damage} damage!";
        if (!target.IsAlive)
            message += $" {target.CombatantName} has been defeated!";

        Debug.Log($"[Level5Battle] {message}", enemy);
        OnEnemyAttack?.Invoke(message);
    }

    /// <summary>
    /// Level 0 boss-turn path: eliminate the first living ally in the fixed turn order without damage
    /// calculation, random targeting, critical hits, misses, or a second attack.
    /// </summary>
    private void ExecuteLevel0BossTurn()
    {
        if (state != BattleState.InProgress)
            return;

        Combatant enemy = CurrentCombatant;
        if (enemy == null || !enemy.IsEnemy || !enemy.IsAlive)
            return;

        Combatant target = FindFirstLivingPlayer();
        if (target == null)
        {
            Debug.LogWarning("[Level5Battle] Level 0 boss has no living player target.", this);
            return;
        }

        int applied = target.TakeDamage(target.CurrentHP);
        Debug.Log($"[Level5Battle] Level 0 boss eliminates {target.CombatantName} for {applied} fatal damage.", enemy);

        if (!target.IsAlive && !target.IsEnemy && AllPlayersDead())
        {
            EndDefeat();
            return;
        }

        if (state == BattleState.InProgress)
            EndCurrentTurn();
    }

    /// <summary>
    /// Returns the first living, non-enemy combatant in the fixed turn order, or null if none remain.
    /// </summary>
    private Combatant FindFirstLivingPlayer()
    {
        if (turnOrder == null) return null;

        foreach (var combatant in turnOrder)
        {
            if (combatant != null && !combatant.IsEnemy && combatant.IsAlive)
                return combatant;
        }

        return null;
    }

    /// <summary>
    /// Returns a random living, non-enemy combatant from the fixed turn order, or null if none remain.
    /// Every living player is equally likely; there is no preference by position, HP or Defense.
    /// </summary>
    private Combatant FindRandomLivingPlayer()
    {
        if (turnOrder == null) return null;

        var livingPlayers = new System.Collections.Generic.List<Combatant>();
        foreach (var combatant in turnOrder)
        {
            if (combatant != null && !combatant.IsEnemy && combatant.IsAlive)
                livingPlayers.Add(combatant);
        }

        if (livingPlayers.Count == 0)
            return null;

        if (livingPlayers.Count == 1)
            return livingPlayers[0];

        var weightedTargets = new System.Collections.Generic.List<Combatant>();
        foreach (var player in livingPlayers)
        {
            float hpPercent = player.MaxHP > 0 ? ((float)player.CurrentHP / player.MaxHP) * 100f : 0f;
            int weight = hpPercent < 50f ? 2 : 1;

            for (int i = 0; i < weight; i++)
                weightedTargets.Add(player);
        }

        return weightedTargets[Random.Range(0, weightedTargets.Count)];
    }

    /// <summary>
    /// Advances to the next living combatant in the fixed order and logs whose turn it is. Does nothing
    /// once the battle has ended.
    /// </summary>
    public void EndCurrentTurn()
    {
        if (state != BattleState.InProgress)
            return;

        int next = FindNextLivingIndex(currentIndex);
        if (next < 0)
        {
            Debug.Log("[Level5Battle] No living combatants remain.", this);
            return;
        }

        currentIndex = next;
        LogCurrentTurn();
    }

    /// <summary>
    /// Stops the turn cycle and raises OnBattleEnded with "VICTORY!". Does not destroy the enemy, show a
    /// victory animation/screen or transition scenes: those are out of scope for this prototype step.
    /// </summary>
    private void EndBattle(Combatant defeatedEnemy)
    {
        state = BattleState.Victory;

        GameProgressManager.Instance.MarkLevel5Completed();

        Debug.Log($"[Level5Battle] {defeatedEnemy.CombatantName} defeated. VICTORY!", defeatedEnemy);
        OnBattleEnded?.Invoke("VICTORY!");
    }

    /// <summary>
    /// Stops the turn cycle and raises OnBattleEnded with "DEFEAT". Does not destroy any characters, show
    /// a game over screen, restart or transition scenes: those are out of scope for this prototype step.
    /// </summary>
    private void EndDefeat()
    {
        state = BattleState.Defeat;

        Debug.Log("[Level5Battle] All player characters defeated. DEFEAT.", this);
        Debug.Log("Level5BattleController firing OnBattleEnded with DEFEAT");
        OnBattleEnded?.Invoke("DEFEAT");
    }

    /// <summary>Returns true once every non-enemy combatant in the turn order is dead.</summary>
    private bool AllPlayersDead()
    {
        if (turnOrder == null) return false;

        foreach (var combatant in turnOrder)
        {
            if (combatant != null && !combatant.IsEnemy && combatant.IsAlive)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the index of the next living combatant after <paramref name="fromIndex"/>, or -1 if none.
    /// </summary>
    private int FindNextLivingIndex(int fromIndex)
    {
        for (int step = 1; step <= turnOrder.Length; step++)
        {
            int candidate = (fromIndex + step) % turnOrder.Length;
            Combatant combatant = turnOrder[candidate];
            if (combatant != null && combatant.IsAlive)
                return candidate;
        }

        return -1;
    }

    private void LogCurrentTurn()
    {
        Combatant combatant = CurrentCombatant;
        if (combatant == null)
        {
            Debug.LogWarning($"[Level5Battle] Turn order slot {currentIndex} is empty.", this);
            return;
        }

        Debug.Log($"[Level5Battle] Turn: {combatant.CombatantName}", combatant);
        OnTurnChanged?.Invoke(combatant);

        // Auto-run the Final Enemy's turn: no button or key input drives it, unlike the player actions.
        if (state == BattleState.InProgress && combatant.IsEnemy && combatant.IsAlive)
            TriggerEnemyAttack();
    }
}
