using System;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommunicatingPlayer : MonoBehaviour
{
    public PlayerRoles playerRole;
    [SerializeField] private TMP_Text playerNameText;

    [Header("Voice Chat")]
    [SerializeField] private Image ChatStateImage;
    [SerializeField] private Sprite SpeakingImage;
    [SerializeField] private Sprite NotSpeakingImage;
    [SerializeField] private Sprite MutedImage;
    public EventTrigger MuteButton;
    [SerializeField] private VivoxParticipant Participant;

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

    public void AddListener(EventTrigger eventTrigger, EventTriggerType triggerType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new() { eventID = triggerType };
        entry.callback.AddListener(_ => callback());
        eventTrigger.triggers.Add(entry);
    }

    public void Show(VivoxParticipant vivoxParticipant, PlayerData playerData)
    {
        gameObject.SetActive(true);

        Participant = vivoxParticipant;
        playerNameText.text = playerData.playerName.ToString();

        Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
        Participant.ParticipantSpeechDetected += UpdateChatStateImage;

        UpdateChatStateImage();
    }

    public void Hide()
    {
        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
            Participant = null;
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
            Participant = null;
        }
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
}
