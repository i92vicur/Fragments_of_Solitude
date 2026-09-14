/*
SettingsManager.cs

Purpose: Central storage and management of player preferences (music volume, SFX volume, fullscreen).

Design decisions:
- Singleton pattern: Static Instance provides global access from UI and audio systems.
- Persistent across scenes: DontDestroyOnLoad keeps a single instance alive for the whole session.
- Self-bootstrapping: Instance auto-creates a hidden GameObject if none exists in the scene, so no scene setup is required.
- PlayerPrefs backing store: Values are loaded once on Awake and written immediately whenever a setter is called.
- Defaults on first run: music 1, SFX 1, fullscreen enabled.
- Mixer access: The shared GameAudioMixer lives under Resources so it can be loaded by code without any scene wiring.
- Application: MusicVolume and SfxVolume are applied to the mixer's exposed parameters, Fullscreen to Screen.fullScreen.

Usage:
- Read:  SettingsManager.Instance.MusicVolume
- Write: SettingsManager.Instance.SetMusicVolume(0.5f)
- Route an AudioSource to SFX: source.outputAudioMixerGroup = SettingsManager.SfxGroup;
- Subscribe to OnSettingsChanged to react to any change.
*/

using System;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class SettingsManager : MonoBehaviour
{
    public const string MusicVolumeKey = "Settings.MusicVolume";
    public const string SfxVolumeKey = "Settings.SfxVolume";
    public const string FullscreenKey = "Settings.Fullscreen";

    public const float DefaultMusicVolume = 1f;
    public const float DefaultSfxVolume = 1f;
    public const bool DefaultFullscreen = true;

    public const string MixerResourcePath = "Audio/GameAudioMixer";
    public const string MusicVolumeParameter = "MusicVolume";
    public const string SfxVolumeParameter = "SFXVolume";
    public const string MusicGroupName = "Music";
    public const string SfxGroupName = "SFX";

    /// <summary>Attenuation applied at volume 0; matches the mixer's minimum (fully silent).</summary>
    private const float MinVolumeDecibels = -80f;

    private static SettingsManager instance;
    private static AudioMixer mixer;
    private static AudioMixerGroup musicGroup;
    private static AudioMixerGroup sfxGroup;

    public static SettingsManager Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = FindFirstObjectByType<SettingsManager>();
            if (instance == null)
            {
                var go = new GameObject(nameof(SettingsManager));
                instance = go.AddComponent<SettingsManager>();
            }
            return instance;
        }
    }

    /// <summary>Shared audio mixer, loaded from Resources. Null if the asset is missing.</summary>
    public static AudioMixer Mixer
    {
        get
        {
            if (mixer == null) mixer = Resources.Load<AudioMixer>(MixerResourcePath);
            return mixer;
        }
    }

    /// <summary>Mixer group every music track should be routed through. Null if the mixer is missing.</summary>
    public static AudioMixerGroup MusicGroup
    {
        get
        {
            if (musicGroup != null) return musicGroup;

            var m = Mixer;
            if (m == null) return null;

            var groups = m.FindMatchingGroups(MusicGroupName);
            if (groups != null && groups.Length > 0) musicGroup = groups[0];
            return musicGroup;
        }
    }

    /// <summary>Mixer group every sound effect should be routed through. Null if the mixer is missing.</summary>
    public static AudioMixerGroup SfxGroup
    {
        get
        {
            if (sfxGroup != null) return sfxGroup;

            var m = Mixer;
            if (m == null) return null;

            var groups = m.FindMatchingGroups(SfxGroupName);
            if (groups != null && groups.Length > 0) sfxGroup = groups[0];
            return sfxGroup;
        }
    }

    /// <summary>Raised after any setting changes and has been persisted.</summary>
    public event Action OnSettingsChanged;

    private float musicVolume = DefaultMusicVolume;
    private float sfxVolume = DefaultSfxVolume;
    private bool fullscreen = DefaultFullscreen;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool Fullscreen => fullscreen;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        ApplyMusicVolume();
        ApplySfxVolume();
        ApplyFullscreen();
    }

    // Re-applied in Start because a mixer written to during Awake can be overwritten by its start snapshot.
    private void Start()
    {
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public float GetMusicVolume() => musicVolume;

    public float GetSfxVolume() => sfxVolume;

    public bool GetFullscreen() => fullscreen;

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(musicVolume, value)) return;

        musicVolume = value;
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
        OnSettingsChanged?.Invoke();
    }

    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(sfxVolume, value)) return;

        sfxVolume = value;
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        ApplySfxVolume();
        OnSettingsChanged?.Invoke();
    }

    public void SetFullscreen(bool value)
    {
        if (fullscreen == value) return;

        fullscreen = value;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        ApplyFullscreen();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>Restores the default values and persists them.</summary>
    public void ResetToDefaults()
    {
        musicVolume = DefaultMusicVolume;
        sfxVolume = DefaultSfxVolume;
        fullscreen = DefaultFullscreen;
        Save();
        ApplyMusicVolume();
        ApplySfxVolume();
        ApplyFullscreen();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>Converts a linear 0-1 volume to the decibel scale used by AudioMixer parameters.</summary>
    public static float LinearToDecibels(float linear)
    {
        linear = Mathf.Clamp01(linear);
        return linear <= 0.0001f ? MinVolumeDecibels : Mathf.Log10(linear) * 20f;
    }

    private void ApplySfxVolume()
    {
        var m = Mixer;
        if (m == null) return;

        m.SetFloat(SfxVolumeParameter, LinearToDecibels(sfxVolume));
    }

    private void ApplyMusicVolume()
    {
        var m = Mixer;
        if (m == null) return;

        m.SetFloat(MusicVolumeParameter, LinearToDecibels(musicVolume));
    }

    private void ApplyFullscreen()
    {
        if (Screen.fullScreen != fullscreen) Screen.fullScreen = fullscreen;
    }

    private void Load()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, DefaultFullscreen ? 1 : 0) != 0;
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
