using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SettingsUI : Singleton<SettingsUI>
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button leaveGameButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button testVoiceChatAudioVolume;
    [SerializeField] private Button resetToDefaultButton;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider voiceChatSlider;
    [SerializeField] private TMP_Dropdown frameRateDropdown;
    [SerializeField] private TMP_Dropdown fullscreenDropdown;
    [SerializeField] private TMP_Text gameVersionText;
    [SerializeField] TMP_Text fpsText;
    private float deltaTime;
    [SerializeField] private AudioMixer audioMixer;

    private void Start()
    {
        gameVersionText.text = $"v{Application.version}";

        closeButton.onClick.AddListener(Hide);

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != Loader.Scene.MainMenu.ToString())
        {
            leaveGameButton.gameObject.SetActive(true);
            leaveGameButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);
        }
        else
        {
            leaveGameButton.gameObject.SetActive(false);
        }

        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        voiceChatSlider.onValueChanged.AddListener(SetVoiceChatVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        fullscreenDropdown.onValueChanged.AddListener(SetFullscreenMode);
        frameRateDropdown.onValueChanged.AddListener(SetFrameRate);

        testVoiceChatAudioVolume.onClick.AddListener(GameAudioManager.Instance.TestVolume);
        resetToDefaultButton.onClick.AddListener(ResetToDefaults);

        InitializeFullscreenDropdown();
        InitializeFrameRateDropdown();
        LoadSavedSettings();

        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!canvas.enabled) Show(); else Hide();
        }

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
    }

    public void Show()
    {
        EventSystem.current.SetSelectedGameObject(null);
        canvas.enabled = true;
    }

    public void Hide()
    {
        canvas.enabled = false;
    }

    private void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    private void SetVoiceChatVolume(float value)
    {
        audioMixer.SetFloat("VoiceChatVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("VoiceChatVolume", value);
    }

    private void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void SetFullscreenMode(int index)
    {
        switch (index)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 1: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 2: Screen.fullScreenMode = FullScreenMode.MaximizedWindow; break;
            case 3: Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }
        PlayerPrefs.SetInt("FullscreenMode", index);
    }

    private void InitializeFullscreenDropdown()
    {
        fullscreenDropdown.ClearOptions();
        fullscreenDropdown.AddOptions(new List<string> {
            "Fullscreen", "Exclusive Fullscreen", "Maximized Window", "Windowed"
        });

        fullscreenDropdown.value = PlayerPrefs.GetInt("FullscreenMode", 0);
        fullscreenDropdown.RefreshShownValue();
    }

    private void SetFrameRate(int index)
    {
        if (index == 0)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = index switch
            {
                1 => 30,
                2 => 60,
                3 => 90,
                4 => 120,
                5 => 144,
                _ => 60
            };
        }

        PlayerPrefs.SetInt("TargetFrameRate", index);
    }


    private void InitializeFrameRateDropdown()
    {
        frameRateDropdown.ClearOptions();

        frameRateDropdown.AddOptions(new List<string>
        {
            "VSync", "30FPS", "60FPS", "90FPS", "120FPS", "144FPS"
        });

        frameRateDropdown.value = PlayerPrefs.GetInt("TargetFrameRate", 0);
        frameRateDropdown.RefreshShownValue();
    }

    private void LoadSavedSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedVoiceChat = PlayerPrefs.GetFloat("VoiceChatVolume", 1f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
        int fullscreenMode = PlayerPrefs.GetInt("FullscreenMode", 0);
        int targetFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 0);

        masterVolumeSlider.value = savedVolume;
        voiceChatSlider.value = savedVoiceChat;
        musicVolumeSlider.value = savedMusic;
        sfxVolumeSlider.value = savedSFX;
        fullscreenDropdown.value = fullscreenMode;
        frameRateDropdown.value = targetFrameRate;

        SetMasterVolume(savedVolume);
        SetVoiceChatVolume(savedVoiceChat);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
        SetFullscreenMode(fullscreenMode);
        SetFrameRate(targetFrameRate);
    }

    public void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetVoiceChatVolume(1f);
        SetMusicVolume(1f);
        SetSFXVolume(1f);
        SetFullscreenMode(0);
        SetFrameRate(0);

        masterVolumeSlider.value = 1f;
        voiceChatSlider.value = 1f;
        musicVolumeSlider.value = 1f;
        sfxVolumeSlider.value = 1f;
        fullscreenDropdown.value = 0;
        frameRateDropdown.value = 0;

        fullscreenDropdown.RefreshShownValue();
        frameRateDropdown.RefreshShownValue();
    }

}
