/*
AudioHub.cs

Purpose: Centralized audio playback for prototype events (shooting, hits, deaths, level-ups, boss events).

Design decisions:
- Singleton pattern: Static Instance ensures only one AudioHub exists and can be accessed globally without scene setup.
- Lazy initialization: If no AudioHub exists in the scene, a fallback AudioSource is created on Awake.
- AudioSource management: Assigns or creates a single AudioSource on the GameObject hosting the hub. All PlayXxx() methods use PlayOneShot to layer sounds.
- Clip assignment: Public fields for each event type (playerShootClip, enemyHitClip, enemyDieClip, etc.) are assigned in the Inspector.
- Safe playback: Each PlayXxx() method checks if the clip exists before calling PlayOneShot. Missing clips result in silent no-ops (no error spam).
- Volume control: Master volume slider [0–1] scales all playback.
- Extensibility: New event types simply add a new public AudioClip field and a corresponding PlayXxx() method.
- Simplicity: No ducking, crossfades, or complex mixing—suitable for prototype.

Configuration:
- audioSource: Leave empty to auto-create, or assign an existing AudioSource.
- Clips: Assign audio files for each event in the Inspector.
- volume: Master volume (0–1).

Usage:
- Attach to any persistent GameObject (e.g., AudioManager or Player).
- Systems call AudioHub.Instance.PlayXxx() to trigger sounds (e.g., AudioHub.Instance.PlayPlayerShoot()).
- Sounds play over each other (PlayOneShot), so rapid events layer properly.

Notes:
- No 3D spatial audio (all sounds play at camera/listener position).
- No queuing or priority system (all sounds play immediately).
- For music or looping ambient sounds, consider a separate system later.
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
