using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManual : NetworkBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private NetworkVariable<float> scrollValue = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private Button audioToggleButton;
    private AudioSource manualAudio;

    public void Initialise(Camera playerCamera)
    {
        audioToggleButton.onClick.AddListener(ToggleAudio);
        canvas.worldCamera = playerCamera;
        Disable();
    }

    private void ToggleAudio()
    {
        if (manualAudio == null)
        {
            manualAudio = GameAudioManager.Instance.PlaySfxInterruptable("manual", 1f, true);
            return;
        }

        if (manualAudio.isPlaying)
        {
            manualAudio.Pause();
        }
        else
        {
            if (manualAudio.time > 0) // If it's paused, resume
            {
                manualAudio.UnPause();
            }
            else // If it was completely stopped, restart
            {
                manualAudio.Play();
            }
        }
    }

    public void Enable()
    {
        canvas.enabled = true;
        EnableServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableServerRpc(ServerRpcParams serverRpcParams = default)
    => EnableClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void EnableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        canvas.enabled = true;
    }

    public void Disable()
    {
        canvas.enabled = false;
        DisableServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableServerRpc(ServerRpcParams serverRpcParams = default)
    => DisableClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void DisableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        canvas.enabled = false;
    }

    private void Update()
    {
        if (IsOwner) scrollValue.Value = scrollbar.value;
        else scrollbar.value = scrollValue.Value;
    }
}

