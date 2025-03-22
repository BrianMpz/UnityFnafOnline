using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BackstagePlayerBehaviour : PlayerBehaviour
{
    [Header("Specialised Variables")]
    public Maintenance maintenance;
    public Door door;
    public Zap zap;
    [SerializeField] private Light RoomLight;
    private float zapCooldown;
    private int zapAttempts;

    public override bool IsPlayerVulnerable(Node currentNode)
    {
        if (currentNode != door.linkedNode) return false;

        if (door.isDoorClosed.Value)
        {
            return false;
        }
        return true;
    }

    private protected override void UpdateCameraView()
    {
        cameraController.SetCameraView();
    }

    private protected override void UpdatePowerUsage()
    {
        currentPowerUsage.Value = 0;

        if (door.isDoorClosed.Value) currentPowerUsage.Value += 2;
        if (door.doorLight.isFlashingLight.Value) currentPowerUsage.Value += 1;

        if (playerComputer.isMonitorUp.Value) currentPowerUsage.Value += 1f;

        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) currentPowerUsage.Value -= 4;

        base.UpdatePowerUsage();
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

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        if (!isPlayerAlive.Value) yield break;

        GameAudioManager.Instance.StopAllSfx();
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream);

        float elapsedTime = 0;

        while (elapsedTime < .7f)
        {
            cameraController.LerpTowardsDeathView();
            yield return null;
            elapsedTime += Time.deltaTime;
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

    public void Zap()
    {
        if (zapCooldown < 10)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }

        zapCooldown = 0;
        zapAttempts++;

        GameAudioManager.Instance.PlaySfxOneShot("controlled shock");

        ZapServerRpc(zapAttempts);

        MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ZapServerRpc(int zapAttempts)
    {
        currentPower.Value -= (zapAttempts * zapAttempts) / 2;
        zap.ZapAnimatronic();
    }

    public override void Update()
    {
        base.Update();

        zapCooldown += Time.deltaTime;
    }

    public override bool IsAnimatronicCloseToAttack(Node currentNode)
    {
        if (currentNode == door.linkedNode) return true;

        return false;
    }
}