/*
BossFeedback.cs

Purpose: Provide audio and visual feedback when a boss takes damage or dies.

Design decisions:
- Event-driven: Subscribes to boss Health.OnHealthChanged and OnZeroHealth to track state changes without per-frame polling.
- Damage detection: Tracks lastHealth to detect damage (current < lastHealth) and triggers hit feedback.
- Dual audio/VFX: On each hit, plays boss-specific audio (PlayBossHit) and visual effect (SpawnBossHit).
- Death sequence: When boss health reaches zero, plays boss-death audio and VFX plus a victory cue (PlayVictory) to celebrate the win.
- Automatic attachment: EnemySpawner adds this component to boss instances at spawn time.
- Safety: Defensive checks for null AudioHub/VFXHub so missing systems do not crash gameplay.
- Simplicity: No damage scaling, no screen shake, no knockout effects—just audio and particles.

Configuration:
- No public fields; all audio/VFX references managed via AudioHub and VFXHub singletons.

Usage:
- Automatically added to boss instances by EnemySpawner.
- Requires AudioHub and VFXHub to be present in the scene (will create fallbacks if missing).

Notes:
- OnZeroHealth may also trigger EnemyDeath.HandleDeath, which plays additional death effects. Both can coexist (audio/VFX will layer).
- Victory cue is played so the player hears confirmation that the run is complete.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class BossFeedback : MonoBehaviour
{
    private Health health;
    private int lastHealth = -1;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null) return;
        lastHealth = health.CurrentHealth;
        health.OnHealthChanged += OnHealthChanged;
        health.OnZeroHealth += OnZeroHealth;
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
            health.OnZeroHealth -= OnZeroHealth;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current < lastHealth)
        {
            // Boss took damage
            if (AudioHub.Instance != null) AudioHub.Instance.PlayBossHit();
            if (VFXHub.Instance != null) VFXHub.Instance.SpawnBossHit(transform.position);
        }
        lastHealth = current;
    }

    private void OnZeroHealth()
    {
        // Boss died — play boss die and victory
        if (AudioHub.Instance != null) AudioHub.Instance.PlayBossDie();
        if (VFXHub.Instance != null) VFXHub.Instance.SpawnBossDeath(transform.position);
        // Play victory cue as well (spawner also pauses and shows HUD)
        if (AudioHub.Instance != null) AudioHub.Instance.PlayVictory();
    }
}
