using System;
using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
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
    private NetworkVariable<FixedString128Bytes> gameStartingTextString = new(writePerm: NetworkVariableWritePermission.Server);

    private void Start()
    {
        // Set up singleplayer warning and chat based on online status.
        bool online = MultiplayerManager.isPlayingOnline;
        singleplayerWarningText.enabled = !online;
        shuffleRolesButton.gameObject.SetActive(IsServer);

        if (online)
        {
            VivoxManager.Instance.SwitchToLobbyChat();
            PlayerPrefs.SetString("GameCode", MultiplayerManager.Instance.joinCode);
        }
        else
        {
            PlayerPrefs.SetString("GameCode", "");
        }

        // Initialize lobby UI.
        startButton.gameObject.SetActive(true);
        aboutToStartGame = false;
        currentLobbyState = LobbyState.WaitingToStart;
        gameStartingTextString.OnValueChanged += UpdateCountDownTextValue;
        gameStartingText.text = "";

        // Set room code text.
        string joinCode = MultiplayerManager.Instance.joinCode;
        roomCodeText.text = $"Room Code: {joinCode}";

        // Set up button actions.
        roomCodeCopyButton.onClick.AddListener(() => GUIUtility.systemCopyBuffer = joinCode);
        leaveButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);
        shuffleRolesButton.onClick.AddListener(MultiplayerManager.Instance.ShufflePlayerRoles);
        startButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.DisallowHavingNoRole();
            if (currentLobbyState == LobbyState.WaitingToStart)
                StartGameCountdown();
            else if (currentLobbyState == LobbyState.Starting)
                CancelStartGameCountdown();
        });

        ToggleStartButton();
    }


    private void ToggleStartButton()
    {
        if (IsServer)
        {
            startButton.gameObject.SetActive(true);
        }
        else
        {
            startButton.gameObject.SetActive(false);
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

        yield return new WaitForSeconds(1);

        MultiplayerManager.Instance.ResetPlayersLoadedIntoGameSceneDictionary();
        Loader.LoadNetworkScene(Loader.Scene.Game);
    }

}
