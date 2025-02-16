using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCommunicationSystem : NetworkBehaviour
{
    [SerializeField] private PlayerBehaviour playerBehaviour;

    [SerializeField] private Canvas joinCommsCanvas;
    [SerializeField] private Button joinCommsButton;

    [SerializeField] private Canvas commsCanvas;
    [SerializeField] private Button callAllPlayersButton;
    [SerializeField] private Button leaveCommsButton;

    [SerializeField] private Canvas spectatorCanvas;

    [SerializeField] private List<CommunicatingPlayer> communicatingPlayerList;
    private AudioSource callAudio;
    [SerializeField] private GameObject callImage;
    public bool isConnected;
    private bool isOnComms;

    private void Start()
    {
        if (MultiplayerManager.isPlayingOnline)
        {
            playerBehaviour.OnPowerDown += OnCallLeave;
            joinCommsButton.onClick.AddListener(OnJoiningCall);
            leaveCommsButton.onClick.AddListener(OnCallLeave);
            callAllPlayersButton.onClick.AddListener(() => { StartCoroutine(CallAllPlayers()); });

            communicatingPlayerList.ForEach(spectatingPlayer => spectatingPlayer.Hide());
            StartCoroutine(PerpetuallyUpdateCommunicatingPlayers());
            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantJoined;
        }
        callImage.SetActive(false);
    }

    public void Initialise()
    {
        Disable();
    }

    private IEnumerator CallAllPlayers()
    {
        CallAllPlayersServerRpc();
        callAllPlayersButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(3);
        callAllPlayersButton.gameObject.SetActive(true);
    }

    public override void OnDestroy()
    {
        if (MultiplayerManager.isPlayingOnline)
        {
            VivoxManager.Instance.ChannelJoined -= OnCallJoined;
        }
    }

    private IEnumerator PerpetuallyUpdateCommunicatingPlayers()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            UpdateCommunicatingPlayers();
        }
    }

    private void UpdateCommunicatingPlayers()
    {
        if (VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName) == null) return;

        List<VivoxParticipant> currentChannel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName);

        communicatingPlayerList.ForEach(communicatingPlayer => communicatingPlayer.Hide());

        foreach (VivoxParticipant participant in currentChannel)
        {
            PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromVivoxId(participant.PlayerId);

            Debug.Log($"{playerData.playerName} is {playerData.role}");

            communicatingPlayerList.First(communicatingPlayer => communicatingPlayer.playerRole == playerData.role).Show(participant, playerData);
        }
    }

    private void OnParticipantJoined(VivoxParticipant vivoxParticipant)
    {
        if (VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName) == null) return;
        GameAudioManager.Instance.StopSfx(callAudio);
        GameAudioManager.Instance.PlaySfxOneShot("call pick up");
    }

    private void OnJoiningCall()
    {
        if (!IsOwner) return;

        VivoxManager.Instance.SwitchToGameChat();
        VivoxManager.Instance.ChannelJoined += OnCallJoined;

        joinCommsButton.enabled = false;
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = "Joining...";
    }

    private void OnCallJoined(string roomName)
    {
        if (roomName != VivoxManager.Instance.gameChatName) return;
        if (!IsOwner) return;

        VivoxManager.Instance.ChannelJoined -= OnCallJoined;

        isConnected = true;

        callAllPlayersButton.enabled = true;

        if (!isOnComms) return;

        commsCanvas.enabled = isConnected;
        joinCommsCanvas.enabled = !isConnected;
    }

    private void OnCallLeave()
    {
        if (!IsOwner) return;
        if (!isConnected) return;

        isConnected = false;

        joinCommsButton.enabled = false;
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = "Leaving Call...";

        commsCanvas.enabled = isConnected;
        joinCommsCanvas.enabled = !isConnected;

        VivoxManager.Instance.SwitchToPrivateChat();

        StartCoroutine(WaitForCallLeave());

        if (!isOnComms) return;

        commsCanvas.enabled = isConnected;
        joinCommsCanvas.enabled = !isConnected;
    }

    private IEnumerator WaitForCallLeave()
    {
        yield return new WaitForSeconds(5);
        yield return new WaitUntil(() => { return !VivoxManager.Instance.IsInChannel(VivoxManager.Instance.gameChatName); });
        joinCommsButton.enabled = true;
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = "Join Call";
    }

    public void Enable()
    {
        if (!IsOwner) return;

        isOnComms = true;

        commsCanvas.enabled = isConnected;
        joinCommsCanvas.enabled = !isConnected;
        spectatorCanvas.enabled = false;

        EnableServerRpc(); // for spectators
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableServerRpc(ServerRpcParams serverRpcParams = default)
    => EnableClientRpc(serverRpcParams.Receive.SenderClientId);


    public void Disable()
    {
        if (!IsOwner) return;

        isOnComms = false;

        joinCommsCanvas.enabled = false;
        commsCanvas.enabled = false;

        DisableServerRpc(); // for spectators
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableServerRpc(ServerRpcParams serverRpcParams = default)
    => DisableClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void EnableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        commsCanvas.enabled = false;
        joinCommsCanvas.enabled = false;
        spectatorCanvas.enabled = true;
    }

    [ClientRpc]
    private void DisableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        commsCanvas.enabled = false;
        joinCommsCanvas.enabled = false;
        spectatorCanvas.enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CallAllPlayersServerRpc(ServerRpcParams serverRpcParams = default)
    => CallPlayerClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void CallPlayerClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        if (VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName) != null || SpectatorUI.Instance.isSpectating) return;

        callImage.SetActive(true);

        PlayCallAudioAsync();
    }

    private async void PlayCallAudioAsync()
    {
        callAudio = GameAudioManager.Instance.PlaySfxInterruptable("calling");

        await Task.Delay(TimeSpan.FromSeconds(2.4));

        GameAudioManager.Instance.StopSfx(callAudio);
        callImage.SetActive(false);
        Debug.Log("calling");
    }
}
