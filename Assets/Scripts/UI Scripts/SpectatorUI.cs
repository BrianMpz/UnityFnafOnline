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
        if (!isSpectatingAPlayer) return;
        if (!isSpectating) return;

        PlayerBehaviour playerBehaviour = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerList[currentPlayerSpectatingIndex]);
        if (playerBehaviour == default) return;

        if (!PlayerRoleManager.Instance.IsPlayerDead(playerBehaviour))
            currentPlayerPowerText.text = $"Player Power: {playerBehaviour.currentPower.Value:F1}%";
        else
            currentPlayerPowerText.text = "";

        float timeLeft = Mathf.Max(360 - GameManager.Instance.currentGameTime.Value, 0);
        timeLeftText.text = $"{timeLeft:F1}";
    }

    private void UpdateGameTimeText(int previousHour, int currentHour)
    {
        hourText.text = $"{currentHour}AM";
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
        if (VivoxManager.Instance.GetChannel(VivoxManager.Instance.lobbyChatName) == null) return;

        List<VivoxParticipant> currentChannel = VivoxManager.Instance.GetChannel(VivoxManager.Instance.lobbyChatName);

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

        PlayerBehaviour playerBehaviour = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole);

        playerBehaviour.spectatorCamera.enabled = true;

        if (playerBehaviour.playerComputer.isMonitorUp.Value)
        {
            CameraName currentCameraName = playerBehaviour.playerComputer.playerCameraSystem.currentCameraName.Value;
            CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(currentCameraName);
            GlobalCameraSystem.Instance.EnableCameraComponent(cameraData);
        }

        playerBehaviour.isPlayerAlive.OnValueChanged += CheckPlayerAliveStatus;
    }

    private void StopSpectating(int index)
    {
        if (!isSpectating) return;

        PlayerBehaviour playerBehaviour = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerList[index]);
        if (playerBehaviour == default) return;

        playerBehaviour.spectatorCamera.enabled = false;

        GlobalCameraSystem.Instance.DisableAllCameraComponents();

        playerBehaviour.isPlayerAlive.OnValueChanged -= CheckPlayerAliveStatus;
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
