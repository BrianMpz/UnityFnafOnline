using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkSingleton<GameManager>
{
    public AudioListener DefaultAudioListener;
    public Camera DefaultCamera;

    public static PlayerBehaviour localPlayerBehaviour;
    [SerializeField] private PlayerRoleManager playerRoleManager;
    public const float MaxGameLength = 360f;
    public NetworkVariable<int> currentHour;
    public NetworkVariable<float> currentGameTime = new(writePerm: NetworkVariableWritePermission.Server);
    public GameNight gameNight;
    public event Action OnGameStarted;
    public event Action OnGameWin;
    public event Action OnGameOver;
    public Action<Node, Node> OnAnimatronicMoved;
    public Action OnFoxyStatusChanged;
    public Action<Node> OnFoxyAttacking;
    public Action<PlayerRoles> OnPlayerPowerDown;
    public bool isPlaying;
    public bool IsSpectating { get => SpectatorUI.Instance.isSpectating; }
    private Coroutine gameTimeCoroutine;

    private void Start()
    {
        MultiplayerManager.Instance.ReadyToStartGameServerRpc();
    }

    private void Update()
    {
        if (!IsServer) return;
        HandleGameTime();
    }

    private void HandleGameTime()
    {
        float gameTime = currentGameTime.Value;
        int newGameHour;
        if (gameTime < 60f) newGameHour = 12;
        else if (gameTime < 120f) newGameHour = 1;
        else if (gameTime < 180f) newGameHour = 2;
        else if (gameTime < 240f) newGameHour = 3;
        else if (gameTime < 300f) newGameHour = 4;
        else if (gameTime < 360f) newGameHour = 5;
        else newGameHour = 6;

        currentHour.Value = newGameHour;
    }

    public IEnumerator Initalise(GameNight gameNight)
    {
        yield return PlayerRoleManager.Instance.EstablishOwnerships();

        StartGameClientRpc(gameNight);
    }

    [ClientRpc]
    public void StartGameClientRpc(GameNight gameNight)
    {
        this.gameNight = gameNight;
        StartCoroutine(StartGame());
    }

    public IEnumerator StartGame()
    {
        localPlayerBehaviour = playerRoleManager.GetLocalPlayerBehaviour();

        yield return new WaitForSeconds(2);

        if (isPlaying) yield break;

        isPlaying = true;
        OnGameStarted?.Invoke();
        StartGameMusic();

        if (!IsServer) yield break;

        gameTimeCoroutine ??= StartCoroutine(StartGameTime());
    }

    private async void StartGameMusic()
    {
        // Stop any currently playing music
        GameAudioManager.Instance.StopMusic();

        // Play new music
        AudioSource lastBreath = GameAudioManager.Instance.PlayMusic("last breath");

        float duration = 9f;
        float elapsedTime = 0f;

        // Fade out loop
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            lastBreath.volume = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            await Task.Yield(); // Smooth async updates
        }

        GameAudioManager.Instance.StopMusic();
    }

    private IEnumerator StartGameTime()
    {
        Debug.Log("Game has started!");

        currentGameTime.Value = 0;

        float gameEndTime = MaxGameLength;

        while (currentGameTime.Value < gameEndTime)
        {
            yield return null;
            currentGameTime.Value += Time.deltaTime;

            if (!isPlaying) yield break;
        }

        Debug.Log("Game has Ended!");

        EndGameClientRpc();
    }

    [ServerRpc(RequireOwnership = false)] public void OnPlayerPowerDownServerRpc(PlayerRoles playerRole) => OnPlayerPowerDown?.Invoke(playerRole);

    [ClientRpc]
    public void EndGameClientRpc()
    {
        StartCoroutine(EndGame());

        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
    }

    public IEnumerator EndGame()
    {
        CompleteNight(); // Mark night as completed

        OnGameWin?.Invoke();
        isPlaying = false;

        GameAudioManager.Instance.StopAllSfx();
        GameAudioManager.Instance.PlaySfxInterruptable("game win");

        yield return new WaitForSeconds(7);

        GameAudioManager.Instance.PlaySfxOneShot("kids cheering");
    }

    private void CompleteNight()
    {
        int nightIndex = (int)gameNight;

        // Unlock the current night and all previous nights
        for (int i = 0; i <= nightIndex; i++)
        {
            PlayerPrefs.SetInt("CompletedNight_" + i, 1);
        }

        // Unlock the next night (if it exists)
        if (nightIndex + 1 < Enum.GetValues(typeof(GameNight)).Length)
        {
            PlayerPrefs.SetInt("CompletedNight_" + (nightIndex + 1), 1);
        }

        PlayerPrefs.Save(); // Save to disk to persist across sessions
    }

    public IEnumerator HandleDeath(FixedString64Bytes killer)
    {
        RelayDeathServerRpc(killer);

        yield return MiscellaneousGameUI.Instance.deathScreenUI.Show();

        if (!playerRoleManager.IsEveryoneDead()) SpectatorUI.Instance.Show();

        CheckForGameOverServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RelayDeathServerRpc(FixedString64Bytes killer, ServerRpcParams serverRpcParams = default)
    {
        if (playerRoleManager.IsEveryoneDead())
        {
            StopCoroutine(gameTimeCoroutine);
            isPlaying = false;
        }

        RelayDeathClientRpc(killer, serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void RelayDeathClientRpc(FixedString64Bytes killer, ulong SenderClientId)
    {
        if (SenderClientId == NetworkManager.Singleton.LocalClientId) return;

        PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromClientId(SenderClientId);

        Debug.Log($"{playerData.playerName} has met their end at the hands of {killer}");

        SpectatorUI.Instance.RefreshSpectator();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckForGameOverServerRpc()
    {
        if (playerRoleManager.IsEveryoneDead())
        {
            RelayGameOverClientRpc();
        }
    }

    [ClientRpc]
    private void RelayGameOverClientRpc()
    {
        OnGameOver?.Invoke();
        isPlaying = false;
        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
    }

    public void BackToLobby()
    {
        CleanUpNetworkObjects();
        MultiplayerManager.Instance.PlayAgain();
    }

    private void CleanUpNetworkObjects()
    {
        // Get all the NetworkObjects in the current scene
        var networkObjects = FindObjectsByType<NetworkObject>(sortMode: FindObjectsSortMode.None);

        foreach (var networkObject in networkObjects)
        {
            // Check if the object is not marked as persistent
            if (networkObject.gameObject.scene.name != "DontDestroyOnLoad")
            {
                // Despawn the non-persistent object
                if (networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
            }
        }
    }
}

public enum GameNight
{
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven
}

