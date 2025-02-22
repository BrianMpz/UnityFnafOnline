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
    [SerializeField] private TMP_Text CommsDownText;

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
            playerBehaviour.OnPowerDown += () => { OnCallLeave(false); };
            leaveCommsButton.onClick.AddListener(() => { OnCallLeave(false); });
            joinCommsButton.onClick.AddListener(OnJoiningCall);
            callAllPlayersButton.onClick.AddListener(() => { StartCoroutine(CallAllPlayers()); });

            communicatingPlayerList.ForEach(spectatingPlayer => spectatingPlayer.Hide());
            StartCoroutine(PerpetuallyUpdateCommunicatingPlayers());
            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantJoined;
        }
        callImage.SetActive(false);

        Maintenance.Instance.communicationsState.OnValueChanged += CommunicationStateChanged;
        CommsDownText.enabled = false;
    }

    private void CommunicationStateChanged(State previousValue, State newValue)
    {
        if (newValue == State.ONLINE) return;
        if (!isConnected) return;

        OnCallLeave(true);
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

            communicatingPlayerList.First(communicatingPlayer => communicatingPlayer.playerRole == playerData.role).Show(participant, playerData);
        }
    }

    private void OnParticipantJoined(VivoxParticipant vivoxParticipant)
    {
        if (VivoxManager.Instance.GetChannel(VivoxManager.Instance.gameChatName) == null) return;
        GameAudioManager.Instance.StopSfx(callAudio);
        GameAudioManager.Instance.PlaySfxOneShot("call pick up");
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
            GameAudioManager.Instance.PlaySfxOneShot("button error");
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

        isConnected = true;

        callAllPlayersButton.enabled = true;

        if (!isOnComms) return;

        commsCanvas.enabled = isConnected;
        joinCommsCanvas.enabled = !isConnected;
    }

    private void OnCallLeave(bool systemDown = false)
    {
        if (!IsOwner) return;
        if (!isConnected) return;

        isConnected = false;

        joinCommsButton.enabled = false;

        string joinLeaveText = systemDown ? "Connection Lost..." : "Leaving Call...";
        joinCommsButton.GetComponentInChildren<TMP_Text>().text = joinLeaveText;

        VivoxManager.Instance.SwitchToPrivateChat();

        StartCoroutine(WaitForCallLeave());

        if (!isOnComms) return;

        commsCanvas.enabled = isConnected;
        joinCommsCanvas.enabled = !isConnected;
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
        if (SpectatorUI.Instance.isSpectating) return;

        StartCoroutine(PlayCallAudio());
    }

    private IEnumerator PlayCallAudio()
    {
        callImage.SetActive(true);
        callAudio = GameAudioManager.Instance.PlaySfxInterruptable("calling");

        yield return new WaitForSeconds(2.4f);

        GameAudioManager.Instance.StopSfx(callAudio);
        callImage.SetActive(false);
    }
}
