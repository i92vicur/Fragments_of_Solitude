/*
AudioHub.cs

Purpose:
- Controls the audio hub flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class AudioHub : MonoBehaviour
{
    public static AudioHub Instance { get; private set; }

    [Header("Audio Source (optional)")]
    [Tooltip("If empty, an AudioSource will be added to this GameObject at runtime.")]
    public AudioSource audioSource;

    [Header("Clips")]
    public AudioClip playerShootClip;
    public AudioClip enemyHitClip;
    public AudioClip enemyDieClip;
    public AudioClip playerHitClip;
    public AudioClip levelUpClip;
    public AudioClip bossSpawnClip;

    [Header("Impact / Misc")]
    public AudioClip impactClip;

    [Header("Boss clips")]
    public AudioClip bossHitClip;
    public AudioClip bossDieClip;
    public AudioClip victoryClip;

    [Header("Playback")]
    [Tooltip("Master volume for hub playback (0-1)")]
    [Range(0f,1f)]
    public float volume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple AudioHub instances found. Destroying duplicate.", this);
            Destroy(this.gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (audioSource.outputAudioMixerGroup == null)
            audioSource.outputAudioMixerGroup = SettingsManager.SfxGroup;
    }

    private void PlayClip(AudioClip clip, float vol = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume * vol));
    }

    // Public methods for gameplay events (safe to call when Instance is null)
    public void PlayPlayerShoot() => PlayClip(playerShootClip);
    public void PlayEnemyHit() => PlayClip(enemyHitClip);
    public void PlayEnemyDie() => PlayClip(enemyDieClip);
    public void PlayPlayerHit() => PlayClip(playerHitClip);
    public void PlayLevelUp() => PlayClip(levelUpClip);
    public void PlayBossSpawn() => PlayClip(bossSpawnClip);

    // Impact
    public void PlayImpact() => PlayClip(impactClip);

    // Boss-specific
    public void PlayBossHit() => PlayClip(bossHitClip);
    public void PlayBossDie() => PlayClip(bossDieClip);
    public void PlayVictory() => PlayClip(victoryClip);
}
