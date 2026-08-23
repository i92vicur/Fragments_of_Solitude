/*
PlayerBossMegaAttack.cs

Purpose: Player-only special attack that spends 3 Boss Attack Pieces during an active boss fight
and emits a multi-layer radial projectile burst from the player's current position.

Design decisions:
- Boss restriction: Queries EnemySpawner.IsBossEncounterActive rather than checking for a boss GameObject.
- One key press = one activation: Uses Input.GetKeyDown so holding U does not repeat.
- Piece spending: Consumes exactly 3 pieces at activation time via PlayerBossAttackPieces.
  HUD updates immediately through the existing OnPiecesChanged event subscription.
- Radial pattern: Each layer distributes projectiles evenly across 360 degrees. Later layers
  receive an angular offset so the rings do not align at the exact same angles.
- Reuse: Uses the existing PlayerProjectile prefab and PlayerProjectile component. No new
  projectile prefab and no pooling system are introduced.
- Snapshot origin: Captures the player's current position once at activation time so the full
  burst is centered on where the mega attack was triggered.
*/

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBossMegaAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField, Tooltip("Key used to activate the boss mega attack.")]
    private KeyCode activationKey = KeyCode.U;

    [Header("Radial Burst")]
    [SerializeField, Tooltip("Number of radial layers to emit.")]
    private int radialLayerCount = 3;

    [SerializeField, Tooltip("Projectile counts for each layer. Array length is kept in sync with radialLayerCount.")]
    private int[] projectilesPerLayer = new int[] { 8, 12, 16 };

    [SerializeField, Tooltip("Projectile speed override used for the mega attack burst.")]
    private float projectileSpeed = 14f;

    [SerializeField, Tooltip("Delay in seconds between radial layers.")]
    private float delayBetweenLayers = 0.08f;

    [SerializeField, Tooltip("Angular offset in degrees added per successive layer.")]
    private float angularOffsetBetweenLayers = 7.5f;

    [SerializeField, Tooltip("Projectile lifetime override used for the mega attack burst.")]
    private float projectileLifetime = 2.5f;

    private PlayerBossAttackPieces bossAttackPieces;
    private PlayerAutoAttack autoAttack;
    private EnemySpawner enemySpawner;
    private Coroutine activeBurstCoroutine;

    private void OnValidate()
    {
        if (activationKey == KeyCode.None) activationKey = KeyCode.U;
        if (radialLayerCount < 1) radialLayerCount = 1;
        if (projectileSpeed <= 0f) projectileSpeed = 0.1f;
        if (delayBetweenLayers < 0f) delayBetweenLayers = 0f;
        if (projectileLifetime <= 0f) projectileLifetime = 0.1f;

        if (projectilesPerLayer == null)
            projectilesPerLayer = new int[radialLayerCount];

        if (projectilesPerLayer.Length != radialLayerCount)
        {
            int[] resized = new int[radialLayerCount];
            for (int i = 0; i < radialLayerCount; i++)
            {
                if (projectilesPerLayer != null && i < projectilesPerLayer.Length)
                    resized[i] = projectilesPerLayer[i];
                else if (i > 0)
                    resized[i] = resized[i - 1] + 4;
                else
                    resized[i] = 8;
            }
            projectilesPerLayer = resized;
        }

        for (int i = 0; i < projectilesPerLayer.Length; i++)
        {
            if (projectilesPerLayer[i] < 1)
                projectilesPerLayer[i] = 1;
        }
    }

    private void Awake()
    {
        bossAttackPieces = GetComponent<PlayerBossAttackPieces>();
        autoAttack = GetComponent<PlayerAutoAttack>();
        enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (bossAttackPieces == null)
            Debug.LogError("PlayerBossMegaAttack: PlayerBossAttackPieces component not found on Player.", this);

        if (autoAttack == null)
            Debug.LogError("PlayerBossMegaAttack: PlayerAutoAttack component not found on Player.", this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(activationKey))
            TryActivateMegaAttack();
    }

    public bool TryActivateMegaAttack()
    {
        if (activeBurstCoroutine != null) return false;
        if (bossAttackPieces == null || autoAttack == null) return false;

        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (enemySpawner == null || !enemySpawner.IsBossEncounterActive)
            return false;

        GameObject projectilePrefab = autoAttack.GetProjectilePrefab();
        if (projectilePrefab == null)
        {
            Debug.LogWarning("PlayerBossMegaAttack: Player projectile prefab is missing, cannot activate mega attack.", this);
            return false;
        }

        bool consumed = bossAttackPieces.TryConsumePieces(PlayerBossAttackPieces.RequiredPiecesForMegaAttack);
        if (!consumed)
            return false;

        Vector2 burstOrigin = transform.position;
        activeBurstCoroutine = StartCoroutine(FireBurstRoutine(burstOrigin, projectilePrefab, autoAttack.GetAdditionalProjectileDamage()));
        return true;
    }

    private IEnumerator FireBurstRoutine(Vector2 burstOrigin, GameObject projectilePrefab, int bonusDamage)
    {
        for (int layerIndex = 0; layerIndex < radialLayerCount; layerIndex++)
        {
            EmitLayer(projectilePrefab, burstOrigin, layerIndex, bonusDamage);

            if (AudioHub.Instance != null)
                AudioHub.Instance.PlayPlayerShoot();

            if (layerIndex < radialLayerCount - 1 && delayBetweenLayers > 0f)
                yield return new WaitForSeconds(delayBetweenLayers);
        }

        activeBurstCoroutine = null;
    }

    private void EmitLayer(GameObject projectilePrefab, Vector2 burstOrigin, int layerIndex, int bonusDamage)
    {
        int count = GetProjectileCountForLayer(layerIndex);
        float stepDegrees = 360f / count;
        float baseOffset = angularOffsetBetweenLayers * layerIndex;

        for (int i = 0; i < count; i++)
        {
            float angleDeg = baseOffset + (stepDegrees * i);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            SpawnBurstProjectile(projectilePrefab, burstOrigin, direction, bonusDamage);
        }
    }

    private void SpawnBurstProjectile(GameObject projectilePrefab, Vector2 burstOrigin, Vector2 direction, int bonusDamage)
    {
        var projectileGO = Instantiate(projectilePrefab, burstOrigin, Quaternion.identity);
        if (projectileGO == null)
        {
            Debug.LogWarning("PlayerBossMegaAttack: Failed to instantiate projectile for mega attack.", this);
            return;
        }

        var projectile = projectileGO.GetComponent<PlayerProjectile>();
        if (projectile == null)
        {
            Debug.LogWarning("PlayerBossMegaAttack: Instantiated projectile prefab is missing PlayerProjectile. Destroying invalid instance.", this);
            Destroy(projectileGO);
            return;
        }

        projectile.Initialize(direction, projectileSpeed, projectileLifetime);

        if (bonusDamage != 0)
            projectile.SetDamage(projectile.GetDamage() + bonusDamage);
    }

    private int GetProjectileCountForLayer(int layerIndex)
    {
        if (projectilesPerLayer == null || projectilesPerLayer.Length == 0)
            return 1;

        if (layerIndex < projectilesPerLayer.Length)
            return Mathf.Max(1, projectilesPerLayer[layerIndex]);

        return Mathf.Max(1, projectilesPerLayer[projectilesPerLayer.Length - 1]);
    }
}