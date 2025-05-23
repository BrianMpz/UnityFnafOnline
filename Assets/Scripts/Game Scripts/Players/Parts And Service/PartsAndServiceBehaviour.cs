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
    private bool isGettingJumpscared;

    [ClientRpc]
    public override void PlayDoorKnockAudioClientRpc(int indexOfCurrentNode, bool ferociousBanging)
    {
        // play sound with left panning

        if (!IsOwner) return;
        if (!isPlayerAlive.Value) return;

        string audioClip = ferociousBanging ? "ferocious banging" : "door knock";
        AudioSource knocking = GameAudioManager.Instance.PlaySfxInterruptable(audioClip, true);
        knocking.panStereo = -0.5f;
    }

    private protected override void UpdateCameraView()
    {
        cameraController.SetCameraView();
    }

    private protected override void UpdatePowerUsage()
    {
        currentPowerUsage.Value = 0;

        if (door.isDoorClosed.Value) currentPowerUsage.Value += 5;
        if (door.doorLight.isFlashingLight.Value) currentPowerUsage.Value++;

        if (playerComputer.isMonitorUp.Value) currentPowerUsage.Value++;

        if (Maintenance.Instance.powerGeneratorState.Value == State.ONLINE) currentPowerUsage.Value += 1f;

        if (PowerGenerator.Instance.isChargingSomeone.Value)
        {
            if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) currentPowerUsage.Value -= 5; // if charging yourself charge as normal
            else currentPowerUsage.Value += 5; // drain power if charging someone else
        }

        if (ultraPowerDrain.Value) currentPowerUsage.Value += 10;

        base.UpdatePowerUsage();
    }

    public override void PowerOn()
    {
        base.PowerOn();
        GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", false, 0.5f, true);
    }

    public override void PowerOff()
    {
        base.PowerOff();
    }

    public override void Update()
    {
        base.Update();

        if (!GameManager.Instance.isPlaying) return;

        RoomLight.enabled = isPlayerPoweredOn.Value && PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.PartsAndService);
        flashLight.enabled = !isPlayerPoweredOn.Value && PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.PartsAndService) || isGettingJumpscared;
    }

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        if (!isPlayerAlive.Value) yield break;
        isGettingJumpscared = true;

        flashLight.enabled = true;

        GameAudioManager.Instance.StopAllSfx();
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream, false);

        float elapsedTime = 0;

        while (elapsedTime < .7f)
        {
            cameraController.LerpTowardsDeathView();
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        GameAudioManager.Instance.StopSfx(audioSource);
        isGettingJumpscared = false;
    }

    public override IEnumerator WaitUntilKillConditionsAreMet(Node currentNode)
    {
        float forceDeathTime = Time.time + timeToWaitBeforeKill;

        AudioSource moaningNoDiddy = GameAudioManager.Instance.PlaySfxInterruptable("moan", true);
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

    public override void Initialise()
    {
        base.Initialise();

        if (!IsOwner) return;

        door.linkedNode.isOccupied.OnValueChanged += (prev, next) => CheckDoorNodeForFreddyEntry(door, next);
    }

    private void CheckDoorNodeForFreddyEntry(Door door, bool isNowOccupied)
    {
        if (!isPlayerAlive.Value || !isNowOccupied) return;

        Freddy freddy = AnimatronicManager.Instance.freddy;
        if (door.linkedNode.occupier == freddy)
        {
            float duration = freddy.currentMovementWaitTime.Value + 2f;
            Hallucinations.Instance.StartHallucination(duration);
        }
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

    public override bool IsAnimatronicCloseToAttack(Node currentNode)
    {
        if (currentNode == door.linkedNode) return true;

        return false;
    }

    public override bool CanGoldenFreddySpawnIn()
    {
        return partsAndServiceCameraController.currentView.Value != PartsAndServiceCameraController_View.GeneratorView;
    }

    public override bool HasSpottedGoldenFreddy()
    {
        return partsAndServiceCameraController.currentView.Value == PartsAndServiceCameraController_View.GeneratorView;
    }

    public override bool HasLookedAwayFromGoldenFreddy()
    {
        return partsAndServiceCameraController.currentView.Value != PartsAndServiceCameraController_View.GeneratorView;
    }

    public override bool HasBlockedFoxy()
    {
        return door.isDoorClosed.Value;
    }

    public override IEnumerator IsFoxyReadyToAttack(Node hallwayNode, float definitiveAttackTime)
    {
        yield return new WaitUntil(() => Time.time > definitiveAttackTime || GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(hallwayNode) || door.doorLight.isFlashingLight.Value);
    }
}
