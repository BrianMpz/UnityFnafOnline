using Unity.Netcode;
using UnityEngine;

public class DoorButton : NetworkBehaviour
{
    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Door door;

    private void OnMouseDown()
    {
        PlayerComputer playerComputer = door.playerBehaviour.playerComputer;
        bool cantToggleDoor = playerComputer.isMonitorUp.Value && !playerComputer.isMonitorAlwaysUp;
        if (cantToggleDoor) return;

        if (IsOwner) door.ToggleDoor();
    }

    public void TurnOn()
    {
        GetComponent<Renderer>().material = onMaterial;
        TurnOnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TurnOnServerRpc(ServerRpcParams serverRpcParams = default)
        => TurnOnClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TurnOnClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        GetComponent<Renderer>().material = onMaterial;
    }

    public void TurnOff()
    {
        GetComponent<Renderer>().material = offMaterial;
        TurnOffServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TurnOffServerRpc(ServerRpcParams serverRpcParams = default)
        => TurnOffClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TurnOffClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        GetComponent<Renderer>().material = offMaterial;
    }
}
