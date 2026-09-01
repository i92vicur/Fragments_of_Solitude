/*
PlatformerAudioHub.cs

Purpose: Provide one persistent audio entry point for platformer jump, landing, dash, death and respawn cues.

Design decisions:
- A singleton survives scene reloads with DontDestroyOnLoad; duplicate instances destroy themselves.
- Clips are played as one-shots through an automatically created AudioSource, and missing clips are safe no-ops.
- Death marks a pending respawn cue, which plays only after PlatformerLevel3 finishes loading so the sound aligns
    with the restarted player rather than the death frame.
*/

using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlatformerAudioHub : MonoBehaviour
{
    public static PlatformerAudioHub Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip respawnClip;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource audioSource;
    private bool awaitingRespawn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void PlayJump() => Play(jumpClip);
    public void PlayLand() => Play(landClip);
    public void PlayDash() => Play(dashClip);

    public void PlayDeath()
    {
        awaitingRespawn = true;
        Play(deathClip);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!awaitingRespawn || scene.name != "PlatformerLevel3")
            return;

        awaitingRespawn = false;
        Play(respawnClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
