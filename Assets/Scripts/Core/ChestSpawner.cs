/*
ChestSpawner.cs

Purpose: Randomly place a configurable number of Chest prefab instances at valid positions
every time a run begins.

Design decisions:
- Lifecycle: Called from Start() so it runs once when the scene loads (including after a
  scene-reload restart). A full scene reload destroys all objects automatically, so there
  is never a risk of old chests surviving into a new run.
- Instance tracking: Maintains a list of every spawned chest GameObject. If SpawnRunChests()
  is called more than once in the same scene lifetime (e.g. via ContextMenu in Editor), any
  still-living instances from the previous call are destroyed before the new batch is created.
- Position validation order: inside arena → clear of player start → clear of boss spawn →
  no environment obstacle → no other chest nearby. Validity is the priority; randomness
  is secondary.
- Arena bounds: auto-detected from Arena_Bounds/Wall_* colliders (same pattern as
  EnemySpawner). Falls back to inspector-configured arenaMin/arenaMax values.
- Debug gizmos: OnDrawGizmosSelected draws the valid spawn area rect and last-run chest
  positions in the Scene view. No runtime overhead; gizmos are editor-only.
- No pooling: Chests are few (default 5) and instantiated once per run. Pooling is
  unnecessary overhead here.

Usage:
- Attach to any scene object in Main.
- Assign chestPrefab in the Inspector.
- Arena bounds, PlayerStart, and BossSpawnPoint are found automatically if present in scene.
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChestSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField, Tooltip("Reusable chest prefab to instantiate at valid spawn points.")]
    private GameObject chestPrefab;

    [Header("Spawn Count")]
    [SerializeField, Tooltip("How many chests to try to spawn at run start.")]
    private int chestCount = 5;

    [SerializeField, Tooltip("Maximum total random attempts before stopping spawn search.")]
    private int maxTotalAttempts = 200;

    [Header("Playable Area")]
    [SerializeField, Tooltip("Whether to auto-detect playable area from Arena_Bounds wall colliders.")]
    private bool autoDetectArenaBounds = true;

    [SerializeField, Tooltip("Fallback playable area minimum (x, y) when auto-detect is disabled or fails.")]
    private Vector2 arenaMin = new Vector2(-19f, -13f);

    [SerializeField, Tooltip("Fallback playable area maximum (x, y) when auto-detect is disabled or fails.")]
    private Vector2 arenaMax = new Vector2(19f, 13f);

    [Header("Validation")]
    [SerializeField, Tooltip("Obstacle/environment layers used to reject invalid chest positions.")]
    private LayerMask obstacleLayerMask = 0;

    [SerializeField, Tooltip("Extra margin from arena edges to avoid clipping into walls.")]
    private float borderMargin = 0.2f;

    [SerializeField, Tooltip("Minimum distance from player start position.")]
    private float minDistanceFromPlayerStart = 2.5f;

    [SerializeField, Tooltip("Minimum distance from BossSpawnPoint.")]
    private float minDistanceFromBossSpawn = 2.5f;

    [SerializeField, Tooltip("Minimum center-to-center distance between spawned chests.")]
    private float minDistanceBetweenChests = 1.5f;

    [Header("Optional explicit references")]
    [SerializeField, Tooltip("Player start transform. If null, a GameObject named 'PlayerStart' is located automatically.")]
    private Transform playerStartPoint;

    [SerializeField, Tooltip("Boss spawn transform. If null, a GameObject named 'BossSpawnPoint' is located automatically.")]
    private Transform bossSpawnPoint;

    [Header("Debug (runtime read-only)")]
    [SerializeField] private int lastRequestedCount;
    [SerializeField] private int lastSpawnedCount;
    [SerializeField] private int lastRejectedCount;
    [SerializeField] private int lastAttemptsUsed;
    [SerializeField] private bool lastReachedMaxAttempts;

    // Positions accepted this run, used to enforce inter-chest spacing.
    private readonly List<Vector2> spawnedChestPositions = new List<Vector2>();

    // Live instance references so we can destroy them if SpawnRunChests is called again.
    private readonly List<GameObject> spawnedChestInstances = new List<GameObject>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void OnValidate()
    {
        if (chestCount < 0) chestCount = 0;
        if (maxTotalAttempts < 1) maxTotalAttempts = 1;
        if (borderMargin < 0f) borderMargin = 0f;
        if (minDistanceFromPlayerStart < 0f) minDistanceFromPlayerStart = 0f;
        if (minDistanceFromBossSpawn < 0f) minDistanceFromBossSpawn = 0f;
        if (minDistanceBetweenChests < 0f) minDistanceBetweenChests = 0f;

        if (arenaMax.x < arenaMin.x)
        {
            float midX = (arenaMin.x + arenaMax.x) * 0.5f;
            arenaMin.x = midX - 0.5f;
            arenaMax.x = midX + 0.5f;
        }

        if (arenaMax.y < arenaMin.y)
        {
            float midY = (arenaMin.y + arenaMax.y) * 0.5f;
            arenaMin.y = midY - 0.5f;
            arenaMax.y = midY + 0.5f;
        }
    }

    private void Start()
    {
        ResolveDefaults();
        SpawnRunChests();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Destroy any currently tracked chests and spawn a fresh batch.
    /// Safe to call multiple times in the same scene lifetime.
    /// </summary>
    [ContextMenu("Spawn Chests Now (Editor)")]
    public void SpawnRunChests()
    {
        ClearSpawnedChests();
        ResetDebugStats();

        if (chestPrefab == null)
        {
            Debug.LogError("ChestSpawner: chestPrefab is not assigned.", this);
            return;
        }

        lastRequestedCount = chestCount;

        if (chestCount <= 0)
        {
            Debug.Log("ChestSpawner: chest count is 0 — nothing to spawn.", this);
            return;
        }

        float chestRadius = GetApproximateRadius(chestPrefab);
        int attempts = 0;

        while (lastSpawnedCount < chestCount && attempts < maxTotalAttempts)
        {
            attempts++;
            Vector2 candidate = GetRandomCandidateInsideArena(chestRadius + borderMargin);

            if (!IsCandidateValid(candidate, chestRadius))
            {
                lastRejectedCount++;
                continue;
            }

            GameObject instance = Instantiate(chestPrefab, new Vector3(candidate.x, candidate.y, 0f), Quaternion.identity);
            spawnedChestPositions.Add(candidate);
            spawnedChestInstances.Add(instance);
            lastSpawnedCount++;
        }

        lastAttemptsUsed = attempts;
        lastReachedMaxAttempts = lastSpawnedCount < chestCount;

        if (lastReachedMaxAttempts)
        {
            Debug.LogWarning(
                $"ChestSpawner: spawned {lastSpawnedCount}/{chestCount} chests in {attempts} attempts " +
                $"({lastRejectedCount} rejected). Max attempts reached — map may be too crowded.",
                this);
        }
        else
        {
            Debug.Log(
                $"ChestSpawner: spawned {lastSpawnedCount}/{chestCount} chests in {attempts} attempts " +
                $"({lastRejectedCount} rejected).",
                this);
        }
    }

    /// <summary>
    /// Destroy all chest instances tracked by this spawner and clear position records.
    /// </summary>
    public void ClearSpawnedChests()
    {
        for (int i = spawnedChestInstances.Count - 1; i >= 0; i--)
        {
            if (spawnedChestInstances[i] != null)
                Destroy(spawnedChestInstances[i]);
        }

        spawnedChestInstances.Clear();
        spawnedChestPositions.Clear();
    }

    // -------------------------------------------------------------------------
    // Initialisation helpers
    // -------------------------------------------------------------------------

    private void ResolveDefaults()
    {
        if (obstacleLayerMask == 0)
        {
            int env = LayerMask.NameToLayer("Environment");
            if (env != -1) obstacleLayerMask = 1 << env;
        }

        if (autoDetectArenaBounds)
            TryAutoDetectArenaBounds();

        if (playerStartPoint == null)
        {
            GameObject go = GameObject.Find("PlayerStart");
            if (go != null) playerStartPoint = go.transform;
        }

        if (bossSpawnPoint == null)
        {
            GameObject go = GameObject.Find("BossSpawnPoint");
            if (go != null) bossSpawnPoint = go.transform;
        }
    }

    private void TryAutoDetectArenaBounds()
    {
        GameObject arenaGO = GameObject.Find("Arena_Bounds");
        if (arenaGO == null) return;

        Transform left   = arenaGO.transform.Find("Wall_Left");
        Transform right  = arenaGO.transform.Find("Wall_Right");
        Transform top    = arenaGO.transform.Find("Wall_Top");
        Transform bottom = arenaGO.transform.Find("Wall_Bottom");

        if (left == null || right == null || top == null || bottom == null) return;

        BoxCollider2D leftCol   = left.GetComponent<BoxCollider2D>();
        BoxCollider2D rightCol  = right.GetComponent<BoxCollider2D>();
        BoxCollider2D topCol    = top.GetComponent<BoxCollider2D>();
        BoxCollider2D bottomCol = bottom.GetComponent<BoxCollider2D>();

        if (leftCol == null || rightCol == null || topCol == null || bottomCol == null) return;

        float leftInner   = left.position.x   + (leftCol.size.x   * left.lossyScale.x)   * 0.5f;
        float rightInner  = right.position.x  - (rightCol.size.x  * right.lossyScale.x)  * 0.5f;
        float bottomInner = bottom.position.y  + (bottomCol.size.y * bottom.lossyScale.y) * 0.5f;
        float topInner    = top.position.y    - (topCol.size.y    * top.lossyScale.y)    * 0.5f;

        arenaMin = new Vector2(leftInner, bottomInner);
        arenaMax = new Vector2(rightInner, topInner);
    }

    // -------------------------------------------------------------------------
    // Spawn logic
    // -------------------------------------------------------------------------

    private Vector2 GetRandomCandidateInsideArena(float margin)
    {
        float minX = arenaMin.x + margin;
        float maxX = arenaMax.x - margin;
        float minY = arenaMin.y + margin;
        float maxY = arenaMax.y - margin;

        // Clamp in case margin makes the range inverted (very small map).
        if (maxX < minX) { float mid = (arenaMin.x + arenaMax.x) * 0.5f; minX = mid; maxX = mid; }
        if (maxY < minY) { float mid = (arenaMin.y + arenaMax.y) * 0.5f; minY = mid; maxY = mid; }

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    private bool IsCandidateValid(Vector2 candidate, float chestRadius)
    {
        // 1. Must be inside arena rect with border margin.
        if (!IsInsideArena(candidate, chestRadius + borderMargin))
            return false;

        // 2. Must be far enough from player start.
        if (playerStartPoint != null &&
            Vector2.Distance(candidate, playerStartPoint.position) < minDistanceFromPlayerStart)
            return false;

        // 3. Must be far enough from boss spawn.
        if (bossSpawnPoint != null &&
            Vector2.Distance(candidate, bossSpawnPoint.position) < minDistanceFromBossSpawn)
            return false;

        // 4. Must not overlap environment obstacles.
        float testRadius = Mathf.Max(chestRadius, 0.1f);
        if (Physics2D.OverlapCircle(candidate, testRadius, obstacleLayerMask) != null)
            return false;

        // 5. Must be far enough from every already-accepted chest.
        float spacing = Mathf.Max(minDistanceBetweenChests, chestRadius * 2f);
        for (int i = 0; i < spawnedChestPositions.Count; i++)
        {
            if (Vector2.Distance(candidate, spawnedChestPositions[i]) < spacing)
                return false;
        }

        return true;
    }

    private bool IsInsideArena(Vector2 position, float margin)
    {
        return position.x - margin >= arenaMin.x
            && position.x + margin <= arenaMax.x
            && position.y - margin >= arenaMin.y
            && position.y + margin <= arenaMax.y;
    }

    private float GetApproximateRadius(GameObject prefab)
    {
        if (prefab == null) return 0.5f;

        Collider2D col = prefab.GetComponent<Collider2D>() ?? prefab.GetComponentInChildren<Collider2D>();
        if (col == null) return 0.5f;

        if (col is CircleCollider2D circle)
            return circle.radius * Mathf.Max(prefab.transform.localScale.x, prefab.transform.localScale.y);

        if (col is BoxCollider2D box)
        {
            Vector3 scale = prefab.transform.localScale;
            float w = box.size.x * Mathf.Abs(scale.x);
            float h = box.size.y * Mathf.Abs(scale.y);
            return Mathf.Max(w, h) * 0.5f;
        }

        return Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
    }

    private void ResetDebugStats()
    {
        lastRequestedCount = 0;
        lastSpawnedCount   = 0;
        lastRejectedCount  = 0;
        lastAttemptsUsed   = 0;
        lastReachedMaxAttempts = false;
    }

    // -------------------------------------------------------------------------
    // Editor gizmos (editor-only, no runtime cost)
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        // Draw the valid spawn rect in cyan.
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.15f);
        Vector3 centre = new Vector3((arenaMin.x + arenaMax.x) * 0.5f, (arenaMin.y + arenaMax.y) * 0.5f, 0f);
        Vector3 size   = new Vector3(arenaMax.x - arenaMin.x, arenaMax.y - arenaMin.y, 0f);
        Gizmos.DrawCube(centre, size);

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireCube(centre, size);

        // Draw exclusion zones around player start and boss spawn.
        if (playerStartPoint != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            DrawWireCircle(playerStartPoint.position, minDistanceFromPlayerStart, 24);
        }

        if (bossSpawnPoint != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
            DrawWireCircle(bossSpawnPoint.position, minDistanceFromBossSpawn, 24);
        }

        // Draw last-run chest positions as yellow circles.
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        for (int i = 0; i < spawnedChestPositions.Count; i++)
        {
            Vector3 p = new Vector3(spawnedChestPositions[i].x, spawnedChestPositions[i].y, 0f);
            DrawWireCircle(p, 0.35f, 12);
            Gizmos.DrawSphere(p, 0.08f);
        }
    }

    private static void DrawWireCircle(Vector3 centre, float radius, int segments)
    {
        if (segments < 3) segments = 3;
        float step = Mathf.PI * 2f / segments;
        Vector3 prev = centre + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            Vector3 next = centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}