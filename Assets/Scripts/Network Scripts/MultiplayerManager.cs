using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using System.IO;

public class MultiplayerManager : NetworkSingleton<MultiplayerManager>// handles most of netcode 
{
    public static bool isPlayingOnline;
    public const int MaxPlayers = 5;
    public const string PlayerprefsPlayerNameLocation = "PLAYERNAME";
    public string joinCode { get; private set; }
    public string playerName;

    public Action<bool> OnDisconnectedFromGame;
    public event Action<bool> OnTryingToJoinGame;
    public event Action<PlayerData> Game_ClientDisconnect;
    public event Action OnPlayerDataListChanged;
    public event Action OnKick;

    public NetworkVariable<GameNight> gameNight = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkList<PlayerData> playerDataList = new(writePerm: NetworkVariableWritePermission.Server);
    private Dictionary<ulong, bool> playersLoadedIntoGameSceneDictionary;

    private void Start()
    {
        InitialisePlayerList();

        if (!isPlayingOnline) // i.e we are playing offline
        {
            CreateOfflineRoom();
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha1))
        {
            GameAudioManager.Instance.PlaySfxOneShot("select 1");
            TakeScreenshot();
        }
    }

    void TakeScreenshot()
    {
        string folderName = "Screenshots";
        string directory = Path.Combine(Application.persistentDataPath, folderName);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string filename = $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string path = Path.Combine(directory, filename);

        ScreenCapture.CaptureScreenshot(path, 1);
        Debug.Log($"Screenshot saved to: {path}");
    }

    private void InitialisePlayerList()
    {
        playersLoadedIntoGameSceneDictionary = new();
        playerDataList = new();
        playerDataList.OnListChanged += PlayerDataList_OnListChanged;
    }

    private void PlayerDataList_OnListChanged(NetworkListEvent<PlayerData> changeEvent) // playerDataList has been modified
    {
        OnPlayerDataListChanged?.Invoke();
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong id) // rejected from joining game
    {
        OnDisconnectedFromGame?.Invoke(false);
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong id) // one of the clients have disconnected
    {
        PlayerData disconnectedPlayerData = GetPlayerDataFromClientId(id);
        Game_ClientDisconnect?.Invoke(disconnectedPlayerData);
        playerDataList.Remove(disconnectedPlayerData);

        // if the player has disconnected mid-load, remove them from this dict;
        if (playersLoadedIntoGameSceneDictionary.Keys.Contains(id)) playersLoadedIntoGameSceneDictionary.Remove(id);
        CheckToBegin();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientConnectedServerRpc(FixedString128Bytes name, FixedString128Bytes vId, ServerRpcParams srp = default)
    {
        if (isPlayingOnline)
            playerDataList.Add(new PlayerData
            {
                playerName = name,
                clientId = srp.Receive.SenderClientId,
                role = srp.Receive.SenderClientId == 0 ? PlayerRoles.SecurityOffice : GetRandomUnusedPlayerRole(),
                vivoxId = vId
            });
        else
            playerDataList.Add(new PlayerData
            {
                playerName = name,
                clientId = srp.Receive.SenderClientId,
                role = GetRandomUnusedPlayerRole(),
            });
    }

    private void Client_JoinLobbyChannel(ulong id) // rejected from joining game
    {
        if (id == NetworkManager.Singleton.LocalClientId) VivoxManager.Instance.JoinedRoom(joinCode);

        NetworkManager.Singleton.OnClientConnectedCallback -= Client_JoinLobbyChannel;
    }

    private void JoinRoomApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) // handles join requests
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool gameHasAlreadyStarted = currentSceneName != Loader.Scene.Lobby.ToString();
        bool isGameAboutToStart = currentSceneName == Loader.Scene.Lobby.ToString() && LobbyUI.Instance.aboutToStartGame;
        bool isGameFull = NetworkManager.Singleton.ConnectedClientsIds.Count == MaxPlayers;

        if (gameHasAlreadyStarted) // game ha
        {
            response.Approved = false;
            response.Reason = "The game has already started!";
            return;
        }
        if (isGameAboutToStart)
        {
            response.Approved = false;
            response.Reason = "The game has already about to start!";
            return;
        }
        if (isGameFull)
        {
            response.Approved = false;
            response.Reason = "The room is full!";
            return;
        }

        response.Approved = true;
    }

    public async void HostOnlineRoomAsync()
    {
        OnTryingToJoinGame?.Invoke(true);

        await RelayManager.Instance.CreateRelayAsync(maxPlayers: MaxPlayers);

        joinCode = RelayManager.Instance.GetJoinCode();

        VivoxManager.Instance.JoinedRoom(joinCode);

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.ConnectionApprovalCallback += JoinRoomApproval;

        Loader.LoadNetworkScene(Loader.Scene.Lobby);
    }

    public async void JoinOnlineRoomAsync(string joinCode)
    {
        this.joinCode = joinCode.ToUpper();

        OnTryingToJoinGame?.Invoke(false);

        if (!IsCodeValid())
        {
            OnDisconnectedFromGame?.Invoke(false);
            return;
        }

        await RelayManager.Instance.JoinRelayAsync(joinCode.ToUpper());

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += Client_JoinLobbyChannel;
    }

    public void LeaveGame()
    {
        NetworkManager.Singleton.Shutdown();
        Loader.LoadScene(Loader.Scene.MainMenu);
    }

    public void PlayAgain()
    {
        Loader.LoadNetworkScene(Loader.Scene.Lobby);
    }

    public void CreateOfflineRoom()
    {
        // creates a local, singleplayer lobby
        joinCode = "N/A";
        NetworkManager.Singleton.StartHost();
        Loader.LoadNetworkScene(Loader.Scene.Lobby);
    }

    public bool IsCodeValid() => joinCode.Length == 6;

    public override void OnNetworkSpawn()
    {
        if (isPlayingOnline) ClientConnectedServerRpc(playerName, VivoxService.Instance.SignedInPlayerId);
        else ClientConnectedServerRpc(playerName, "");
    }

    public bool IsPlayerIndexConnected(int playerIndex) // lobby only
    {
        return playerIndex < playerDataList.Count;
    }

    public PlayerData GetLocalPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromClientId(ulong localClientId)
    {
        foreach (PlayerData data in playerDataList)
        {
            if (data.clientId == localClientId)
            {
                return data;
            }
        }
        return default;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromPlayerRole(PlayerRoles playerRole)
    {
        foreach (PlayerData data in playerDataList)
        {
            if (data.role == playerRole)
            {
                return data;
            }
        }
        return default;
    }

    public PlayerData GetPlayerDataFromPlayerName(string name)
    {
        foreach (PlayerData data in playerDataList)
        {
            if (data.playerName.ToString() == name)
            {
                return data;
            }
        }
        return default;
    }

    public PlayerData GetPlayerDataFromVivoxId(string id)
    {
        foreach (PlayerData data in playerDataList)
        {
            if (data.vivoxId.ToString() == id)
            {
                return data;
            }
        }
        return default;
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex) // lobby only
    {
        return playerDataList[playerIndex];
    }

    public PlayerRoles GetLocalPlayerRole()
    {
        return GetLocalPlayerData().role;
    }

    public void ChangePlayerRole(int playerIndex, bool next)
    {
        PlayerData playerData = GetPlayerDataFromPlayerIndex(playerIndex);

        ulong senderId = playerData.clientId;
        PlayerRoles currentRole = playerData.role;
        do
        {
            // true = get next role else get prev role
            currentRole = next ? NextRole(currentRole) : PrevRole(currentRole);
        }
        while (!IsRoleAvailable(currentRole));

        SetPlayerRole(senderId, currentRole);
    }

    private PlayerRoles NextRole(PlayerRoles currentState)
    {
        PlayerRoles[] values = (PlayerRoles[])Enum.GetValues(typeof(PlayerRoles));
        int nextIndex = ((int)currentState + 1) % values.Length;
        return values[nextIndex];
    }

    private PlayerRoles PrevRole(PlayerRoles currentState)
    {
        PlayerRoles[] values = (PlayerRoles[])Enum.GetValues(typeof(PlayerRoles));
        int prevIndex = ((int)currentState - 1 + values.Length) % values.Length;
        return values[prevIndex];
    }

    private void SetPlayerRole(ulong clientId, PlayerRoles role)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(clientId);
        PlayerData playerData = playerDataList[playerDataIndex];
        playerData.role = role;
        playerDataList[playerDataIndex] = playerData;
    }

    private bool IsRoleAvailable(PlayerRoles role)
    {
        if (role == PlayerRoles.None) return true; // any amount of people can spectate

        foreach (PlayerData playerData in playerDataList)
        {
            if (playerData.role == role)
            {
                return false;
            }
        }
        return true;
    }

    private PlayerRoles GetRandomUnusedPlayerRole()
    {
        PlayerRoles[] playerRoles = (PlayerRoles[])Enum.GetValues(typeof(PlayerRoles));
        List<PlayerRoles> shuffledPlayerRoles = playerRoles.OrderBy(x => UnityEngine.Random.value).ToList();

        shuffledPlayerRoles.Remove(PlayerRoles.None); // we dont want to randomly select it

        foreach (PlayerRoles role in shuffledPlayerRoles)
        {
            if (IsRoleAvailable(role))
            {
                return role;
            }
        }

        return PlayerRoles.None; // only select it if there is no other choice
    }

    public void DisallowNobodyHavingARole() // when starting the game the player cant have a spectator
    {
        foreach (PlayerData playerData in playerDataList)
        {
            if (playerData.role != PlayerRoles.None) return; // at least one person has a role
        }

        ShufflePlayerRoles(false);
    }

    public void ShufflePlayerRoles(bool shouldIgnoreSpectators = true) // gives each player a random role
    {
        foreach (PlayerData playerData in playerDataList)
        {
            if (playerData.role == PlayerRoles.None && shouldIgnoreSpectators) continue;
            SetPlayerRole(playerData.clientId, GetRandomUnusedPlayerRole());
        }
    }

    public void KickPlayer(int playerIndex)
    {
        ulong clientId = GetPlayerDataFromPlayerIndex(playerIndex).clientId;
        KickPlayerClientRpc(NewClientRpcSendParams(clientId));
    }

    [ClientRpc]
    public void KickPlayerClientRpc(ClientRpcParams clientRpcParams)
    {
        OnKick?.Invoke();
    }

    public void ResetPlayersLoadedIntoGameSceneDictionary() // called when about to load into the game scene
    {
        playersLoadedIntoGameSceneDictionary = new();
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playersLoadedIntoGameSceneDictionary[clientId] = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyToStartGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playersLoadedIntoGameSceneDictionary[serverRpcParams.Receive.SenderClientId] = true; // once players load into scene they will be true
        CheckToBegin();
    }

    private void CheckToBegin()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != Loader.Scene.Game.ToString() || GameManager.Instance.isPlaying) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playersLoadedIntoGameSceneDictionary[clientId] == false)
            {
                return; //if a player hasn't loaded in dont start
            }
        }
        StartCoroutine(GameManager.Instance.Initalise(gameNight.Value));
    }

    public static ClientRpcParams NewClientRpcSendParams(ulong recipientId) // used when sending clienrrpcs
    {
        return new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { recipientId }
            }
        };
    }

    public static ClientRpcParams NewClientRpcSendParams(ulong[] recipientsId)
    {
        return new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = recipientsId
            }
        };
    }
}
