using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAudioLureSystem : NetworkBehaviour
{
    [SerializeField] AudioClip[] audioClips;
    [SerializeField] private Canvas canvas;
    [SerializeField] private bool isPlayingLure;
    [SerializeField] private float lureDuration;
    public Action<NodeName, float> OnLurePlayed;

    public void Initialise(Camera playerCamera)
    {
        isPlayingLure = false;
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

    [ServerRpc]
    public void PlayLureServerRpc(NodeName nodeName)
    {
        if (!isPlayingLure) StartCoroutine(PlayLure(nodeName));
    }

    private IEnumerator PlayLure(NodeName nodeName)
    {
        isPlayingLure = true;
        PlayLureClientRpc(nodeName, lureDuration);
        yield return new WaitForSeconds(lureDuration);
        isPlayingLure = false;
    }

    [ClientRpc]
    public void PlayLureClientRpc(NodeName nodeName, float lureDuration)
    {
        OnLurePlayed?.Invoke(nodeName, lureDuration);
        AnimatronicManager.Instance.PlayAudioLure(nodeName, audioClips[UnityEngine.Random.Range(0, audioClips.Length)]);
    }
}
