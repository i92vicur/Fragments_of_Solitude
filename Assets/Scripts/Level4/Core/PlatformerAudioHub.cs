/*
PlatformerAudioHub.cs

Purpose:
- Controls the platformer audio hub flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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

        if (audioSource.outputAudioMixerGroup == null)
            audioSource.outputAudioMixerGroup = SettingsManager.SfxGroup;

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
