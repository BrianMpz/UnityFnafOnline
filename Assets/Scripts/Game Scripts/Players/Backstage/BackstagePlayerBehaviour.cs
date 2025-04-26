using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BackstagePlayerBehaviour : PlayerBehaviour
{
    [Header("Specialised Variables")]
    [SerializeField] private BackstageCameraController backstageCameraController;
    [SerializeField] private Light RoomLight;
    public Maintenance maintenance;
    public Door door;
    public Zap zap;
    private float zapCooldown;
    private int zapAttempts;


    public override void Initialise()
    {
        base.Initialise();

        if (!IsOwner) return;

        StartCoroutine(HandleZapWarningAudio());

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

    private IEnumerator HandleZapWarningAudio()
    {
        AudioSource freddles = GameAudioManager.Instance.PlaySfxInterruptable("freddles", false, volume: 0f, loop: true);

        float volumeIncreasePerSecond = 0.015f; // Increase by 1.5% per second
        float currentVolume = 0f;

        while (freddles != null && isPlayerAlive.Value)
        {
            BackstageCameraController_View currentView = backstageCameraController.currentView.Value;
            BackstageCameraController_View shockView = BackstageCameraController_View.ShockView;

            if (currentView != shockView)
            {
                // If the player is not watching the animatronic, increase the volume constantly
                currentVolume += volumeIncreasePerSecond * Time.deltaTime * GetZapWarningAudioIncreaseCoefficient();
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                // If the player is looking at the animatronic, reset the volume
                currentVolume = 0f;
            }

            // Clamp the volume to prevent it from going over 1
            if (freddles != null) freddles.volume = Mathf.Clamp(currentVolume - 0.2f, 0f, 1f);

            yield return null;
        }
    }

    private float GetZapWarningAudioIncreaseCoefficient()
    {
        if (zap == null) return 1;

        return Mathf.Lerp(1, 4, zap.movementProgressValue.Value);
    }

    public override bool IsPlayerVulnerable(Node currentNode)
    {
        if (currentNode != door.linkedNode) return false;

        return !door.isDoorClosed.Value;
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

        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) currentPowerUsage.Value -= 5;

        if (ultraPowerDrain.Value) currentPowerUsage.Value += 10;

        base.UpdatePowerUsage();
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

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        if (!isPlayerAlive.Value) yield break;

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
    }

    public override void PowerOn()
    {
        base.PowerOn();

        GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", false, 0.5f, true);
    }

    public override void PowerOff()
    {
        base.PowerOff();

        playerComputer.DisableComputerSystem();
    }

    public override void Update()
    {
        base.Update();

        if (!GameManager.Instance.isPlaying) return;

        zapCooldown += Time.deltaTime;

        RoomLight.enabled = PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.Backstage);
        RoomLight.intensity = isPlayerPoweredOn.Value ? 4f : 0.3f;
    }

    public void Zap()
    {
        if (zapCooldown < 10 || !isPlayerPoweredOn.Value)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error", true);
            return;
        }

        zapCooldown = 0;
        zapAttempts++;

        ZapServerRpc(zapAttempts);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ZapServerRpc(int zapAttempts)
    {
        if (!isPlayerPoweredOn.Value) return;

        currentPower.Value -= zapAttempts * zapAttempts / 2;
        zap.GetZapped();
    }

    public override bool IsAnimatronicCloseToAttack(Node currentNode)
    {
        if (currentNode == door.linkedNode) return true;

        return false;
    }

    public override bool CanGoldenFreddySpawnIn()
    {
        return backstageCameraController.currentView.Value != BackstageCameraController_View.DoorView;
    }

    public override bool HasSpottedGoldenFreddy()
    {
        return backstageCameraController.currentView.Value == BackstageCameraController_View.DoorView;
    }

    public override bool HasLookedAwayFromGoldenFreddy()
    {
        return backstageCameraController.currentView.Value != BackstageCameraController_View.DoorView;
    }

    public override bool HasBlockedFoxy()
    {
        return door.isDoorClosed.Value;
    }

    public override IEnumerator IsFoxyReadyToAttack(Node hallwayNode, float definitiveAttackTime)
    {
        yield return new WaitUntil(() => !hallwayNode.isOccupied.Value && (Time.time > definitiveAttackTime || GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(hallwayNode)));
    }
}