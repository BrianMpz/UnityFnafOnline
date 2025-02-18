using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PartsAndServiceBehaviour : PlayerBehaviour
{
    public PartsAndServiceCameraController cameraController;
    public PowerGenerator generator;
    public Door door;
    [SerializeField] private Light RoomLight;
    [SerializeField] private Light flashLight;
    [SerializeField] private float timeToWaitBeforeKill;

    [ClientRpc]
    public override void KnockOnDoorClientRpc(int indexOfCurrentNode, ClientRpcParams clientRpcParams)
    {
        power.Value -= 1;
        AudioSource knocking = GameAudioManager.Instance.PlaySfxInterruptable("door knock");
        knocking.panStereo = -0.5f;
    }

    public override void SetCameraView()
    {
        cameraController.SetCameraView();
    }

    public override void SetUsage()
    {
        powerUsage.Value = 1;

        if (door.isDoorClosed.Value) powerUsage.Value += 4;
        if (door.doorLight.isFlashingLight.Value) powerUsage.Value++;

        if (playerComputer.isMonitorUp.Value) powerUsage.Value++;
        if (playerComputer.playerCommunicationSystem.isConnected) powerUsage.Value += 1f;

        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) powerUsage.Value -= 4;
    }

    public override void PowerOn()
    {
        base.PowerOn();
        RoomLight.enabled = true;
        flashLight.enabled = false;
        PowerOnServerRpc();
        AudioSource ambiance = GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", 0.5f, true);
    }
    [ServerRpc(RequireOwnership = false)]
    private void PowerOnServerRpc(ServerRpcParams serverRpcParams = default) => PowerOnClientRpc(serverRpcParams.Receive.SenderClientId);
    [ClientRpc]
    private void PowerOnClientRpc(ulong ignoreId)
    { if (NetworkManager.Singleton.LocalClientId == ignoreId) return; RoomLight.enabled = true; flashLight.enabled = false; }

    public override void PowerOff()
    {
        base.PowerOff();
        RoomLight.enabled = false;
        flashLight.enabled = true;
        PowerOffServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void PowerOffServerRpc(ServerRpcParams serverRpcParams = default) => PowerOffClientRpc(serverRpcParams.Receive.SenderClientId);
    [ClientRpc]
    private void PowerOffClientRpc(ulong ignoreId)
    { if (NetworkManager.Singleton.LocalClientId == ignoreId) return; RoomLight.enabled = false; flashLight.enabled = true; }

    private protected override IEnumerator DeathAnimation(string deathScream)
    {
        if (!isAlive.Value) yield break;

        flashLight.enabled = true;
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream);
        float elapedTime = 0;

        while (elapedTime < .7f)
        {
            cameraController.LerpTowardsDeathView();
            yield return null;
            elapedTime += Time.deltaTime;
        }
        GameAudioManager.Instance.StopSfx(audioSource);
    }

    public override IEnumerator WaitToKill(Node currentNode)
    {
        float forceDeathTime = Time.time + timeToWaitBeforeKill;

        AudioSource moaningNoDiddy = GameAudioManager.Instance.PlaySfxInterruptable("moan");
        moaningNoDiddy.panStereo = -0.5f;

        if (playerComputer.isMonitorUp.Value)
        {
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || !playerComputer.isMonitorUp.Value || !isAlive.Value;
            });

            yield break;
        }
        else playerComputer.Lock();
    }

    public override bool IsVulnerable(Node currentNode)
    {
        if (currentNode != door.linkedNode) return false;

        if (door.isDoorClosed.Value)
        {
            return false;
        }
        return true;
    }

    public override bool IsCameraUp()
    {
        return playerComputer.isMonitorUp.Value;
    }
}
