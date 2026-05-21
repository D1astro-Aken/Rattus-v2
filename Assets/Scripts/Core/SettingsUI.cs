
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;
    public Button openSettingsButton;
    public Button closeSettingsButton;

    [Header("Sound Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Future Settings (Placeholders)")]
    public Toggle shadowsToggle;
    public Dropdown qualityDropdown;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.AddListener(OpenSettings);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (shadowsToggle != null)
        {
            shadowsToggle.onValueChanged.AddListener(OnShadowsChanged);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            PopulateQualityDropdown();
        }

        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UpdateUI();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (SettingsManager.instance == null) return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SettingsManager.instance.masterVolume;
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = SettingsManager.instance.musicVolume;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SettingsManager.instance.sfxVolume;
        }

        if (shadowsToggle != null)
        {
            shadowsToggle.isOn = SettingsManager.instance.shadowsEnabled;
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.value = SettingsManager.instance.qualityLevel;
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (SettingsManager.instance != null)
        {
            SettingsManager.instance.SetMasterVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (SettingsManager.instance != null)
        {
            SettingsManager.instance.SetMusicVolume(value);
            if (MusicManager.instance != null)
            {
                MusicManager.instance.UpdateVolume();
            }
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (SettingsManager.instance != null)
        {
            SettingsManager.instance.SetSFXVolume(value);
            if (SoundManager.instance != null)
            {
                SoundManager.instance.UpdateVolume();
            }
        }
    }

    private void OnShadowsChanged(bool enabled)
    {
        if (SettingsManager.instance != null)
        {
            SettingsManager.instance.SetShadowsEnabled(enabled);
        }
    }

    private void OnQualityChanged(int level)
    {
        if (SettingsManager.instance != null)
        {
            SettingsManager.instance.SetQualityLevel(level);
        }
    }

    private void PopulateQualityDropdown()
    {
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        }
    }
}
