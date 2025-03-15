using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public static bool CanPlayOnline;
    [SerializeField] public bool canPlayOnline;
    [SerializeField] private Button playButton;
    [SerializeField] private Button playOfflineButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_InputField playerNameInputField;

    private void Start()
    {
        string playerName = PlayerPrefs.GetString(MultiplayerManager.PlayerprefsPlayerNameLocation, "");
        if (playerName != "") playerNameInputField.text = playerName;

        CanPlayOnline = canPlayOnline;

        playButton.onClick.AddListener(PlayOnline);
        playOfflineButton.onClick.AddListener(PlayOffline);

        settingsButton.onClick.AddListener(SettingsUI.Instance.Show);

        quitButton.onClick.AddListener(Application.Quit);

        playerNameInputField.onValueChanged.AddListener(s => { TruncateUsername(s); });
    }

    private void PlayOffline()
    {
        if (playerNameInputField.text != "")
            PlayerPrefs.SetString(MultiplayerManager.PlayerprefsPlayerNameLocation, playerNameInputField.text);

        MultiplayerManager.isPlayingOnline = false;
        Loader.LoadScene(Loader.Scene.Matchmaking);
    }

    private void PlayOnline()
    {
        if (!CanPlayOnline)
        {
            NotImplementedInBuildUI.Instance.Show();
            return;
        }

        if (playerNameInputField.text != "") PlayerPrefs.SetString(MultiplayerManager.PlayerprefsPlayerNameLocation, playerNameInputField.text);

        MultiplayerManager.isPlayingOnline = true;
        Loader.LoadScene(Loader.Scene.Matchmaking);

    }

    private void TruncateUsername(string input) // set max username length to 14
    {
        if (string.IsNullOrEmpty(input) || input.Length <= 14) return;

        playerNameInputField.text = input[..14];
    }
}
