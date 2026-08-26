/*
EnemySpawner.cs

Design decisions and functionality:
- Purpose: Spawn enemies around the player at runtime for arena playtests.
- Spawn strategy: Spawns one enemy per interval at a random angle and random distance between minDistance and maxDistance from the assigned Player Transform.
- Configuration: enemyPrefab, playerTransform, initialDelay, spawnInterval, minDistance, maxDistance, and maxLiving are configurable in the Inspector with OnValidate ensuring sane defaults.
- Living tracking: Tracks spawned enemies explicitly by subscribing to EnemyDeath.OnDeath on each instantiated enemy. This avoids scene-wide searches and reliably decrements the living count when a tracked enemy dies.
- Isolation: Does not count manually placed scene enemies because it only increments livingCount for instantiated enemies.
- Safety:
  - If required references are missing, the spawner disables itself with a clear error.
  - Unsubscribes from tracked enemies on spawner destruction to avoid callbacks to a destroyed spawner.
- Simplicity: No pooling, no waves, no pathfinding. Suitable for open arena tests.
- Gizmos: Optionally draws min/max spawn radius around the Player to visualize spawn band in Scene view.

Notes:
- Ensure the Enemy prefab contains EnemyDeath and EnemyMovement so it will be tracked and will move toward the assigned Player Transform automatically.
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyEntry
    {
        public string label;
        public GameObject prefab;
        [Tooltip("Relative spawn weight; higher = more likely. Use positive integers.")]
        public int weight = 1;
    }

    [SerializeField, Tooltip("Enemy types that may be spawned. Configure at least one entry.")]
    private EnemyEntry[] enemyTypes = new EnemyEntry[0];

    [SerializeField, Tooltip("Player transform to target. Assign the Player GameObject's transform.")]
    private Transform playerTransform;

    [SerializeField, Tooltip("Initial delay in seconds before the first spawn.")]
    private float initialDelay = 2f;

    [SerializeField, Tooltip("Time in seconds between spawns. This is the starting interval; the runtime interval may change if progression is enabled.")]
    private float spawnInterval = 3f;

    [SerializeField, Tooltip("Minimum allowed spawn interval when progression reaches its target.")]
    private float minSpawnInterval = 0.5f;

    [SerializeField, Tooltip("Minimum spawn distance from the player.")]
    private float minDistance = 5f;

    [SerializeField, Tooltip("Maximum spawn distance from the player.")]
    private float maxDistance = 10f;

    [SerializeField, Tooltip("Maximum number of living spawned enemies (starting value). The runtime maximum may increase if progression is enabled.")]
    private int maxLiving = 5;

    [SerializeField, Tooltip("Optional: enable simple time-based progression that makes spawns more demanding over time.")]
    private bool enableProgression = true;

    [SerializeField, Tooltip("How many seconds it takes to reach the progression target values. Clamped >= 1.0.")]
    private float progressionDuration = 180f;

    [SerializeField, Tooltip("Target maximum living enemies at the end of progression (runtime value will lerp from maxLiving to this).")]
    private int targetMaxLiving = 10;

    [SerializeField, Tooltip("Draw spawn radius gizmos in the Scene view.")]
    private bool drawGizmos = true;

    // Spawn validation / arena bounds
    [Header("Spawn Validation")]
    [SerializeField, Tooltip("Maximum number of attempts to find a valid spawn position per spawn interval.")]
    private int maxSpawnAttempts = 12;

    [SerializeField, Tooltip("Obstacle layer mask used to validate spawn overlap (usually 'Environment').")]
    private LayerMask obstacleLayerMask = 0;

    [SerializeField, Tooltip("Whether to attempt to auto-detect arena bounds from a GameObject named 'Arena_Bounds' with child wall colliders.")]
    private bool autoDetectArenaBounds = true;

    [SerializeField, Tooltip("Fallback arena minimum (x,y) if auto-detect fails.")]
    private Vector2 arenaMin = new Vector2(-19f, -13f);

    [SerializeField, Tooltip("Fallback arena maximum (x,y) if auto-detect fails.")]
    private Vector2 arenaMax = new Vector2(19f, 13f);

    // Optional boss configuration
    [Header("Boss (optional)")]
    [SerializeField, Tooltip("Boss prefab to spawn when player reaches threshold (optional).")]
    private GameObject bossPrefab;
    [SerializeField, Tooltip("Exact scene Transform used for boss spawning. The boss is instantiated at this position and rotation with no random search.")]
    private Transform bossSpawnPoint;
    [SerializeField, Tooltip("Player level at or above which the boss spawns.")]
    private int bossLevelThreshold = 10;
    [SerializeField, Tooltip("Temporary Phase 1 gate: when enabled, reaching level 6 bypasses the boss trigger so the staircase transition can take over. This remains reversible for later boss restoration.")]
    private bool phase1Level6BossBypass = true;
    [SerializeField, Tooltip("If true, regular spawning pauses while the boss is alive.")]
    private bool pauseSpawningDuringBoss = true;

    // Runtime tracking values (derived from serialized fields and progression)
    private int livingCount = 0;
    private HashSet<EnemyDeath> tracked = new HashSet<EnemyDeath>();
    private bool running = false;
    private float nextSpawnTime = 0f;

    private float spawnStartTime = 0f;
    private float currentSpawnInterval = 0f;
    private int currentMaxLiving = 0;
    private float baseSpawnInterval = 0f;
    private int baseMaxLiving = 0;

    // Boss runtime state
    private bool bossEncounterActive = false;
    private bool bossSpawned = false; // ensures single spawn per session
    private bool pendingBossSpawn = false; // if level-up occurred while paused for level-up, delay spawn
    private EnemyDeath activeBossDeathRef = null;
    private PlayerExperience cachedPlayerExperience = null;

    /// <summary>
    /// True only while the current run is in an active boss encounter window
    /// (after boss spawn and before boss defeat).
    /// </summary>
    public bool IsBossEncounterActive => bossEncounterActive;

    void OnValidate()
    {
        if (minDistance < 0f) minDistance = 0f;
        if (maxDistance < minDistance) maxDistance = minDistance + 1f;
        if (spawnInterval <= 0f) spawnInterval = 1f;
        if (minSpawnInterval < 0.05f) minSpawnInterval = 0.05f;
        if (minSpawnInterval > spawnInterval) minSpawnInterval = spawnInterval;
        if (initialDelay < 0f) initialDelay = 0f;
        if (progressionDuration < 1f) progressionDuration = 1f;
        if (maxLiving < 1) maxLiving = 1;
        if (targetMaxLiving < maxLiving) targetMaxLiving = maxLiving;
    }

    void Start()
    {
        if ((enemyTypes == null || enemyTypes.Length == 0) || playerTransform == null)
        {
            Debug.LogError("EnemySpawner requires at least one enemy type configured and a playerTransform assigned. Disabling spawner.", this);
            enabled = false;
            return;
        }

        // Cache reference to PlayerExperience if available so we can listen for level-ups for boss triggers
        cachedPlayerExperience = playerTransform.GetComponent<PlayerExperience>();
        if (cachedPlayerExperience != null)
        {
            cachedPlayerExperience.OnLevelUp += OnPlayerLevelUp;
        }

        // Ensure at least one valid prefab is present
        bool hasValid = false;
        foreach (var e in enemyTypes)
        {
            if (e != null && e.prefab != null) { hasValid = true; break; }
        }
        if (!hasValid)
        {
            Debug.LogError("EnemySpawner: No valid enemy prefabs found in enemyTypes. Disabling spawner.", this);
            enabled = false;
            return;
        }

        // Basic validation
        if (minDistance < 0f) minDistance = 0f;
        if (maxDistance < minDistance) maxDistance = minDistance + 1f;
        if (spawnInterval <= 0f) spawnInterval = 1f;

        // If obstacleLayerMask not set, try to default to Environment
        if (obstacleLayerMask == 0)
        {
            int env = LayerMask.NameToLayer("Environment");
            if (env != -1) obstacleLayerMask = 1 << env;
        }

        // Try to auto-detect arena bounds from Arena_Bounds children if requested
        if (autoDetectArenaBounds)
        {
            var arenaGO = GameObject.Find("Arena_Bounds");
            if (arenaGO != null)
            {
                var left = arenaGO.transform.Find("Wall_Left");
                var right = arenaGO.transform.Find("Wall_Right");
                var top = arenaGO.transform.Find("Wall_Top");
                var bottom = arenaGO.transform.Find("Wall_Bottom");
                if (left != null && right != null && top != null && bottom != null)
                {
                    var leftCol = left.GetComponent<BoxCollider2D>();
                    var rightCol = right.GetComponent<BoxCollider2D>();
                    var topCol = top.GetComponent<BoxCollider2D>();
                    var bottomCol = bottom.GetComponent<BoxCollider2D>();
                    if (leftCol != null && rightCol != null && topCol != null && bottomCol != null)
                    {
                        // compute inner playable rect from walls (world sizes)
                        float leftInner = left.position.x + (leftCol.size.x * left.lossyScale.x) * 0.5f;
                        float rightInner = right.position.x - (rightCol.size.x * right.lossyScale.x) * 0.5f;
                        float bottomInner = bottom.position.y + (bottomCol.size.y * bottom.lossyScale.y) * 0.5f;
                        float topInner = top.position.y - (topCol.size.y * top.lossyScale.y) * 0.5f;

                        arenaMin = new Vector2(leftInner, bottomInner);
                        arenaMax = new Vector2(rightInner, topInner);
                    }
                }
            }
        }

        // Initialize progression/runtime values
        baseSpawnInterval = spawnInterval;
        baseMaxLiving = maxLiving;
        currentSpawnInterval = baseSpawnInterval;
        currentMaxLiving = baseMaxLiving;
        spawnStartTime = Time.time;

        running = true;
        nextSpawnTime = Time.time + initialDelay;
    }

    void Update()
    {
        if (!running) return;

// Skip spawning while the player is in a dialogue conversation
if (DialogueGate.IsDialogueActive)
    return;

// If boss encounter is active and we should pause spawning, skip progression and spawning
if (bossEncounterActive && pauseSpawningDuringBoss)
    return;

// Update runtime progression values (simple linear interpolation over progressionDuration)
if (enableProgression)
{
    float elapsed = Time.time - spawnStartTime;
    float t = Mathf.Clamp01(elapsed / progressionDuration);
    currentSpawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
    currentMaxLiving = Mathf.RoundToInt(Mathf.Lerp((float)baseMaxLiving, (float)targetMaxLiving, t));
}
else
{
    currentSpawnInterval = baseSpawnInterval;
    currentMaxLiving = baseMaxLiving;
}

if (livingCount >= currentMaxLiving) return;

if (Time.time >= nextSpawnTime)
{
    TrySpawnOne();
    nextSpawnTime = Time.time + currentSpawnInterval;
}
    }

    private void OnPlayerLevelUp(int newLevel)
    {
// Only consider spawning a boss once per session
if (bossSpawned) return;

// Temporary Phase 1 gate: level 6 activates the staircase flow instead of triggering the phase-1 boss.
if (phase1Level6BossBypass && newLevel >= 6)
{
    Debug.Log("EnemySpawner: Phase 1 level 6 boss bypass enabled; boss spawn suppressed so staircase transition can handle progression.");
    return;
}

if (newLevel < bossLevelThreshold) return;

// If boss prefab is missing, warn and do nothing
if (bossPrefab == null)
{
    Debug.LogWarning($"EnemySpawner: boss level {bossLevelThreshold} reached but bossPrefab is not assigned.");
    bossSpawned = true; // prevent repeated warnings
    return;
}

// If currently paused for LevelUp (player choosing upgrade), defer spawn until LevelUp pause finishes
if (PauseManager.Instance != null && PauseManager.Instance.IsPausedFor(PauseManager.PauseReason.LevelUp))
{
    pendingBossSpawn = true;
    // listen for pause state changes to know when to spawn
    PauseManager.Instance.OnPauseStateChanged += OnPauseStateChanged_ForBoss;
    Debug.Log("EnemySpawner: Boss spawn deferred until LevelUp pause ends.");
    return;
}

// Spawn immediately
SpawnBossNow();
    }

    private void OnPauseStateChanged_ForBoss(System.Collections.Generic.HashSet<PauseManager.PauseReason> reasons)
    {
// If LevelUp pause ended and we have a pending boss spawn, spawn now
if (!reasons.Contains(PauseManager.PauseReason.LevelUp) && pendingBossSpawn)
{
    pendingBossSpawn = false;
    PauseManager.Instance.OnPauseStateChanged -= OnPauseStateChanged_ForBoss;
    SpawnBossNow();
}
    }

    private void SpawnBossNow()
    {
        if (bossSpawned) return; // double-check
        if (bossPrefab == null) return;

        if (playerTransform == null)
        {
            Debug.LogWarning("EnemySpawner: Cannot spawn boss because playerTransform is null.");
            return;
        }

        if (bossSpawnPoint == null)
        {
            Debug.LogError("EnemySpawner: Boss spawn aborted because BossSpawnPoint is not assigned.", this);
            bossSpawned = true; // fail safely and avoid repeated attempts this run
            return;
        }

        Vector3 bossSpawnPos = bossSpawnPoint.position;
        Quaternion bossSpawnRotation = bossSpawnPoint.rotation;

        var go = Instantiate(bossPrefab, bossSpawnPos, bossSpawnRotation);
        if (go == null)
        {
            Debug.LogWarning("EnemySpawner: Failed to instantiate boss prefab.");
            bossSpawned = true;
            return;
        }

        // Mark this instance as a boss and add feedback hooks
        go.AddComponent<BossMarker>();
        go.AddComponent<BossFeedback>();

        // Play boss spawn audio and VFX
        if (AudioHub.Instance != null) AudioHub.Instance.PlayBossSpawn();
        if (VFXHub.Instance != null) VFXHub.Instance.SpawnBossSpawn(bossSpawnPos);

        // Track boss death so we can resume spawning afterwards
        var death = go.GetComponent<EnemyDeath>();
        if (death != null)
        {
            activeBossDeathRef = death;
            death.OnDeath += OnBossDeath;
        }

        bossEncounterActive = true;
        bossSpawned = true;

        Debug.Log($"EnemySpawner: Boss spawned at level {bossLevelThreshold} threshold.");
    }

    private void OnBossDeath(EnemyDeath died)
    {
// clean up
if (died != null)
{
    try { died.OnDeath -= OnBossDeath; } catch { }
}

activeBossDeathRef = null;
bossEncounterActive = false;

// Stop spawning further enemies for this run — we've reached the victory condition.
running = false;

// Signal victory via PauseManager so UI/other systems can respond
if (PauseManager.Instance != null)
{
    PauseManager.Instance.Pause(PauseManager.PauseReason.Victory);
}

Debug.Log("EnemySpawner: Boss defeated — run complete. Spawning stopped.");
    }
    private void TrySpawnOne()
    {
        if (playerTransform == null) return;

        if (livingCount >= currentMaxLiving) return;

        var chosenPrefab = ChooseEnemyPrefab();
        if (chosenPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: No valid enemy prefab chosen; aborting this spawn.", this);
            return;
        }

        // Find a valid spawn position for the chosen prefab
        Vector2 spawnPos;
        bool found = FindValidSpawnPosition(chosenPrefab, out spawnPos, maxSpawnAttempts);
        if (!found)
        {
            // silently skip this spawn interval if no valid position found
            return;
        }

        var go = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        if (go == null) return;

        // Track this spawned enemy
        var death = go.GetComponent<EnemyDeath>();
        if (death != null)
        {
            // subscribe to death event
            death.OnDeath += OnSpawnedEnemyDeath;
            tracked.Add(death);
            livingCount++;
        }

        // If EnemyMovement is present and the enemy's target is null, assign playerTransform
        var movement = go.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            // Use reflection-free call: EnemyMovement has SetTarget(Transform)
            movement.SetTarget(playerTransform);
        }
    }

    private void OnSpawnedEnemyDeath(EnemyDeath died)
    {
        // Defensive: if spawner was destroyed or disabled, ignore
        if (this == null) return;

        if (died == null) return;

        // Unsubscribe and update counts only if we tracked this instance
        if (tracked.Contains(died))
        {
            tracked.Remove(died);
            livingCount = Mathf.Max(0, livingCount - 1);

            // Try to unsubscribe to be tidy (safe even if object is being destroyed)
            try
            {
                died.OnDeath -= OnSpawnedEnemyDeath;
            }
            catch { }
        }
    }

    private GameObject ChooseEnemyPrefab()
    {
        if (enemyTypes == null || enemyTypes.Length == 0) return null;

        int total = 0;
        foreach (var e in enemyTypes)
        {
            if (e == null || e.prefab == null) continue;
            total += Mathf.Max(0, e.weight);
        }

        if (total <= 0)
        {
            // fallback: return first valid prefab
            foreach (var e in enemyTypes)
            {
                if (e != null && e.prefab != null) return e.prefab;
            }
            return null;
        }

        int roll = Random.Range(1, total + 1);
        int cursor = 0;
        foreach (var e in enemyTypes)
        {
            if (e == null || e.prefab == null) continue;
            int w = Mathf.Max(0, e.weight);
            cursor += w;
            if (roll <= cursor) return e.prefab;
        }

        // Shouldn't happen, but fallback
        foreach (var e in enemyTypes)
        {
            if (e != null && e.prefab != null) return e.prefab;
        }
        return null;
    }

    void OnDestroy()
    {
        // Unsubscribe from any tracked enemies to avoid calling destroyed methods later
        foreach (var death in tracked)
        {
            if (death != null)
            {
                try
                {
                    death.OnDeath -= OnSpawnedEnemyDeath;
                }
                catch { }
            }
        }
        tracked.Clear();

        // Unsubscribe from player experience events if we subscribed
        if (cachedPlayerExperience != null)
        {
            try { cachedPlayerExperience.OnLevelUp -= OnPlayerLevelUp; } catch { }
            cachedPlayerExperience = null;
        }

        // Unsubscribe from pause notifications for boss if we registered
        if (PauseManager.Instance != null)
        {
            try { PauseManager.Instance.OnPauseStateChanged -= OnPauseStateChanged_ForBoss; } catch { }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || playerTransform == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.12f);
        Gizmos.DrawWireSphere(playerTransform.position, maxDistance);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.12f);
        Gizmos.DrawWireSphere(playerTransform.position, minDistance);

        // Draw arena rectangle if available
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.08f);
        Vector3 bottomLeft = new Vector3(arenaMin.x, arenaMin.y, 0f);
        Vector3 topRight = new Vector3(arenaMax.x, arenaMax.y, 0f);
        Vector3 size = topRight - bottomLeft;
        Gizmos.DrawCube((bottomLeft + topRight) * 0.5f, size);
    }

    // --- Spawn validation helpers ---
    private bool FindValidSpawnPosition(GameObject prefab, out Vector2 result, int attempts)
    {
        result = Vector2.zero;
        if (prefab == null || playerTransform == null) return false;

        float prefabRadius = GetApproximateRadius(prefab);

        for (int i = 0; i < Mathf.Max(1, attempts); i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistance, maxDistance);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 candidate = (Vector2)playerTransform.position + offset;

            // check inside arena rect
            if (!IsInsideArena(candidate, prefabRadius)) continue;

            // check overlap with environment obstacles
            Collider2D hit = Physics2D.OverlapCircle(candidate, prefabRadius, obstacleLayerMask);
            if (hit != null) continue;

            // additionally avoid spawning exactly on the player
            if (Vector2.Distance(candidate, playerTransform.position) < (minDistance * 0.5f)) continue;

            result = candidate;
            return true;
        }

        return false;
    }

    private bool IsInsideArena(Vector2 pos, float margin)
    {
        return (pos.x - margin >= arenaMin.x && pos.x + margin <= arenaMax.x && pos.y - margin >= arenaMin.y && pos.y + margin <= arenaMax.y);
    }

    private float GetApproximateRadius(GameObject prefab)
    {
        if (prefab == null) return 0.5f;
        Collider2D col = prefab.GetComponent<Collider2D>() ?? prefab.GetComponentInChildren<Collider2D>();
        Vector3 scale = prefab.transform.localScale;
        if (col is BoxCollider2D box)
        {
            Vector2 worldSize = new Vector2(box.size.x * scale.x, box.size.y * scale.y);
            return Mathf.Max(worldSize.x, worldSize.y) * 0.5f;
        }
        else if (col is CircleCollider2D circ)
        {
            return circ.radius * Mathf.Max(scale.x, scale.y);
        }
        else if (col != null)
        {
            // conservative fallback based on bounds
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
        }

        return 0.5f; // default fallback
    }
}
