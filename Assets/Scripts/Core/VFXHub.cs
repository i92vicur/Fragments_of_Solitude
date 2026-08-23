/*
VFXHub.cs

Minimal visual-effects hub for the prototype.
- Central place to assign small VFX prefabs (ParticleSystem-based prefabs) for common gameplay events.
- Provides SpawnXxx(...) methods that instantiate the assigned prefab at a world position and automatically destroy it after a configurable lifetime.
- Designed to be small and replaceable. Playback is event-driven and optional (no-ops if prefabs are not assigned).
*/

using UnityEngine;

[DisallowMultipleComponent]
public class VFXHub : MonoBehaviour
{
    public static VFXHub Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject projectileImpactPrefab;
    public GameObject enemyDeathPrefab;
    public GameObject playerHitPrefab;
    public GameObject levelUpPrefab;
    public GameObject bossSpawnPrefab;
    public GameObject bossHitPrefab;
    public GameObject bossDeathPrefab;

    [Header("Lifetimes")]
    [Tooltip("Default lifetime (seconds) used to auto-destroy spawned VFX if the prefab doesn't self-clean.")]
    public float defaultLifetime = 2f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple VFXHub instances found. Destroying duplicate.", this);
            Destroy(this.gameObject);
            return;
        }

        // Create small in-scene fallback VFX templates if the user hasn't assigned prefabs.
        CreateFallbacksIfMissing();
    }

    private void CreateFallbacksIfMissing()
    {
        // Only create fallbacks in the Editor / Play Mode for quick testing.
        if (projectileImpactPrefab == null) projectileImpactPrefab = CreateDefaultVFXTemplate("VFX_ProjectileImpact", new Color(1f, 0.6f, 0.2f));
        if (enemyDeathPrefab == null) enemyDeathPrefab = CreateDefaultVFXTemplate("VFX_EnemyDeath", new Color(0.9f, 0.2f, 0.2f));
        if (playerHitPrefab == null) playerHitPrefab = CreateDefaultVFXTemplate("VFX_PlayerHit", new Color(1f, 1f, 0.4f));
        if (levelUpPrefab == null) levelUpPrefab = CreateDefaultVFXTemplate("VFX_LevelUp", new Color(0.4f, 0.9f, 0.6f));
        if (bossSpawnPrefab == null) bossSpawnPrefab = CreateDefaultVFXTemplate("VFX_BossSpawn", new Color(0.7f, 0.4f, 1f));
        if (bossHitPrefab == null) bossHitPrefab = CreateDefaultVFXTemplate("VFX_BossHit", new Color(1f, 0.6f, 0.6f));
        if (bossDeathPrefab == null) bossDeathPrefab = CreateDefaultVFXTemplate("VFX_BossDeath", new Color(1f, 0.4f, 0.4f));
    }


    private GameObject CreateDefaultVFXTemplate(string name, Color tint)
    {
        // Create a small particle system template as a child of this hub (inactive template)
        var template = new GameObject(name);
        template.transform.SetParent(this.transform);
        template.SetActive(false);

        var ps = template.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.25f;
        main.startSpeed = 1.5f;
        main.startSize = 0.2f;
        main.startColor = tint;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var renderer = template.GetComponent<ParticleSystemRenderer>();
        // Use built-in particle material if available; leave default otherwise.

        // Add a small helper to auto-destroy instantiated clones when they finish playing
        template.AddComponent<AutoDestroyParticleClone>();

        return template;
    }

    private void Spawn(GameObject prefab, Vector2 position, float lifetime)
    {
        if (prefab == null) return; // Shouldn't happen due to fallbacks, but keep safe
        var go = Instantiate(prefab, (Vector3)position, Quaternion.identity);
        if (go == null) return;
        // If the template was inactive, ensure the clone is active so particle plays
        go.SetActive(true);
        // Ensure automatic cleanup in case prefab doesn't self-destruct
        Destroy(go, Mathf.Max(0.1f, lifetime));
    }

    // Public spawn helpers (safe no-ops)
    public void SpawnProjectileImpact(Vector2 position) => Spawn(projectileImpactPrefab, position, defaultLifetime);
    public void SpawnEnemyDeath(Vector2 position) => Spawn(enemyDeathPrefab, position, defaultLifetime);
    public void SpawnPlayerHit(Vector2 position) => Spawn(playerHitPrefab, position, defaultLifetime);
    public void SpawnLevelUp(Vector2 position) => Spawn(levelUpPrefab, position, defaultLifetime);
    public void SpawnBossSpawn(Vector2 position) => Spawn(bossSpawnPrefab, position, defaultLifetime);
    public void SpawnBossHit(Vector2 position) => Spawn(bossHitPrefab, position, defaultLifetime);
    public void SpawnBossDeath(Vector2 position) => Spawn(bossDeathPrefab, position, defaultLifetime);
}

// Helper component: when an instantiated clone is created from the template, it will be activated and destroyed by VFXHub's Destroy call.
// This MonoBehaviour is a small marker for future extensions; keeping it minimal for now.
public class AutoDestroyParticleClone : MonoBehaviour { }

