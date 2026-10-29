/*
SettingsPanelController.cs

Purpose:
- Controls the settings panel controller flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Dropdown windowSizeDropdown;

    private void Awake()
    {
        ConfigureSlider(musicVolumeSlider);
        ConfigureSlider(sfxVolumeSlider);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (windowSizeDropdown != null)
        {
            PopulateWindowSizeOptions();
            windowSizeDropdown.onValueChanged.AddListener(OnWindowSizeChanged);
        }
    }

    private void OnEnable()
    {
        RefreshFromSettings();
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

        if (windowSizeDropdown != null)
            windowSizeDropdown.onValueChanged.RemoveListener(OnWindowSizeChanged);
    }

    public void RefreshFromSettings()
    {
        SettingsManager settings = SettingsManager.Instance;

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(settings.MusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(settings.SfxVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(settings.Fullscreen);

        if (windowSizeDropdown != null)
        {
            windowSizeDropdown.SetValueWithoutNotify(FindWindowSizeIndex(settings.WindowSize));
            windowSizeDropdown.interactable = !settings.Fullscreen;
        }
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void PopulateWindowSizeOptions()
    {
        windowSizeDropdown.ClearOptions();

        var labels = new System.Collections.Generic.List<string>();
        foreach (Vector2Int size in SettingsManager.AllowedWindowSizes)
            labels.Add($"{size.x} × {size.y}");

        windowSizeDropdown.AddOptions(labels);
    }

    private static int FindWindowSizeIndex(Vector2Int size)
    {
        for (int i = 0; i < SettingsManager.AllowedWindowSizes.Length; i++)
        {
            if (SettingsManager.AllowedWindowSizes[i] == size) return i;
        }
        return 0;
    }

    private void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSfxVolume(value);
    }

    private void OnFullscreenChanged(bool value)
    {
        SettingsManager.Instance.SetFullscreen(value);

        if (windowSizeDropdown != null)
            windowSizeDropdown.interactable = !value;
    }

    private void OnWindowSizeChanged(int index)
    {
        if (index < 0 || index >= SettingsManager.AllowedWindowSizes.Length) return;

        Vector2Int size = SettingsManager.AllowedWindowSizes[index];
        SettingsManager.Instance.SetWindowSize(size.x, size.y);
    }
}