/*
SettingsManager.cs

Purpose:
- Controls the settings manager flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
    public const string WindowWidthKey = "Settings.WindowWidth";
    public const string WindowHeightKey = "Settings.WindowHeight";

    public const float DefaultMusicVolume = 1f;
    public const float DefaultSfxVolume = 1f;
    public const bool DefaultFullscreen = true;
    public static readonly Vector2Int DefaultWindowSize = new Vector2Int(1920, 1080);

    /// <summary>Explicit list of windowed sizes the player can choose (all 16:9).</summary>
    public static readonly Vector2Int[] AllowedWindowSizes =
    {
        new Vector2Int(960, 540),
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1920, 1080)
    };

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
    private Vector2Int windowSize = DefaultWindowSize;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool Fullscreen => fullscreen;
    public Vector2Int WindowSize => windowSize;

    /// <summary>Starts the manager before the first scene loads so saved display settings apply from the start.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeFirstScene()
    {
        _ = Instance;
    }

    /// <summary>True when the width and height form one of the allowed window sizes.</summary>
    public static bool IsAllowedWindowSize(int width, int height)
    {
        foreach (Vector2Int size in AllowedWindowSizes)
        {
            if (size.x == width && size.y == height) return true;
        }
        return false;
    }

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

    /// <summary>Validates and persists a windowed size, applying it immediately when not in fullscreen. Returns false if the size is not allowed.</summary>
    public bool SetWindowSize(int width, int height)
    {
        if (!IsAllowedWindowSize(width, height)) return false;

        var size = new Vector2Int(width, height);
        if (windowSize == size) return true;

        windowSize = size;
        PlayerPrefs.SetInt(WindowWidthKey, windowSize.x);
        PlayerPrefs.SetInt(WindowHeightKey, windowSize.y);
        PlayerPrefs.Save();
        if (!fullscreen) ApplyWindowSize();
        OnSettingsChanged?.Invoke();
        return true;
    }

    /// <summary>Restores the default values and persists them.</summary>
    public void ResetToDefaults()
    {
        musicVolume = DefaultMusicVolume;
        sfxVolume = DefaultSfxVolume;
        fullscreen = DefaultFullscreen;
        windowSize = DefaultWindowSize;
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
        if (fullscreen)
        {
            if (!Screen.fullScreen) Screen.fullScreen = true;
            return;
        }

        ApplyWindowSize();
    }

    /// <summary>Sets the stored window size in windowed mode. Skips the call when the window already matches.</summary>
    private void ApplyWindowSize()
    {
        if (!Screen.fullScreen && Screen.width == windowSize.x && Screen.height == windowSize.y) return;

        Screen.SetResolution(windowSize.x, windowSize.y, FullScreenMode.Windowed);
    }

    private void Load()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, DefaultFullscreen ? 1 : 0) != 0;
        windowSize = LoadWindowSize();
    }

    /// <summary>Returns the saved window size, or the default when the saved pair is missing or not allowed.</summary>
    private static Vector2Int LoadWindowSize()
    {
        if (!PlayerPrefs.HasKey(WindowWidthKey) || !PlayerPrefs.HasKey(WindowHeightKey)) return DefaultWindowSize;

        int width = PlayerPrefs.GetInt(WindowWidthKey);
        int height = PlayerPrefs.GetInt(WindowHeightKey);
        return IsAllowedWindowSize(width, height) ? new Vector2Int(width, height) : DefaultWindowSize;
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(WindowWidthKey, windowSize.x);
        PlayerPrefs.SetInt(WindowHeightKey, windowSize.y);
        PlayerPrefs.Save();
    }
}
