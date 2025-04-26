using System;
using TMPro;
using UnityEngine;

public class DebugUI : Singleton<DebugUI>
{
    public static bool CanDebug;
    public Action OnBuff;
    public Canvas nodeCanvas;
    public Canvas debugCanvas;
    [SerializeField] private TMP_Text estimatedXPText;
    [SerializeField] private TMP_Text playersAliveText;
    [SerializeField] private TMP_Text averageDifficultyText;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        nodeCanvas.enabled = true;
        debugCanvas.enabled = true;
    }

    public void Hide()
    {
        nodeCanvas.enabled = false;
        debugCanvas.enabled = false;
    }


    private void Update()
    {
        if (!CanDebug || !GameManager.Instance.isPlaying) return;

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha9) && PlayerRoleManager.Instance.IsLocalPlayerAlive())
        {
            GameManager.localPlayerBehaviour.HandleDeath("debug death");
            if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha8))
        {
            GameManager.Instance.currentGameTime.Value = GameManager.MaxGameLength;
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha7))
        {
            OnBuff?.Invoke();
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (nodeCanvas.enabled) Hide(); else Show();
        }

        estimatedXPText.text = $"Estimated XP: {GameManager.Instance.XpGained.Value}";
        playersAliveText.text = $"Players Alive: {PlayerRoleManager.Instance.CountPlayersAlive()}";
        averageDifficultyText.text = $"Average Animatronic Difficulty: {AnimatronicManager.Instance.GetAverageAnimatronicDifficulty()}";
    }
}
