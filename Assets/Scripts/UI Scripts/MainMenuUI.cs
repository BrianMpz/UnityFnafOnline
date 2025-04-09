using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    public static bool CanDebug;
    [SerializeField] public bool canDebug;
    public static bool CanPlayOnline;
    [SerializeField] public bool canPlayOnline;
    public static bool CanUseVoiceChat;
    [SerializeField] private bool canUseVoiceChat;

    [SerializeField] private Button playButton;
    [SerializeField] private Button playOfflineButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_Text ClearDataText;

    private const float ClearDataKeyHoldLength = 2.5f;
    private Coroutine clearDataCoroutine;

    private void Start()
    {
        string playerName = PlayerPrefs.GetString(MultiplayerManager.PlayerprefsPlayerNameLocation, "");
        if (playerName != "") playerNameInputField.text = playerName;

        CanPlayOnline = canPlayOnline;
        CanDebug = canDebug;
        CanUseVoiceChat = canUseVoiceChat;

        playButton.onClick.AddListener(PlayOnline);
        playOfflineButton.onClick.AddListener(PlayOffline);
        settingsButton.onClick.AddListener(SettingsUI.Instance.Show);

        quitButton.onClick.AddListener(Application.Quit);

        playerNameInputField.onValueChanged.AddListener(s => { TruncateUsername(s); });

        GameAudioManager.Instance.PlayMusic("watch your 6");
    }

    private void PlayOffline()
    {
        if (playerNameInputField.text != "") PlayerPrefs.SetString(MultiplayerManager.PlayerprefsPlayerNameLocation, playerNameInputField.text);

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

    private void TruncateUsername(string input)
    {
        int maxUsernameLength = 12;

        if (string.IsNullOrEmpty(input) || input.Length <= maxUsernameLength) return;

        playerNameInputField.text = input[..maxUsernameLength];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D) && clearDataCoroutine == null)
        {
            clearDataCoroutine = StartCoroutine(ClearDataCountdown());
        }

        if (Input.GetKeyUp(KeyCode.D) && clearDataCoroutine != null)
        {
            StopCoroutine(clearDataCoroutine);
            clearDataCoroutine = null;
            ClearDataText.text = "Hold 'D' to clear Save Data";
        }
    }

    private IEnumerator ClearDataCountdown()
    {
        float elapsedTime = 0f;

        yield return new WaitForSeconds(0.5f);

        while (elapsedTime < ClearDataKeyHoldLength)
        {
            if (!Input.GetKey(KeyCode.D))
            {
                yield break; // Stop if key is released
            }

            ClearDataText.text = "Clearing Data...";
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ClearData();
    }

    private void ClearData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        ClearDataText.text = "Save Data has been Cleared!";
        playerNameInputField.text = "";

        clearDataCoroutine = null; // Reset coroutine reference
    }
}
