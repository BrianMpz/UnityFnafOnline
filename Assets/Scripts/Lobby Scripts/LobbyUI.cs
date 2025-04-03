using System;
using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UI;

public class LobbyUI : NetworkSingleton<LobbyUI>
{
    public enum LobbyState
    {
        WaitingToStart,
        Starting,
    }

    public LobbyState currentLobbyState;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button shuffleRolesButton;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text gameStartingText;
    [SerializeField] private Button roomCodeCopyButton;
    [SerializeField] private int countDownTime;
    public bool aboutToStartGame;
    public Action AboutToStartGame;
    public Action CancelToStartGame;
    [SerializeField] private TMP_Text singleplayerWarningText;
    [SerializeField] private Image fadeOutImage;
    private NetworkVariable<FixedString128Bytes> gameStartingTextString = new(writePerm: NetworkVariableWritePermission.Server);

    private void Start()
    {
        bool online = MultiplayerManager.isPlayingOnline;

        fadeOutImage.color = new(0, 0, 0, 0);

        // Set up singleplayer warning and chat based on online status.
        singleplayerWarningText.enabled = !online;
        shuffleRolesButton.gameObject.SetActive(IsServer);
        startButton.gameObject.SetActive(IsServer);

        SetGameCode(online);
        HandleLobbyMusic();
        SetJoinCodeText();

        // Initialize lobby UI.
        aboutToStartGame = false;
        currentLobbyState = LobbyState.WaitingToStart;
        gameStartingTextString.OnValueChanged += UpdateCountDownTextValue;
        gameStartingText.text = "";

        // Set up button actions.
        roomCodeCopyButton.onClick.AddListener(() => GUIUtility.systemCopyBuffer = MultiplayerManager.Instance.joinCode);
        leaveButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);

        if (!IsServer) return;

        shuffleRolesButton.onClick.AddListener(() => { MultiplayerManager.Instance.ShufflePlayerRoles(true); });
        startButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.DisallowNobodyHavingARole();

            if (currentLobbyState == LobbyState.WaitingToStart) StartGameCountdown();
            else if (currentLobbyState == LobbyState.Starting) CancelStartGameCountdown();
        });
    }

    private void SetJoinCodeText()
    {
        string joinCode = MultiplayerManager.Instance.joinCode;
        roomCodeText.text = $"Room Code: {joinCode}";
    }

    private void HandleLobbyMusic()
    {
        GameAudioManager.Instance.StopAllSfx();
        if (!GameAudioManager.Instance.GetMusic().isPlaying) GameAudioManager.Instance.PlayMusic("watch your 6", 0.5f);
    }

    private void SetGameCode(bool online)
    {
        if (online)
        {
            VivoxManager.Instance.SwitchToLobbyChat();
            PlayerPrefs.SetString("GameCode", MultiplayerManager.Instance.joinCode);
        }
        else
        {
            PlayerPrefs.SetString("GameCode", "");
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= CancelStartGameCountdown;
        NetworkManager.Singleton.OnClientDisconnectCallback -= CancelStartGameCountdown;

        base.OnDestroy();
    }

    private void StartGameCountdown()
    {
        StartCoroutine(CountdownToGameStart());
        currentLobbyState = LobbyState.Starting;
        startButton.GetComponentInChildren<TMP_Text>().text = "Cancel";

        NetworkManager.Singleton.OnClientConnectedCallback += CancelStartGameCountdown;
        NetworkManager.Singleton.OnClientDisconnectCallback += CancelStartGameCountdown;
    }

    public void CancelStartGameCountdown(ulong id = default)
    {
        CancelToStartGame.Invoke();
        aboutToStartGame = false;

        StopAllCoroutines();

        currentLobbyState = LobbyState.WaitingToStart;
        startButton.GetComponentInChildren<TMP_Text>().text = "Start";
        gameStartingTextString.Value = $"";

        NetworkManager.Singleton.OnClientConnectedCallback -= CancelStartGameCountdown;
        NetworkManager.Singleton.OnClientDisconnectCallback -= CancelStartGameCountdown;
    }

    private void UpdateCountDownTextValue(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        gameStartingText.text = newValue.ToString();
    }

    private IEnumerator CountdownToGameStart()
    {
        AboutToStartGame.Invoke();
        aboutToStartGame = true;

        for (int i = countDownTime; i > 0; i--)
        {
            gameStartingTextString.Value = $"Game starting in {i}s...";

            yield return new WaitForSeconds(1);
        }

        gameStartingTextString.Value = $"Game starting in 0s...";
        startButton.gameObject.SetActive(false);

        FadeInClientRpc();
        yield return new WaitForSeconds(2f);

        MultiplayerManager.Instance.ResetPlayersLoadedIntoGameSceneDictionary();
        Loader.LoadNetworkScene(Loader.Scene.Game);
    }

    [ClientRpc]
    private void FadeInClientRpc()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float duration = 1.5f; // Duration of the fade-in effect
        float elapsedTime = 0f;

        Color color = fadeOutImage.color;
        color.a = 0; // Start with fully transparent
        fadeOutImage.color = color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0, 1, elapsedTime / duration); // Gradually increase alpha
            GameAudioManager.Instance.GetMusic().volume = Mathf.Lerp(1, 0.1f, elapsedTime / duration);
            fadeOutImage.color = color;
            yield return null; // Wait for the next frame
        }

        // Ensure it's fully opaque at the end
        color.a = 1;
        fadeOutImage.color = color;
    }

}
