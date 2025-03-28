using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class SpectatorUI : Singleton<SpectatorUI>
{
    public bool isSpectating;
    private bool isSpectatingAPlayer;
    public Canvas canvas;
    [SerializeField] private CameraStatic cameraStatic;
    [SerializeField] private Button previousPlayerButton;
    [SerializeField] private Button nextPlayerButton;
    [SerializeField] private TMP_Text currentPlayerSpectatingText;
    [SerializeField] private TMP_Text hourText;
    [SerializeField] private TMP_Text timeLeftText;
    [SerializeField] private TMP_Text currentPlayerPowerText;
    [SerializeField] private protected TMP_Text nightText;
    [SerializeField] private List<SpectatingPlayer> spectatingPlayerList;
    private List<PlayerRoles> playerList;
    private int currentPlayerSpectatingIndex;



    private void Start()
    {
        currentPlayerPowerText.text = "";
        previousPlayerButton.onClick.AddListener(PreviousPlayer);
        nextPlayerButton.onClick.AddListener(NextPlayer);
        GameManager.Instance.OnGameOver += Hide;
        GameManager.Instance.OnGameWin += Hide;
        GameManager.Instance.currentHour.OnValueChanged += UpdateGameTimeText;
        GameManager.Instance.OnGameStarted += SetNightText;

        if (MultiplayerManager.isPlayingOnline) StartCoroutine(PerpetuallyUpdateCommunicatingPlayers());

        currentPlayerSpectatingIndex = 0;

        CreatePlayerList();

        Hide();
    }

    private void CreatePlayerList()
    {
        playerList = new();

        foreach (PlayerData playerData in MultiplayerManager.Instance.playerDataList)
        {
            PlayerBehaviour playerBehaviour = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerData.role);
            if (playerBehaviour == default || playerBehaviour == GameManager.localPlayerBehaviour) continue;

            else playerList.Add(playerData.role);
        }
    }

    void Update()
    {
        UpdateTimeLeft();

        if (!isSpectatingAPlayer) return;
        if (!isSpectating) return;

        PlayerBehaviour currentPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerList[currentPlayerSpectatingIndex]);
        if (currentPlayer == default) return;

        UpdatePlayerPower(currentPlayer);
        HandleGoldenFreddyVisibility(currentPlayer);
    }

    private void HandleGoldenFreddyVisibility(PlayerBehaviour currentPlayer)
    {
        if (currentPlayer.isDyingToGoldenFreddy.Value && !currentPlayer.isPlayerAlive.Value) GFJumpscareImage.Instance.Show(); else GFJumpscareImage.Instance.Hide();
    }

    private void UpdateTimeLeft()
    {
        float timeLeft = Mathf.Max(359.9f - GameManager.Instance.currentGameTime.Value, 0);
        timeLeftText.text = $"{timeLeft:F1}";
    }

    private bool UpdatePlayerPower(PlayerBehaviour currentPlayer)
    {

        if (!PlayerRoleManager.Instance.IsPlayerDead(currentPlayer))
            currentPlayerPowerText.text = $"Player Power: {Mathf.Max(currentPlayer.currentPower.Value, 0):F1}%";
        else
            currentPlayerPowerText.text = "";
        return true;
    }

    private void UpdateGameTimeText(int previousHour, int currentHour)
    {
        hourText.text = $"{currentHour}AM";
    }

    private void SetNightText()
    {
        nightText.text = $"Night {GameManager.Instance.gameNight}";
    }

    private IEnumerator PerpetuallyUpdateCommunicatingPlayers()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            UpdateCommunicatingPlayers();
        }
    }

    private void UpdateCommunicatingPlayers()
    {
        List<VivoxParticipant> currentChannel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.lobbyChatName);

        if (currentChannel == null) return;

        spectatingPlayerList.ForEach(spectatingPlayer => spectatingPlayer.Hide());

        foreach (VivoxParticipant participant in currentChannel)
        {
            PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromVivoxId(participant.PlayerId);

            spectatingPlayerList.First(spectatingPlayer => spectatingPlayer.Participant == null).Show(participant, playerData);
        }
    }

    public void NextPlayer()
    {
        currentPlayerSpectatingIndex = (currentPlayerSpectatingIndex + 1) % playerList.Count;

        UpdateSpectator(currentPlayerSpectatingIndex);
    }

    public void PreviousPlayer()
    {
        currentPlayerSpectatingIndex = (currentPlayerSpectatingIndex - 1 + playerList.Count) % playerList.Count;

        UpdateSpectator(currentPlayerSpectatingIndex);
    }

    private void UpdateSpectator(int index)
    {
        isSpectatingAPlayer = true;
        StopSpectating(currentPlayerSpectatingIndex);
        currentPlayerSpectatingIndex = index;

        PlayerRoles playerRole = playerList[index];

        if (PlayerRoleManager.Instance.IsPlayerDead(playerRole))
        {
            cameraStatic.RefreshMonitorStatic(true);
        }
        else
        {
            cameraStatic.RefreshMonitorStatic(false);
            SpectatePlayer(playerRole);
        }

        currentPlayerSpectatingText.text = MultiplayerManager.Instance.GetPlayerDataFromPlayerRole(playerRole).playerName.ToString();
    }

    private void SpectatePlayer(PlayerRoles playerRole)
    {
        if (!isSpectating) return;

        PlayerBehaviour currentPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole);

        currentPlayer.spectatorCamera.enabled = true;

        if (currentPlayer.playerComputer.isMonitorUp.Value)
        {
            CameraName currentCameraName = currentPlayer.playerComputer.playerCameraSystem.currentCameraName.Value;
            CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(currentCameraName);
            GlobalCameraSystem.Instance.EnableCameraComponent(cameraData);
        }

        currentPlayer.isPlayerAlive.OnValueChanged += CheckPlayerAliveStatus;
    }

    private void StopSpectating(int index)
    {
        if (!isSpectating) return;

        PlayerBehaviour currentPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerList[index]);
        if (currentPlayer == default) return;

        currentPlayer.spectatorCamera.enabled = false;

        GlobalCameraSystem.Instance.DisableAllCameraComponents();

        currentPlayer.isPlayerAlive.OnValueChanged -= CheckPlayerAliveStatus;
    }

    private void CheckPlayerAliveStatus(bool _, bool _1)
    {
        UpdateSpectator(currentPlayerSpectatingIndex);
    }

    public void Show()
    {
        DebugCanvasUI.Instance.Show();
        canvas.enabled = true;
        isSpectating = true;
        isSpectatingAPlayer = false;
        cameraStatic.staticaudio = GameAudioManager.Instance.PlaySfxInterruptable("static audio", 0, true);
        cameraStatic.RefreshMonitorStatic(true);
        currentPlayerSpectatingIndex = 0;
    }

    public void Hide()
    {
        DebugCanvasUI.Instance.Hide();
        canvas.enabled = false;
        isSpectating = false;
        GameAudioManager.Instance.StopSfx(cameraStatic.staticaudio);
        StopSpectating(currentPlayerSpectatingIndex);
    }

    public void RefreshSpectator()
    {
        if (!isSpectating) return;

        UpdateSpectator(currentPlayerSpectatingIndex);
    }

    public PlayerBehaviour GetCurrentSpectator()
    {
        return PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerList[currentPlayerSpectatingIndex]);
    }
}
