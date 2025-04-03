using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGameSystem : NetworkBehaviour
{
    [SerializeField] private Canvas canvas;
    public void Initialise(Camera playerCamera)
    {
        canvas.worldCamera = playerCamera;
        Disable();
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

    }
}

