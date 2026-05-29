
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;
    [SerializeField] private RectTransform settingsContent;
    public Button openSettingsButton;
    public Button closeSettingsButton;

    [Header("Sound Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Future Settings (Placeholders)")]
    public Toggle shadowsToggle;
    public Dropdown qualityDropdown;

    private Canvas settingsCanvas;

    private void Awake()
    {
    }

    private void OnEnable()
    {
        WireEvents();
    }

    private void Start()
    {
        WireEvents();

        UpdateUI();
    }

    private void WireEvents()
    {
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveListener(OpenSettings);
            openSettingsButton.onClick.AddListener(OpenSettings);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(CloseSettings);
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (shadowsToggle != null)
        {
            shadowsToggle.onValueChanged.RemoveListener(OnShadowsChanged);
            shadowsToggle.onValueChanged.AddListener(OnShadowsChanged);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            PopulateQualityDropdown();
        }
    }

    private void Update()
    {
        if (settingsPanel == null) return;
        if (!settingsPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (settingsContent == null) return;

            if (settingsCanvas == null)
                settingsCanvas = settingsContent.GetComponentInParent<Canvas>();

            Camera cam = null;
            if (settingsCanvas != null && settingsCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = settingsCanvas.worldCamera;

            bool inside = RectTransformUtility.RectangleContainsScreenPoint(settingsContent, Input.mousePosition, cam);
            if (!inside)
                CloseSettings();
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
