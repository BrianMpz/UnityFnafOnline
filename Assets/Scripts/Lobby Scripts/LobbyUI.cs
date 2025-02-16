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
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text gameStartingText;
    private NetworkVariable<FixedString128Bytes> gameStartingTextString = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private Button roomCodeCopyButton;
    [SerializeField] private int countDownTime;
    public bool aboutToStartGame;
    public Action AboutToStartGame;
    public Action CancelToStartGame;
    [SerializeField] private TMP_Text singleplayerWarningText;

    private void Start()
    {
        singleplayerWarningText.enabled = !MultiplayerManager.isPlayingOnline;
        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();

        if (!NetworkObject.IsSpawned) NetworkObject.Spawn();

        startButton.gameObject.SetActive(true);
        aboutToStartGame = false;

        currentLobbyState = LobbyState.WaitingToStart;

        gameStartingTextString.OnValueChanged += UpdateCountDownTextValue;
        gameStartingText.text = "";

        string joinCode = MultiplayerManager.Instance.joinCode;
        roomCodeText.text = "Room Code: " + MultiplayerManager.Instance.joinCode;

        if (MultiplayerManager.isPlayingOnline) PlayerPrefs.SetString("GameCode", joinCode);
        else PlayerPrefs.SetString("GameCode", "");

        roomCodeCopyButton.onClick.AddListener(() => { GUIUtility.systemCopyBuffer = joinCode; });

        leaveButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.LeaveGame();
        });

        startButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.DisallowHavingNoRole();

            if (currentLobbyState == LobbyState.WaitingToStart) // start game
            {
                StartGameCountdown();
            }
            else if (currentLobbyState == LobbyState.Starting) // cancel start game
            {
                CancelStartGameCountdown();
            }
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

        MultiplayerManager.Instance.ResetReadyPlayersDictionary();
        NetworkObject.Despawn();
        Loader.LoadNetworkScene(Loader.Scene.Game);
    }

}
