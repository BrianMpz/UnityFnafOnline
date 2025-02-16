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
    [SerializeField] private NotImplementedInBuildUI notImplementedInBuildUI;

    private void Start()
    {
        string playerName = PlayerPrefs.GetString(MultiplayerManager.PlayerprefsPlayerNameLocation, "");
        if (playerName != "") playerNameInputField.text = playerName;

        CanPlayOnline = canPlayOnline;

        playButton.onClick.AddListener(() =>
        {
            if (!CanPlayOnline)
            {
                notImplementedInBuildUI.Show();
                return;
            }

            if (playerNameInputField.text != "")
                PlayerPrefs.SetString(MultiplayerManager.PlayerprefsPlayerNameLocation, playerNameInputField.text);

            MultiplayerManager.isPlayingOnline = true;
            Loader.LoadScene(Loader.Scene.Matchmaking);

        });

        settingsButton.onClick.AddListener(() =>
        {
            notImplementedInBuildUI.Show();
        });

        playOfflineButton.onClick.AddListener(() =>
        {
            if (playerNameInputField.text != "")
                PlayerPrefs.SetString(MultiplayerManager.PlayerprefsPlayerNameLocation, playerNameInputField.text);

            MultiplayerManager.isPlayingOnline = false;
            Loader.LoadScene(Loader.Scene.Matchmaking);
        });

        quitButton.onClick.AddListener(Application.Quit);
    }
}
