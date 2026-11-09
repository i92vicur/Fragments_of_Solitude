using UnityEngine;

[DisallowMultipleComponent]
public class LevelMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Touching the instance makes sure the saved music volume is applied to the mixer.
        _ = SettingsManager.Instance;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.clip = musicClip;
        audioSource.volume = volume;

        if (audioSource.outputAudioMixerGroup == null)
            audioSource.outputAudioMixerGroup = SettingsManager.MusicGroup;

        if (musicClip != null)
            audioSource.Play();
    }
}
