using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMotionDetectionSystem : NetworkBehaviour
{
    [SerializeField] private TrackerNode[] trackerNodes;
    [SerializeField] private Canvas canvas;
    [HideInInspector] public TrackerButton currentTrackerButton;
    public event Action<TrackerButton> OnTrackerUpdate;
    public bool IsTracking { get => currentTrackerButton != null; }
    private Coroutine pulseCoroutine;

    public void Initialise(Camera playerCamera)
    {
        canvas.worldCamera = playerCamera;
        trackerNodes.ToList().ForEach(Node => Node.GetComponent<Image>().color = new(0, 0, 0, 0));
        Disable();
    }

    public void SetTracker(TrackerButton trackerButton)
    {
        currentTrackerButton = trackerButton;
        OnTrackerUpdate?.Invoke(currentTrackerButton);
    }

    private IEnumerator TrackerPulses()
    {
        while (true)
        {
            yield return new WaitForSeconds(2.5f);
            GameAudioManager.Instance.PlaySfxOneShot("camera blip");

            if (!IsTracking) continue;

            foreach (TrackerNode trackerNode in currentTrackerButton.encompassingNodes)
            {
                Node nodeData = AnimatronicManager.Instance.GetNodeFromName(trackerNode.nodeName);

                if (!nodeData.isOccupied.Value) continue;

                StartCoroutine(trackerNode.Blink());
                BlinkNodeServerRpc(trackerNodes.ToList().IndexOf(trackerNode));
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void BlinkNodeServerRpc(int indexOfNode, ServerRpcParams serverRpcParams = default)
    => BlinkNodeClientRpc(serverRpcParams.Receive.SenderClientId, indexOfNode);

    [ClientRpc]
    private void BlinkNodeClientRpc(ulong ignoreId, int indexOfNode)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        StartCoroutine(trackerNodes[indexOfNode].Blink());
    }

    public void Enable()
    {
        canvas.enabled = true;
        pulseCoroutine = StartCoroutine(TrackerPulses());
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

        SetTracker(null);

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableServerRpc(ServerRpcParams serverRpcParams = default)
    => DisableClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void DisableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        canvas.enabled = false;
        trackerNodes.ToList().ForEach(Node => Node.GetComponent<Image>().color = new(0, 0, 0, 0));
    }
}
