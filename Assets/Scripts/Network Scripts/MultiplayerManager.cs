using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : NetworkSingleton<MultiplayerManager>// handles most of netcode 
{
    public static bool isPlayingOnline;
    public const int MaxPlayers = 4;
    public const string PlayerprefsPlayerNameLocation = "PLAYERNAME";
    public string joinCode { get; private set; }
    public string playerName;

    public Action<bool> OnDisconnectedFromGame;
    public event Action<bool> OnTryingToJoinGame;
    public event Action<PlayerData> Game_ClientDisconnect;
    public event Action OnPlayerDataListChanged;
    public NetworkVariable<GameNight> gameNight = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkList<PlayerData> playerDataList;
    private Dictionary<ulong, bool> readyPlayersDictionary;

    private void Start()
    {
        InitialisePlayerList();

        if (!isPlayingOnline) // i.e we are playing offline
        {
            CreateOfflineRoom();
        }
    }

    private void InitialisePlayerList()
    {
        readyPlayersDictionary = new();
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

    private void Client_JoinLobbyChannel(ulong id) // rejected from joining game
    {
        if (id == NetworkManager.Singleton.LocalClientId) VivoxManager.Instance.JoinedRoom(joinCode);

        NetworkManager.Singleton.OnClientConnectedCallback -= Client_JoinLobbyChannel;
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong id) // one of the clients have disconnected
    {
        PlayerData disconnectedPlayerData = GetPlayerDataFromClientId(id);
        Game_ClientDisconnect?.Invoke(disconnectedPlayerData);
        playerDataList.Remove(disconnectedPlayerData);
    }

    private void JoinRoomApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) // handles join requests
    {
        if (SceneManager.GetActiveScene().name != Loader.Scene.Lobby.ToString())
        {
            response.Approved = false;
            response.Reason = "The game has already started!";
            return;
        }
        if (SceneManager.GetActiveScene().name == Loader.Scene.Lobby.ToString() && LobbyUI.Instance.aboutToStartGame)
        {
            response.Approved = false;
            response.Reason = "The game has already about to start!";
            return;
        }
        if (NetworkManager.Singleton.ConnectedClientsIds.Count == MaxPlayers)
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
        joinCode = "N/A";
        NetworkManager.Singleton.StartHost();
        Loader.LoadNetworkScene(Loader.Scene.Lobby);
    }

    public bool IsCodeValid()
    {
        if (joinCode.Length == 6) return true;

        return false;
    }

    public override void OnNetworkSpawn()
    {
        if (isPlayingOnline) ClientConnectedServerRpc(playerName, VivoxService.Instance.SignedInPlayerId);
        else ClientConnectedServerRpc(playerName, "");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientConnectedServerRpc(FixedString128Bytes name, FixedString128Bytes vId, ServerRpcParams srp = default)
    {
        if (isPlayingOnline)
            playerDataList.Add(new PlayerData
            {
                playerName = name,
                clientId = srp.Receive.SenderClientId,
                role = GetUnusedPlayerRole(),
                vivoxId = vId
            });
        else
            playerDataList.Add(new PlayerData
            {
                playerName = name,
                clientId = srp.Receive.SenderClientId,
                role = GetUnusedPlayerRole(),
            });

        Debug.Log($"Adding {name}, with Id {srp.Receive.SenderClientId} and vivox Id {vId}");
    }

    public bool IsPlayerIndexConnected(int playerIndex)
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

    private ulong GetClientIdFromRole(PlayerRoles role)
    {
        foreach (PlayerData data in playerDataList)
        {
            if (data.role == role)
            {
                return data.clientId;
            }
        }
        return default;
    }

    public void ChangePlayerRoleToPrevious(PlayerRoles role)
    {
        ulong senderId = GetClientIdFromRole(role);
        do
        {
            role = PrevRole(role);
        }
        while (!IsRoleAvailable(role));

        SetPlayerRole(role, senderId);
    }

    public void ChangePlayerRoleToNext(PlayerRoles role)
    {
        ulong senderId = GetClientIdFromRole(role);
        do
        {
            role = NextRole(role);
        }
        while (!IsRoleAvailable(role));

        SetPlayerRole(role, senderId);
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

    private void SetPlayerRole(PlayerRoles role, ulong clientId)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(clientId);
        PlayerData playerData = playerDataList[playerDataIndex];
        playerData.role = role;
        playerDataList[playerDataIndex] = playerData;
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

    private bool IsRoleAvailable(PlayerRoles role)
    {
        foreach (PlayerData playerData in playerDataList)
        {
            if (playerData.role == role)
            {
                return false;
            }
        }
        return true;
    }

    private PlayerRoles GetUnusedPlayerRole()
    {
        foreach (PlayerRoles role in Enum.GetValues(typeof(PlayerRoles)))
        {
            if (IsRoleAvailable(role))
            {
                return role;
            }
        }
        return default;
    }

    public void DisallowHavingNoRole()
    {
        PlayerData playerData = GetLocalPlayerData();

        if (playerDataList.Count == 1 &&
            (playerData.role == PlayerRoles.None ||
            playerData.role == PlayerRoles.Kitchen))
        {
            SetPlayerRole(PlayerRoles.SecurityOffice, NetworkManager.Singleton.LocalClientId);
        }
    }

    public void ResetReadyPlayersDictionary()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            readyPlayersDictionary[clientId] = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyToStartGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        readyPlayersDictionary[serverRpcParams.Receive.SenderClientId] = true; // once players load into scene they will be true
        CheckToBegin();
    }

    private void CheckToBegin()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (readyPlayersDictionary[clientId] == false)
            {
                return; //if a player hasn't loaded in dont start
            }
        }
        StartCoroutine(GameManager.Instance.Initalise(gameNight.Value));
    }

    public static ClientRpcParams NewClientRpcSendParams(ulong recipientId)
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
