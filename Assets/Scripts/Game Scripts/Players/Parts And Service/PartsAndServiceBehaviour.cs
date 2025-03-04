using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PartsAndServiceBehaviour : PlayerBehaviour
{
    [Header("Specialised Variables")]
    public PartsAndServiceCameraController partsAndServiceCameraController;
    public PowerGenerator generator;
    public Door door;
    [SerializeField] private Light RoomLight;
    [SerializeField] private Light flashLight;
    [SerializeField] private float timeToWaitBeforeKill;

    [ClientRpc]
    public override void PlayDoorKnockAudioClientRpc(int indexOfCurrentNode, ClientRpcParams _)
    {
        // play sound with left panning
        AudioSource knocking = GameAudioManager.Instance.PlaySfxInterruptable("door knock");
        knocking.panStereo = -0.5f;
    }

    private protected override void UpdateCameraView()
    {
        cameraController.SetCameraView();
    }

    private protected override void UpdatePowerUsage()
    {
        currentPowerUsage.Value = 0;

        if (door.isDoorClosed.Value) currentPowerUsage.Value += 4;
        if (door.doorLight.isFlashingLight.Value) currentPowerUsage.Value++;

        if (playerComputer.isMonitorUp.Value) currentPowerUsage.Value++;

        if (Maintenance.Instance.powerGeneratorState.Value == State.ONLINE) currentPowerUsage.Value += 1f;

        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) currentPowerUsage.Value -= 4;
    }

    public override void PowerOn()
    {
        base.PowerOn();
        RoomLight.enabled = true;
        flashLight.enabled = false;
        PowerOnServerRpc();
        GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", 0.5f, true);
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

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        if (!isPlayerAlive.Value) yield break;

        flashLight.enabled = true;

        GameAudioManager.Instance.StopAllSfx();
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

    public override IEnumerator WaitUntilKillConditionsAreMet(Node currentNode)
    {
        float forceDeathTime = Time.time + timeToWaitBeforeKill;

        AudioSource moaningNoDiddy = GameAudioManager.Instance.PlaySfxInterruptable("moan");
        moaningNoDiddy.panStereo = -0.5f;

        if (playerComputer.isMonitorUp.Value)
        {
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || !playerComputer.isMonitorUp.Value || !isPlayerAlive.Value;
            });

            yield break;
        }
        else playerComputer.Lock();
    }

    public override bool IsPlayerVulnerable(Node currentNode)
    {
        if (currentNode != door.linkedNode) return false;

        if (door.isDoorClosed.Value)
        {
            return false;
        }
        return true;
    }
}
