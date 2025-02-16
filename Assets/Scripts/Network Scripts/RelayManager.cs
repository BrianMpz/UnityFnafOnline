using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : Singleton<RelayManager>
{
    private string joinCode;

    public async Task CreateRelayAsync(int maxPlayers)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections: maxPlayers);

            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocationId: allocation.AllocationId);

            RelayServerData relayServerData = new(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

        }
        catch (Exception)
        {
            MultiplayerManager.Instance.OnDisconnectedFromGame?.Invoke(true);
        }
    }

    public async Task JoinRelayAsync(string codeToJoin)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: codeToJoin);

            RelayServerData relayServerData = new(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

        }
        catch (Exception)
        {
            MultiplayerManager.Instance.OnDisconnectedFromGame?.Invoke(false);
        }
    }

    public string GetJoinCode()
    {
        return joinCode.ToUpper();
    }

}
