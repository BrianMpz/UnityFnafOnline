using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSelectPlayer : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private Button leftOptionButton;
    [SerializeField] private Button rightOptionButton;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerRoleText;
    [SerializeField] private Image hostImage;

    [Header("Helpy Animations")]
    [SerializeField] private Image HelpySecurityOffice;
    [SerializeField] private Image HelpyPartsAndService;
    [SerializeField] private Image HelpyBackstage;

    [Header("Voice Chat")]
    public Image ChatStateImage;
    public EventTrigger MuteButton;
    public Sprite SpeakingImage;
    public Sprite NotSpeakingImage;
    [SerializeField] private Sprite MutedImage;
    public VivoxParticipant Participant;

    private void Start()
    {
        LobbyUI.Instance.AboutToStartGame += HideSelectButtons;
        LobbyUI.Instance.CancelToStartGame += ShowSelectButtons;

        leftOptionButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.ChangePlayerRole(GetPlayerDataFromPlayerIndex().role, next: false);
        });

        rightOptionButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.ChangePlayerRole(GetPlayerDataFromPlayerIndex().role, next: true);
        });

        MultiplayerManager.Instance.OnPlayerDataListChanged += MultiplayerManager_OnPLayerDataListChanged;
        UpdatePlayer();

        if (!MultiplayerManager.isPlayingOnline)
        {
            ChatStateImage.enabled = false;
        }

        AddListener(MuteButton, EventTriggerType.PointerClick, ToggleMute);
    }

    private void ToggleMute()
    {
        if (!MultiplayerManager.isPlayingOnline) return;
        if (Participant == null) return;

        PlayerData participantPlayerData = MultiplayerManager.Instance.GetPlayerDataFromVivoxId(Participant.PlayerId);
        PlayerData localPlayerData = MultiplayerManager.Instance.GetLocalPlayerData();
        if (participantPlayerData.role == localPlayerData.role) VivoxManager.Instance.ToggleMute();
    }

    private void MultiplayerManager_OnPLayerDataListChanged()
    {
        UpdatePlayer();
    }

    private void UpdatePlayer()
    {
        if (MultiplayerManager.Instance.IsPlayerIndexConnected(playerIndex))
        {
            PlayerData playerData = GetPlayerDataFromPlayerIndex();

            playerNameText.text = playerData.playerName.ToString();
            if (playerData.clientId == NetworkManager.Singleton.LocalClientId) playerNameText.color = Color.yellow;
            if (playerData.clientId == 0) hostImage.enabled = true; else hostImage.enabled = false;

            DisableAllHelpys();
            switch (playerData.role)
            {
                case PlayerRoles.SecurityOffice:
                    playerRoleText.text = "Security Office";
                    HelpySecurityOffice.enabled = true;
                    break;
                case PlayerRoles.PartsAndService:
                    playerRoleText.text = "Parts And Service";
                    HelpyPartsAndService.enabled = true;
                    break;
                case PlayerRoles.Backstage:
                    playerRoleText.text = "Back Stage";
                    HelpyBackstage.enabled = true;
                    break;
                default:
                    playerRoleText.text = "Spectator";
                    break;
            }

            Show();
        }
        else
        {
            Hide();
        }
    }

    private PlayerData GetPlayerDataFromPlayerIndex()
    {
        return MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
    }

    private void Show()
    {
        gameObject.SetActive(true);

        if (NetworkManager.Singleton.IsServer)
        {
            ShowSelectButtons();
        }
        else
        {
            HideSelectButtons();
        }

        StartCoroutine(GetSpeechData());
    }

    private IEnumerator GetSpeechData()
    {
        if (MultiplayerManager.isPlayingOnline)
        {
            PlayerData playerData = GetPlayerDataFromPlayerIndex();

            yield return new WaitUntil(() =>
            {
                List<VivoxParticipant> channel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.lobbyChatName);
                if (channel == null) return false;

                return channel.Any(participant => participant.PlayerId == playerData.vivoxId);
            });

            List<VivoxParticipant> channel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.lobbyChatName);
            Participant = channel.First(participant => participant.PlayerId == playerData.vivoxId);

            Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
            Participant.ParticipantSpeechDetected += UpdateChatStateImage;
        }

        UpdateChatStateImage();
    }

    private void ShowSelectButtons()
    {
        leftOptionButton.gameObject.SetActive(true);
        rightOptionButton.gameObject.SetActive(true);
    }

    private void HideSelectButtons()
    {
        leftOptionButton.gameObject.SetActive(false);
        rightOptionButton.gameObject.SetActive(false);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        HideSelectButtons();

        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged -= UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
        }
    }

    private void OnDestroy()
    {
        MultiplayerManager.Instance.OnPlayerDataListChanged -= MultiplayerManager_OnPLayerDataListChanged;

        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged -= UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
        }
    }

    private void DisableAllHelpys()
    {
        HelpySecurityOffice.enabled = false;
        HelpyPartsAndService.enabled = false;
        HelpyBackstage.enabled = false;
    }

    private void UpdateChatStateImage()
    {
        if (!MultiplayerManager.isPlayingOnline) return;
        if (ChatStateImage == null) return;


        if (VivoxService.Instance == null)
        {
            ChatStateImage.gameObject.SetActive(false);
            return;
        }
        else ChatStateImage.gameObject.SetActive(true);

        if (Participant.SpeechDetected)
        {
            ChatStateImage.sprite = SpeakingImage;
        }
        else
        {
            ChatStateImage.sprite = NotSpeakingImage;
        }

        if (Participant.IsMuted)
        {
            ChatStateImage.sprite = MutedImage;
        }
    }

    public void AddListener(EventTrigger eventTrigger, EventTriggerType triggerType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new() { eventID = triggerType };
        entry.callback.AddListener(_ => callback());
        eventTrigger.triggers.Add(entry);
    }
}
