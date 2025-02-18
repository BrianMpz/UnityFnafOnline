using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class BackstagePlayerBehaviour : PlayerBehaviour
{
    public Maintenance maintenance;
    public BackstageCameraController cameraController;
    public Door door;
    [SerializeField] private Light RoomLight;
    [SerializeField] private float timeToWaitBeforeKill;
    private int zapAttempts;
    private float zapCooldown;
    public Zap zap;
    public Action OnZap;

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

    public override void SetCameraView()
    {
        cameraController.SetCameraView();
    }

    public override void SetUsage()
    {
        powerUsage.Value = 1;

        if (door.isDoorClosed.Value) powerUsage.Value += 2;
        if (door.doorLight.isFlashingLight.Value) powerUsage.Value += 1;

        if (playerComputer.playerCommunicationSystem.isConnected) powerUsage.Value += 1f;

        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) powerUsage.Value -= 4;
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

    private protected override IEnumerator DeathAnimation(string deathScream)
    {
        if (!isAlive.Value) yield break;

        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream);
        float elapedTime = 0;

        while (elapedTime < .6f)
        {
            cameraController.LerpTowardsDeathView();
            yield return null;
            elapedTime += Time.deltaTime;
        }
        GameAudioManager.Instance.StopSfx(audioSource);
    }

    public override void PowerOn()
    {
        base.PowerOn();

        playerComputer.EnableComputerSystem();

        RoomLight.intensity = 4;

        PowerOnServerRpc();

        AudioSource ambiance = GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", 0.5f, true);
    }
    [ServerRpc(RequireOwnership = false)]
    private void PowerOnServerRpc(ServerRpcParams serverRpcParams = default) => PowerOnClientRpc(serverRpcParams.Receive.SenderClientId);
    [ClientRpc]
    private void PowerOnClientRpc(ulong ignoreId)
    { if (NetworkManager.Singleton.LocalClientId == ignoreId) return; RoomLight.intensity = 4; }

    public override void PowerOff()
    {
        base.PowerOff();

        playerComputer.DisableComputerSystem();

        RoomLight.intensity = 0.3f;

        PowerOffServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void PowerOffServerRpc(ServerRpcParams serverRpcParams = default) => PowerOffClientRpc(serverRpcParams.Receive.SenderClientId);
    [ClientRpc]
    private void PowerOffClientRpc(ulong ignoreId)
    { if (NetworkManager.Singleton.LocalClientId == ignoreId) return; RoomLight.intensity = 0.3f; }


    [ClientRpc]
    public override void KnockOnDoorClientRpc(int indexOfCurrentNode, ClientRpcParams clientRpcParams)
    {
        power.Value -= 1;
        AudioSource knocking = GameAudioManager.Instance.PlaySfxInterruptable("door knock");
    }

    public void Zap()
    {
        if (zapCooldown < 10)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }
        zapCooldown = 0;
        zapAttempts++;
        OnZap?.Invoke();

        GameAudioManager.Instance.PlaySfxOneShot("controlled shock");
        power.Value -= zapAttempts * zapAttempts / 2;
        ZapServerRpc();
        MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ZapServerRpc()
    {
        zap.ZapAnimatronic();
    }

    public override void Update()
    {
        base.Update();

        zapCooldown += Time.deltaTime;
    }
}
