using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SettingsUI : Singleton<SettingsUI>
{
    [Header("UI Elements")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button leaveGameButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider voiceChatSlider;
    [SerializeField] private Button testVoiceChatAudioVolume;
    [SerializeField] private TMP_Dropdown fullscreenDropdown;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;

    private void Start()
    {
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
        fullscreenDropdown.onValueChanged.AddListener(SetFullscreenMode);

        testVoiceChatAudioVolume.onClick.AddListener(GameAudioManager.Instance.TestVolume);

        InitializeFullscreenDropdown();
        LoadSavedSettings();

        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!canvas.enabled) Show(); else Hide();
        }
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

    private void SetFullscreenMode(int index)
    {
        switch (index)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 1: Screen.fullScreenMode = FullScreenMode.MaximizedWindow; break;
            case 2: Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }
        PlayerPrefs.SetInt("FullscreenMode", index);
    }

    private void InitializeFullscreenDropdown()
    {
        fullscreenDropdown.ClearOptions();
        fullscreenDropdown.AddOptions(new List<string> {
            "Fullscreen", "Maximized Window", "Windowed"
        });

        fullscreenDropdown.value = PlayerPrefs.GetInt("FullscreenMode", 0);
        fullscreenDropdown.RefreshShownValue();
    }

    private void LoadSavedSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedVoiceChat = PlayerPrefs.GetFloat("VoiceChatVolume", 1f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        int fullscreenMode = PlayerPrefs.GetInt("FullscreenMode", 0);

        masterVolumeSlider.value = savedVolume;
        voiceChatSlider.value = savedVoiceChat;
        musicVolumeSlider.value = savedMusic;
        fullscreenDropdown.value = fullscreenMode;

        SetMasterVolume(savedVolume);
        SetVoiceChatVolume(savedVoiceChat);
        SetMusicVolume(savedMusic);
        SetFullscreenMode(fullscreenMode);
    }
}
