using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DebugCanvasUI : Singleton<DebugCanvasUI>
{
    public bool debug;
    public Canvas debugCanvas;
    [SerializeField] private Button leaveButton;
    public Action OnBuff;

    private void Awake()
    {
        leaveButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);
        Hide();
    }

    public void Show()
    {
        debugCanvas.enabled = true;
    }

    public void Hide()
    {
        debugCanvas.enabled = false;
    }


    private void Update()
    {
        if (!debug || !GameManager.Instance.isPlaying) return;

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha9) && PlayerRoleManager.Instance.IsLocalPlayerAlive())
        {
            GameManager.localPlayerBehaviour.HandleDeath("debug death");
            if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha8))
        {
            GameManager.Instance.EndGameClientRpc();
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha7))
        {
            OnBuff?.Invoke();
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (!GameManager.Instance.IsOwner && PlayerRoleManager.Instance.IsLocalPlayerAlive())
            {
                StartCoroutine(PlayerRoleManager.Instance.GetLocalPlayerBehaviour().Die("Golden Freddy"));
            }

            if (debugCanvas.enabled) Hide(); else Show();
        }
    }
}
