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
    [Header("UI Elements")]
    [SerializeField] private int playerIndex;
    [SerializeField] private Button leftOptionButton;
    [SerializeField] private Button rightOptionButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerRoleText;
    [SerializeField] private Image hostImage;

    [Header("Helpy Animations")]
    [SerializeField] private Image HelpySecurityOffice;
    [SerializeField] private Image HelpyPartsAndService;
    [SerializeField] private Image HelpyBackstage;
    [SerializeField] private Image HelpyJanitor;
    [SerializeField] private Image HelpySpectator;

    [Header("Voice Chat")]
    [SerializeField] private Image ChatStateImage;
    [SerializeField] private EventTrigger MuteButton;
    [SerializeField] private Sprite SpeakingImage;
    [SerializeField] private Sprite NotSpeakingImage;
    [SerializeField] private Sprite MutedImage;
    public VivoxParticipant Participant;

    [Header("Xp System")]
    [SerializeField] private TMP_Text currentXpLevelText;
    [SerializeField] private TMP_Text totalXpText;
    [SerializeField] private TMP_Text nextLevelXpText;
    [SerializeField] private Image xpProgressBar;


    private void Start()
    {
        LobbyUI.Instance.AboutToStartGame += HideHostOnlyButtons;
        LobbyUI.Instance.CancelToStartGame += ShowHostOnlyButtons;
        MultiplayerManager.Instance.OnPlayerDataListChanged += UpdatePlayer;

        leftOptionButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.ChangePlayerRole(playerIndex, next: false);
        });

        rightOptionButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.ChangePlayerRole(playerIndex, next: true);
        });

        kickButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.KickPlayer(playerIndex);
        });

        if (!MultiplayerManager.isPlayingOnline)
        {
            ChatStateImage.enabled = false;
        }

        UpdatePlayer();

        AddListener(MuteButton, EventTriggerType.PointerClick, ToggleMute);
    }

    private void ToggleMute()
    {
        if (!MultiplayerManager.isPlayingOnline) return;
        if (Participant == null) return;

        PlayerData participantPlayerData = MultiplayerManager.Instance.GetPlayerDataFromVivoxId(Participant.PlayerId);
        PlayerData localPlayerData = MultiplayerManager.Instance.GetLocalPlayerData();
        if (participantPlayerData.vivoxID == localPlayerData.vivoxID) VivoxManager.Instance.ToggleMute();
    }

    private void UpdatePlayer()
    {
        if (MultiplayerManager.Instance.IsPlayerIndexConnected(playerIndex))
        {
            PlayerData playerData = GetPlayerDataFromPlayerIndex();

            playerNameText.text = playerData.playerName.ToString();

            if (playerData.clientId == NetworkManager.Singleton.LocalClientId) playerNameText.color = Color.yellow;

            hostImage.enabled = playerData.clientId == 0;
            kickButton.gameObject.SetActive(playerData.clientId == 0);

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
                    playerRoleText.text = "Janitor's Closet";
                    HelpyJanitor.enabled = true;
                    break;
                default:
                    playerRoleText.text = "Spectator (dead)";
                    HelpySpectator.enabled = true;
                    break;
            }

            uint experience = playerData.experience;
            uint currentLevel = XPManager.GetLevelFromXp(experience);

            currentXpLevelText.text = currentLevel.ToString();
            totalXpText.text = experience.ToString() + "XP";
            nextLevelXpText.text = XPManager.GetTotalXpForLevel(currentLevel + 1).ToString() + "XP";
            xpProgressBar.fillAmount = XPManager.GetLevelProgress(experience);

            Show();
        }
        else Hide();
    }

    private void Update()
    {
        if (!DebugUI.CanDebug) return;

        if (Input.GetKey(KeyCode.C))
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ForceSetXp(XPManager.MaxXp); // Give max XP
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ForceSetXp(0); // Reset XP
            }
        }
    }

    private void ForceSetXp(uint xp)
    {
        PlayerData playerData = GetPlayerDataFromPlayerIndex();

        // Optional: limit to host (clientId == 0) if needed
        if (playerData.clientId != 0) return;

        MultiplayerManager.Instance.SetPlayerExperience(xp);
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
            ShowHostOnlyButtons();
        }
        else
        {
            HideHostOnlyButtons();
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

                return channel.Any(participant => participant.PlayerId == playerData.vivoxID);
            });

            List<VivoxParticipant> channel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.lobbyChatName);
            Participant = channel.First(participant => participant.PlayerId == playerData.vivoxID);

            Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
            Participant.ParticipantSpeechDetected += UpdateChatStateImage;
        }

        UpdateChatStateImage();
    }

    private void ShowHostOnlyButtons()
    {
        leftOptionButton.gameObject.SetActive(true);
        rightOptionButton.gameObject.SetActive(true);

        if (!MultiplayerManager.Instance.IsPlayerIndexConnected(playerIndex)) return;

        PlayerData playerData = GetPlayerDataFromPlayerIndex();
        kickButton.gameObject.SetActive(playerData.clientId != 0);
    }

    private void HideHostOnlyButtons()
    {
        leftOptionButton.gameObject.SetActive(false);
        rightOptionButton.gameObject.SetActive(false);
        kickButton.gameObject.SetActive(false);
    }

    private void Hide()
    {
        gameObject.SetActive(false);

        if (Participant != null)
        {
            Participant.ParticipantMuteStateChanged -= UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
        }
    }

    private void OnDestroy()
    {
        MultiplayerManager.Instance.OnPlayerDataListChanged -= UpdatePlayer;

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
        HelpyJanitor.enabled = false;
        HelpySpectator.enabled = false;
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
