/*
BossFeedback.cs

Purpose:
- Controls the boss feedback flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
