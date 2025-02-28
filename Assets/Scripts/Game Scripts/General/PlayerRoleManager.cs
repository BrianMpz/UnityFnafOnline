using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerRoleManager : NetworkSingleton<PlayerRoleManager>
{
    public SecurityOfficeBehaviour securityOfficeBehaviour;
    public PartsAndServiceBehaviour partsAndServiceBehaviour;
    public BackstagePlayerBehaviour backstagePlayerBehaviour;

    private void Start()
    {
        if (IsServer) MultiplayerManager.Instance.Game_ClientDisconnect += Game_OnClientDisconnect;
        GameManager.Instance.OnGameStarted += InitialiseLocalPlayer;
        GameManager.Instance.OnGameWin += DisableAllPlayers;
        GameManager.Instance.OnGameOver += DisableAllPlayers;

        DisableAllPlayers();

        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToPrivateChat();
    }

    private void OnDisable()
    {
        if (IsServer) MultiplayerManager.Instance.Game_ClientDisconnect -= Game_OnClientDisconnect;
    }

    private void Game_OnClientDisconnect(PlayerData disconnectedPlayerData)
    {
        PlayerBehaviour playerBehaviour = GetPlayerBehaviourFromRole(disconnectedPlayerData.role);
        if (playerBehaviour != null && playerBehaviour.isPlayerAlive.Value)
        {
            StartCoroutine(playerBehaviour.DisconnectionDeathCleanUp());
        }
    }

    private void DisableAllPlayers()
    {
        securityOfficeBehaviour.Disable();
        partsAndServiceBehaviour.Disable();
        backstagePlayerBehaviour.Disable();
    }

    private void InitialiseLocalPlayer()
    {
        PlayerBehaviour playerBehaviour = GetLocalPlayerBehaviour();

        if (playerBehaviour != default)
        {
            if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToPrivateChat();
            playerBehaviour.Initialise();
        }
        else
        {
            if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
            SpectatorUI.Instance.Show();
        }
    }

    public PlayerBehaviour GetLocalPlayerBehaviour()
    {
        return GetPlayerBehaviourFromRole(MultiplayerManager.Instance.GetLocalPlayerRole());
    }

    public PlayerBehaviour GetPlayerBehaviourFromRole(PlayerRoles playerRole)
    {
        switch (playerRole) // extend to other offices
        {
            case PlayerRoles.SecurityOffice:
                return securityOfficeBehaviour;
            case PlayerRoles.PartsAndService:
                return partsAndServiceBehaviour;
            case PlayerRoles.Backstage:
                return backstagePlayerBehaviour;
        }

        return default;
    }


    public IEnumerator EstablishOwnerships()
    {
        yield return WaitForObjectsToSpawn();

        foreach (PlayerData data in MultiplayerManager.Instance.playerDataList) // foreach player, their resective office is set to alive
        {
            switch (data.role) // extend to other offices
            {
                case PlayerRoles.SecurityOffice:
                    SetSecurityOfficeOwnerships(data.clientId);
                    break;
                case PlayerRoles.PartsAndService:
                    SetPartsAndServiceOwnerships(data.clientId);
                    break;
                case PlayerRoles.Backstage:
                    SetBackstageOwnerships(data.clientId);
                    break;
            }
        }
    }

    private void SetSecurityOfficeOwnerships(ulong clientId)
    {
        ChangeOwnership(securityOfficeBehaviour.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(securityOfficeBehaviour.leftDoor.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.leftDoor.doorLight.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.leftDoor.doorLight.doorLightButton.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.leftDoor.doorButton.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(securityOfficeBehaviour.rightDoor.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.rightDoor.doorLight.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.rightDoor.doorLight.doorLightButton.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.rightDoor.doorButton.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(securityOfficeBehaviour.playerComputer.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.playerComputer.playerCameraSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.playerComputer.playerCommunicationSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);
    }

    private void SetPartsAndServiceOwnerships(ulong clientId)
    {
        ChangeOwnership(partsAndServiceBehaviour.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(partsAndServiceBehaviour.door.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.door.doorLight.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.door.doorLight.doorLightButton.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.door.doorButton.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(partsAndServiceBehaviour.playerComputer.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.playerComputer.playerCameraSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.playerComputer.playerCommunicationSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.generator.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);
    }

    private void SetBackstageOwnerships(ulong clientId)
    {
        ChangeOwnership(backstagePlayerBehaviour.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.maintenance.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(backstagePlayerBehaviour.door.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.door.doorLight.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.door.doorLight.doorLightButton.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.door.doorButton.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(backstagePlayerBehaviour.playerComputer.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.playerComputer.playerCameraSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.playerComputer.playerCommunicationSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstagePlayerBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);
    }

    private IEnumerator WaitForObjectsToSpawn()
    {
        List<NetworkObject> networkObjects = FindObjectsByType<NetworkObject>(sortMode: FindObjectsSortMode.None).ToList();
        yield return new WaitUntil(() => networkObjects.All(obj => obj != null && obj.IsSpawned));
    }

    private void ChangeOwnership(NetworkObject networkObject, ulong targetClientId)
    {
        if (networkObject.OwnerClientId != targetClientId)
        {
            networkObject.ChangeOwnership(targetClientId);
            networkObject.DontDestroyWithOwner = true;
        }
    }

    public bool IsEveryoneDead()
    {
        if (securityOfficeBehaviour.isPlayerAlive.Value) return false;
        if (partsAndServiceBehaviour.isPlayerAlive.Value) return false;
        if (backstagePlayerBehaviour.isPlayerAlive.Value) return false;

        return true;
    }

    public bool IsPlayerDead(PlayerRoles playerRole)
    {
        PlayerBehaviour playerBehaviour = GetPlayerBehaviourFromRole(playerRole);

        if (playerBehaviour == default) return true;

        else return !playerBehaviour.isPlayerAlive.Value;
    }

    public bool IsPlayerDead(PlayerBehaviour playerBehaviour)
    {
        if (playerBehaviour == default) return true;

        else return !playerBehaviour.isPlayerAlive.Value;
    }

    public bool IsLocalPlayerAlive()
    {
        PlayerBehaviour playerBehaviour = GetLocalPlayerBehaviour();
        if (playerBehaviour == default) return false;

        else return playerBehaviour.isPlayerAlive.Value;
    }

    public bool IsPlayerVulnerable(Node currentnode, PlayerNode targetNode)
    {
        if (targetNode.playerBehaviour == securityOfficeBehaviour && securityOfficeBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        if (targetNode.playerBehaviour == partsAndServiceBehaviour && partsAndServiceBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        if (targetNode.playerBehaviour == backstagePlayerBehaviour && backstagePlayerBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        // expand for other players;
        return false;
    }
}
