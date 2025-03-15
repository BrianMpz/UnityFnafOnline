using System;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommunicatingPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerRoleText;

    [Header("Voice Chat")]
    [SerializeField] private Image ChatStateImage;
    [SerializeField] private Sprite SpeakingImage;
    [SerializeField] private Sprite NotSpeakingImage;
    [SerializeField] private Sprite MutedImage;
    public EventTrigger MuteButton;
    public VivoxParticipant Participant;

    [Header("Helpy Animations")]
    [SerializeField] private Image HelpySecurityOffice;
    [SerializeField] private Image HelpyPartsAndService;
    [SerializeField] private Image HelpyBackstage;
    [SerializeField] private Image HelpyJanitor;
    [SerializeField] private Image HelpySpectator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Hide();
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

    public void Show(VivoxParticipant vivoxParticipant, PlayerData playerData)
    {
        gameObject.SetActive(true);

        Participant = vivoxParticipant;
        playerNameText.text = playerData.playerName.ToString();
        playerRoleText.text = playerData.role.ToString();

        Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
        Participant.ParticipantSpeechDetected += UpdateChatStateImage;

        UpdateChatStateImage();

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
            case PlayerRoles.Janitor:
                playerRoleText.text = "Janitor";
                HelpyJanitor.enabled = true;
                break;
            default:
                playerRoleText.text = "Spectator";
                HelpySpectator.enabled = true;
                break;
        }
    }

    public void Hide()
    {
        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged -= UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
            Participant = null;
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Hide();
    }

    private void UpdateChatStateImage()
    {
        if (Participant == null) return;
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

    private void DisableAllHelpys()
    {
        HelpySecurityOffice.enabled = false;
        HelpyPartsAndService.enabled = false;
        HelpyBackstage.enabled = false;
        HelpyJanitor.enabled = false;
        HelpySpectator.enabled = false;
    }

    public void AddListener(EventTrigger eventTrigger, EventTriggerType triggerType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new() { eventID = triggerType };
        entry.callback.AddListener(_ => callback());
        eventTrigger.triggers.Add(entry);
    }
}
