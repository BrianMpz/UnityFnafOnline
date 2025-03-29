using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages player roles, assigns ownership of objects, and handles disconnections.
/// </summary>
public class PlayerRoleManager : NetworkSingleton<PlayerRoleManager>
{
    public SecurityOfficeBehaviour securityOfficeBehaviour;
    public PartsAndServiceBehaviour partsAndServiceBehaviour;
    public BackstagePlayerBehaviour backstageBehaviour;
    public JanitorPlayerBehaviour janitorBehaviour;

    private void Start()
    {
        if (IsServer) MultiplayerManager.Instance.Game_ClientDisconnect += Game_OnClientDisconnect;
        GameManager.Instance.OnGameStarted += InitialiseLocalPlayer;
        GameManager.Instance.OnGameWin += DisableAllPlayers;
        GameManager.Instance.OnGameOver += DisableAllPlayers;

        DisableAllPlayers(); // the local player is turned on during run time

        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToPrivateChat();
    }

    private void OnDisable()
    {
        if (IsServer) MultiplayerManager.Instance.Game_ClientDisconnect -= Game_OnClientDisconnect;
    }

    private void Game_OnClientDisconnect(PlayerData disconnectedPlayerData) // only the server handles this project
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
        backstageBehaviour.Disable();
        janitorBehaviour.Disable();
        // expand for other players
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
            // if the player doesnt have a roles then spectate for the duration of the game
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
        return playerRole switch // extend to other offices
        {
            PlayerRoles.SecurityOffice => securityOfficeBehaviour,
            PlayerRoles.PartsAndService => partsAndServiceBehaviour,
            PlayerRoles.Backstage => backstageBehaviour,
            PlayerRoles.Janitor => janitorBehaviour,
            _ => default,
        };
    }


    public IEnumerator EstablishOwnerships()
    {
        yield return WaitForObjectsToSpawn();

        foreach (PlayerData data in MultiplayerManager.Instance.playerDataList) // for each player, their resective office is set to alive
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
                case PlayerRoles.Janitor:
                    SetJanitorOwnerships(data.clientId);
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
        ChangeOwnership(securityOfficeBehaviour.playerComputer.playerAudioLureSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.playerComputer.playerMotionDetectionSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.playerComputer.playerManual.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(securityOfficeBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(securityOfficeBehaviour.keypadSystem.GetComponent<NetworkObject>(), clientId);
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
        ChangeOwnership(partsAndServiceBehaviour.playerComputer.playerMotionDetectionSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.playerComputer.playerAudioLureSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(partsAndServiceBehaviour.playerComputer.playerManual.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(partsAndServiceBehaviour.generator.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(partsAndServiceBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);
    }

    private void SetBackstageOwnerships(ulong clientId)
    {
        ChangeOwnership(backstageBehaviour.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.maintenance.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(backstageBehaviour.door.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.door.doorLight.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.door.doorLight.doorLightButton.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.door.doorButton.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(backstageBehaviour.playerComputer.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.playerComputer.playerCameraSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.playerComputer.playerCommunicationSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.playerComputer.playerMotionDetectionSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.playerComputer.playerAudioLureSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(backstageBehaviour.playerComputer.playerManual.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(backstageBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);
    }

    private void SetJanitorOwnerships(ulong clientId)
    {
        ChangeOwnership(janitorBehaviour.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(janitorBehaviour.playerComputer.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(janitorBehaviour.playerComputer.playerCameraSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(janitorBehaviour.playerComputer.playerCommunicationSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(janitorBehaviour.playerComputer.playerMotionDetectionSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(janitorBehaviour.playerComputer.playerAudioLureSystem.GetComponent<NetworkObject>(), clientId);
        ChangeOwnership(janitorBehaviour.playerComputer.playerManual.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(janitorBehaviour.cameraController.GetComponent<NetworkObject>(), clientId);

        ChangeOwnership(janitorBehaviour.mask.GetComponent<NetworkObject>(), clientId);
    }

    private IEnumerator WaitForObjectsToSpawn()
    {
        // we wont be able to change ownership unhtil the objects spawn
        List<NetworkObject> networkObjects = FindObjectsByType<NetworkObject>(sortMode: FindObjectsSortMode.None).ToList();
        yield return new WaitUntil(() => networkObjects.All(obj => obj != null && obj.IsSpawned));
    }

    private void ChangeOwnership(NetworkObject networkObject, ulong targetClientId)
    {
        if (networkObject.OwnerClientId != targetClientId)
        {
            networkObject.ChangeOwnership(targetClientId);
            // we dont want to destroy with owner because we want the server to still conrtrol it
            networkObject.DontDestroyWithOwner = true;
        }
    }

    public bool IsEveryoneDead()
    {
        if (securityOfficeBehaviour.isPlayerAlive.Value) return false;
        if (partsAndServiceBehaviour.isPlayerAlive.Value) return false;
        if (backstageBehaviour.isPlayerAlive.Value) return false;
        if (janitorBehaviour.isPlayerAlive.Value) return false;

        return true;
    }

    public bool IsSpectatingPlayer(PlayerRoles playerRole)
    {
        PlayerBehaviour playerBehaviour = GetPlayerBehaviourFromRole(playerRole);
        return playerBehaviour.isPlayerAlive.Value && GameManager.Instance.IsSpectating && SpectatorUI.Instance.GetCurrentSpectator().playerRole == playerRole;
    }

    public bool IsControllingPlayer(PlayerRoles playerRole)
    {
        PlayerBehaviour playerBehaviour = GetPlayerBehaviourFromRole(playerRole);
        return playerBehaviour.isPlayerAlive.Value && GameManager.localPlayerBehaviour == playerBehaviour;
    }

    public bool IsSpectatingOrControllingPlayer(PlayerRoles playerRole)
    {
        return IsSpectatingPlayer(playerRole) || IsControllingPlayer(playerRole);
    }

    public bool IsPlayerDead(PlayerBehaviour playerBehaviour)
    {
        if (playerBehaviour == null) return true;

        else return !playerBehaviour.isPlayerAlive.Value;
    }

    public bool IsPlayerDead(PlayerRoles playerRole)
    {
        PlayerBehaviour playerBehaviour = GetPlayerBehaviourFromRole(playerRole);

        return IsPlayerDead(playerBehaviour);
    }

    public bool IsLocalPlayerAlive()
    {
        PlayerBehaviour playerBehaviour = GetLocalPlayerBehaviour();

        return !IsPlayerDead(playerBehaviour);
    }

    public bool IsPlayerVulnerableToAttack(Node currentnode, PlayerNode targetNode)
    {
        if (targetNode.playerBehaviour == securityOfficeBehaviour && securityOfficeBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        if (targetNode.playerBehaviour == partsAndServiceBehaviour && partsAndServiceBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        if (targetNode.playerBehaviour == backstageBehaviour && backstageBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        if (targetNode.playerBehaviour == janitorBehaviour && janitorBehaviour.IsPlayerVulnerable(currentnode))
        {
            return true;
        }

        // expand for other players;
        return false;
    }

    public bool IsAnimatronicAboutToAttack(Node currentNode)
    {
        if (securityOfficeBehaviour.IsAnimatronicCloseToAttack(currentNode)) return true;
        if (partsAndServiceBehaviour.IsAnimatronicCloseToAttack(currentNode)) return true;
        if (backstageBehaviour.IsAnimatronicCloseToAttack(currentNode)) return true;
        if (janitorBehaviour.IsAnimatronicCloseToAttack(currentNode)) return true;

        return false;
    }
}
