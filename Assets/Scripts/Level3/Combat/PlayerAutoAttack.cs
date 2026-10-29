/*
PlayerAutoAttack.cs

Purpose:
- Controls the player auto attack flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField, Tooltip("Projectile prefab to instantiate (assign in Inspector).")]
    private GameObject projectilePrefab;

    [SerializeField, Tooltip("Optional spawn point for projectiles. If null, player's position will be used.")]
    private Transform projectileSpawnPoint;

    [SerializeField, Tooltip("Attack cooldown in seconds between shots.")]
    private float attackInterval = 0.5f;

    [SerializeField, Tooltip("Attack range in world units.")]
    private float attackRange = 8f;

    [SerializeField, Tooltip("Layer mask used to select candidate targets (enemies).")]
    private LayerMask targetLayerMask = 0;

    [SerializeField, Tooltip("Optional tag to require on targets. Leave empty to ignore tag.")]
    private string requiredTargetTag = "";

    private float cooldownTimer = 0f;
    private int extraProjectileDamage = 0;

    [SerializeField, Tooltip("Minimum allowed attack interval (seconds).")]
    private float minAttackInterval = 0.1f;

    private bool fatalErrorDisabled = false;

    private const float MouseMoveThresholdSqr = 1f;
    private Vector2 lastPadAim = Vector2.right;
    private bool aimingWithPad = false;
    private bool mouseUsedThisSession = false;

    // [DIAG] temporal: registra solo cambios de estado y una muestra cada 0.5 s con el stick inclinado.
    private bool diagStickActive;
    private bool diagAimingWithPad;
    private bool diagMouseUsed;
    private int diagPadCount = -1;
    private float diagNextSampleTime;

    private void DiagLogAim(Vector2 stick)
    {
        bool stickActive = stick != Vector2.zero;
        int pads = Gamepad.all.Count;
        bool changed = stickActive != diagStickActive || aimingWithPad != diagAimingWithPad
            || mouseUsedThisSession != diagMouseUsed || pads != diagPadCount;
        bool sample = stickActive && Time.unscaledTime >= diagNextSampleTime;

        if (changed || sample)
        {
            Debug.Log($"[DIAG Aim] stick={stick} mag={stick.magnitude:F2} pads={pads} aimingWithPad={aimingWithPad} mouseUsed={mouseUsedThisSession} lastPadAim={lastPadAim}", this);
            diagNextSampleTime = Time.unscaledTime + 0.5f;
        }

        diagStickActive = stickActive;
        diagAimingWithPad = aimingWithPad;
        diagMouseUsed = mouseUsedThisSession;
        diagPadCount = pads;
    }

    void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("PlayerAutoAttack: projectilePrefab is not assigned. Disabling auto-attack.", this);
            enabled = false;
            fatalErrorDisabled = true;
            return;
        }

        if (attackInterval <= 0f) attackInterval = 0.1f;
        if (attackRange <= 0f) attackRange = 1f;

    }

    void Update()
    {
        if (fatalErrorDisabled) return;

        // Checked before the cooldown so mouse movement during a cooldown is not missed.
        Mouse mouse = Mouse.current;
        UpdateAimSource(mouse);
        DiagLogAim(GameInput.Aim); // [DIAG] temporal

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        Vector2 spawnPos = (projectileSpawnPoint != null) ? (Vector2)projectileSpawnPoint.position : (Vector2)transform.position;
        Vector2 dir = transform.right;
        if (aimingWithPad)
        {
            dir = lastPadAim;
        }
        else
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mouse != null)
            {
                Vector3 mouseScreenPos = mouse.position.ReadValue();
                mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
                dir = (Vector2)mouseWorldPos - (Vector2)transform.position;
            }
        }
        if (dir.sqrMagnitude <= 0f) dir = transform.right;
        dir.Normalize();

        var projGO = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        if (projGO == null)
        {
            Debug.LogError("PlayerAutoAttack: Failed to instantiate projectile prefab.", this);
            return;
        }

        var projComp = projGO.GetComponent<PlayerProjectile>();
        if (projComp == null)
        {
            Debug.LogError("PlayerAutoAttack: Instantiated projectile prefab does not contain PlayerProjectile component. Disabling auto-attack.", this);
            Destroy(projGO);
            enabled = false;
            fatalErrorDisabled = true;
            return;
        }

        projComp.Initialize(dir);
        if (extraProjectileDamage != 0)
            projComp.SetDamage(projComp.GetDamage() + extraProjectileDamage);

        if (AudioHub.Instance != null)
            AudioHub.Instance.PlayPlayerShoot();

        cooldownTimer = attackInterval;
    }

    // A deflected stick takes priority. Mouse movement or clicks switch back to mouse aim.
    // Until the mouse is used, a connected gamepad is the default aim source, so an idle cursor cannot override it.
    private void UpdateAimSource(Mouse mouse)
    {
        Vector2 stick = GameInput.Aim;
        if (stick != Vector2.zero)
        {
            lastPadAim = stick.normalized;
            aimingWithPad = true;
            return;
        }

        bool mouseActive = mouse != null &&
            (mouse.delta.ReadValue().sqrMagnitude > MouseMoveThresholdSqr ||
             mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame);

        if (mouseActive)
        {
            mouseUsedThisSession = true;
            aimingWithPad = false;
        }
        else if (!mouseUsedThisSession)
        {
            aimingWithPad = Gamepad.all.Count > 0;
        }
    }

    public void IncreaseProjectileDamage(int amount)
    {
        if (amount <= 0) return;
        extraProjectileDamage += amount;
    }

    public void ReduceAttackInterval(float delta)
    {
        if (delta <= 0f) return;
        attackInterval = Mathf.Max(minAttackInterval, attackInterval - delta);
    }

    public GameObject GetProjectilePrefab()
    {
        return projectilePrefab;
    }

    public int GetAdditionalProjectileDamage()
    {
        return extraProjectileDamage;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Vector3 origin = (projectileSpawnPoint != null) ? projectileSpawnPoint.position : transform.position;
        Gizmos.DrawSphere(origin, 0.05f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.12f);
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}