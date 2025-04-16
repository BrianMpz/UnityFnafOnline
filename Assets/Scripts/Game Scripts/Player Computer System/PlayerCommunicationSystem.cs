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
    [SerializeField] private PlayerComputer playerComputer;
    [SerializeField] private Canvas joinCommsCanvas;
    [SerializeField] private Canvas commsCanvas;
    [SerializeField] private Canvas spectatorCanvas;
    [SerializeField] private Button joinCommsButton;
    [SerializeField] private Button callAllPlayersButton;
    [SerializeField] private Button leaveCommsButton;
    [SerializeField] private TMP_Text CommsDownText;

    [SerializeField] private List<CommunicatingPlayer> communicatingPlayerList;
    [SerializeField] private Image callImage;
    private AudioSource callAudio;
    public bool isConnectedToCall;

    private void Start()
    {
        if (MultiplayerManager.isPlayingOnline)
        {
            playerComputer.playerBehaviour.OnPowerDown += () => OnCallLeave(false);
            callAllPlayersButton.onClick.AddListener(() => StartCoroutine(CallAllPlayers()));
            leaveCommsButton.onClick.AddListener(() => OnCallLeave(false));
            joinCommsButton.onClick.AddListener(OnJoiningCall);

            communicatingPlayerList.ForEach(player => player.Hide());

            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantJoined;
            Maintenance.Instance.communicationsState.OnValueChanged += CommunicationStateChanged;

            StartCoroutine(PerpetuallyUpdateCommunicatingPlayers());
        }

        callImage.color = new(1, 1, 1, 0);
        CommsDownText.enabled = false;
    }

    private void CommunicationStateChanged(State _, State newMaintenanceState)
    {
        if (isConnectedToCall && newMaintenanceState != State.ONLINE) StartCoroutine(RandomlyDisconnect());
    }

    private IEnumerator RandomlyDisconnect()
    {
        while (Maintenance.Instance.communicationsState.Value != State.ONLINE)
        {
            yield return new WaitForSeconds(1f);

            // 10% chance per second to drop call
            if (UnityEngine.Random.Range(0, 10) == 0)
            {
                OnCallLeave(true);
                yield break;
            }
        }
    }

    public void Initialise(Camera playerCamera)
    {
        commsCanvas.worldCamera = playerCamera;
        joinCommsCanvas.worldCamera = playerCamera;
        spectatorCanvas.worldCamera = playerCamera;
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
            yield return new WaitForSeconds(0.2f);
            UpdateCommunicatingPlayers();
        }
    }

    private void UpdateCommunicatingPlayers()
    {
        List<VivoxParticipant> currentChannel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName);

        if (currentChannel == null) return;

        communicatingPlayerList.ForEach(player => player.Hide());

        foreach (VivoxParticipant participant in currentChannel)
        {
            PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromVivoxId(participant.PlayerId);

            communicatingPlayerList.First(player => player.Participant == null).Show(participant, playerData);
        }
    }

    private void OnParticipantJoined(VivoxParticipant vivoxParticipant)
    {
        if (VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName) == null) return;
        GameAudioManager.Instance.StopSfx(callAudio);
        GameAudioManager.Instance.PlaySfxOneShot("call pick up", true);
    }

    private IEnumerator CommsOffline()
    {
        CommsDownText.enabled = true;
        yield return new WaitForSeconds(2);
        CommsDownText.enabled = false;
    }

    private void OnJoiningCall()
    {
        if (!IsOwner) return;

        if (Maintenance.Instance.communicationsState.Value != State.ONLINE)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error", true);
            StartCoroutine(CommsOffline());
            return;
        }

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

        isConnectedToCall = true;
        callAllPlayersButton.enabled = true;

        if (playerComputer.currentComputerScreen.Value != ComputerScreen.Comms || !playerComputer.isMonitorUp.Value) return;

        commsCanvas.enabled = true;
        joinCommsCanvas.enabled = false;
    }

    private void OnCallLeave(bool systemDown = false)
    {
        if (!IsOwner) return;
        if (!isConnectedToCall) return;

        isConnectedToCall = false;
        joinCommsButton.enabled = false;

        string joinLeaveText = systemDown ? "Connection Lost..." : "Leaving Call...";
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = joinLeaveText;

        VivoxManager.Instance.SwitchToPrivateChat();

        StartCoroutine(WaitForCallLeave());

        if (playerComputer.currentComputerScreen.Value != ComputerScreen.Comms || !playerComputer.isMonitorUp.Value) return;

        commsCanvas.enabled = false;
        joinCommsCanvas.enabled = true;
    }

    private IEnumerator WaitForCallLeave()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(1, 10));
        yield return new WaitUntil(() => { return !VivoxManager.Instance.IsInChannel(VivoxManager.Instance.gameChatName); });
        joinCommsButton.enabled = true;
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = "Join Call";
    }

    public void Enable()
    {
        if (!IsOwner) return;

        commsCanvas.enabled = isConnectedToCall;
        joinCommsCanvas.enabled = !isConnectedToCall;
        spectatorCanvas.enabled = false;

        EnableServerRpc(); // for spectators
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableServerRpc(ServerRpcParams serverRpcParams = default)
    => EnableClientRpc(serverRpcParams.Receive.SenderClientId);


    public void Disable()
    {
        if (!IsOwner) return;

        joinCommsCanvas.enabled = false;
        commsCanvas.enabled = false;
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = "Join Call";

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
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = "Join Call";
    }

    [ServerRpc(RequireOwnership = false)]
    private void CallAllPlayersServerRpc(ServerRpcParams serverRpcParams = default)
    => CallPlayerClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void CallPlayerClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        if (SpectatorUI.Instance.isSpectating) return;
        if (isConnectedToCall) return;

        StartCoroutine(PlayCallAudio());
    }

    private IEnumerator PlayCallAudio()
    {
        callImage.color = new(1, 1, 1, 1);
        callAudio = GameAudioManager.Instance.PlaySfxInterruptable("calling", true);

        yield return new WaitForSeconds(2.4f);

        GameAudioManager.Instance.StopSfx(callAudio);
        callImage.color = new(1, 1, 1, 0);
    }
}
