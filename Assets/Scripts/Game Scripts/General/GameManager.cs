using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkSingleton<GameManager>
{
    public NetworkVariable<uint> XpGained = new(writePerm: NetworkVariableWritePermission.Server);
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

        float duration = 7f;
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
        currentGameTime.Value = 0;

        float gameEndTime = MaxGameLength;

        while (currentGameTime.Value < gameEndTime)
        {
            yield return null;

            CalculateXP();

            currentGameTime.Value += Time.deltaTime;

            if (!isPlaying) yield break; // game over
        }

        CalculateXP(true);

        WinGameClientRpc();
    }

    private void CalculateXP(bool log = false)
    {
        float timeSurvived = Mathf.Min(currentGameTime.Value, MaxGameLength);
        int playersAlive = Mathf.Max(playerRoleManager.CountPlayersAlive(), 1);
        int totalPlayableRoles = Enum.GetValues(typeof(PlayerRoles)).Length - 1; // take away one for spectator role
        int totalNights = Enum.GetValues(typeof(GameNight)).Length;
        int currentNight = (int)gameNight + 1;

        XpGained.Value = CalculateXP(timeSurvived, playersAlive, totalPlayableRoles, totalNights, currentNight, log);
    }

    public static uint CalculateXP(float timeSurvived, float playersAlive, float totalPlayableRoles, float totalNights, float currentNight, bool log = false)
    {
        // Normalize key values
        float timeRatio = Mathf.Pow(timeSurvived / MaxGameLength, 4f);
        float nightRatio = Mathf.Pow(currentNight / totalNights, 4f);
        float survivalRate = playersAlive / totalPlayableRoles;
        float gameWinMultiplier = playersAlive == 0 ? 0.1f : 1f;

        // Final XP computation
        float maxXp = 1000000f;
        float finalXp = maxXp * timeRatio * nightRatio * survivalRate * gameWinMultiplier;

        if (log)
        {
            // Debug.Log($"--- XP Calculation Log ---");
            // Debug.Log($"Time Survived: {timeSurvived} / {MaxGameLength} → Time Ratio: {timeRatio:F3}");
            // Debug.Log($"Night: {(float)gameNight + 1f} / {totalNights} → Night Ratio: {nightRatio:F3}");
            // Debug.Log($"Players Alive: {playersAlive} / {totalPlayableRoles} → Survival Ratio: {survivalRate:F3}");
            // Debug.Log($"Game Win Multiplier: {gameWinMultiplier:F2}");
            // Debug.Log($"Final XP: {finalXp}");

            Debug.Log($"Final XP gained on Night {currentNight}: {finalXp}");
        }

        return (uint)Mathf.RoundToInt(finalXp);
    }

    [ServerRpc(RequireOwnership = false)] public void OnPlayerPowerDownServerRpc(PlayerRoles playerRole) => OnPlayerPowerDown?.Invoke(playerRole);

    [ClientRpc]
    public void WinGameClientRpc()
    {
        WinGame();

        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
    }

    public void WinGame()
    {
        CompleteNight(); // Mark night as completed

        OnGameWin?.Invoke();
        isPlaying = false;
    }

    private void CompleteNight()
    {
        int nightIndex = (int)gameNight;

        // Unlock the current night and all previous nights
        for (int i = 0; i <= nightIndex; i++)
        {
            PlayerPrefs.SetInt("HasCompletedNight_" + i, 1);
        }

        // Unlock the next night (if it exists)
        if (nightIndex + 1 < Enum.GetValues(typeof(GameNight)).Length)
        {
            PlayerPrefs.SetInt("HasCompletedNight_" + (nightIndex + 1), 1);
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
            CalculateXP(true);

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
        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(sortMode: FindObjectsSortMode.None);

        foreach (NetworkObject networkObject in networkObjects)
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

