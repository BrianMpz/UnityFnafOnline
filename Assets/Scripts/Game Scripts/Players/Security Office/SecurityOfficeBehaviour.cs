using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SecurityOfficeBehaviour : PlayerBehaviour
{
    [Header("Specialised Variables")]
    public Door leftDoor;
    public Door rightDoor;
    public KeypadSystem keypadSystem;
    [SerializeField] private Light RoomLight;
    [SerializeField] private Light flashLight;
    [SerializeField] private Node LeftDoorwayNode;
    [SerializeField] private Node RightDoorwayNode;
    private bool isGettingJumpscared;


    public override void Initialise()
    {
        base.Initialise();

        if (!IsOwner) return;

        leftDoor.linkedNode.isOccupied.OnValueChanged += (prev, next) => CheckDoorNodeForFreddyEntry(leftDoor, next);
        rightDoor.linkedNode.isOccupied.OnValueChanged += (prev, next) => CheckDoorNodeForFreddyEntry(rightDoor, next);
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

    private protected override void UpdateCameraView()
    {
        cameraController.SetCameraView();
    }

    private protected override void UpdatePowerUsage()
    {
        currentPowerUsage.Value = 1;

        if (leftDoor.isDoorClosed.Value) currentPowerUsage.Value += 1.5f;
        if (leftDoor.doorLight.isFlashingLight.Value) currentPowerUsage.Value++;

        if (rightDoor.isDoorClosed.Value) currentPowerUsage.Value += 1.5f;
        if (rightDoor.doorLight.isFlashingLight.Value) currentPowerUsage.Value++;

        if (playerComputer.isMonitorUp.Value) currentPowerUsage.Value++;
        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) currentPowerUsage.Value -= 5;

        if (ultraPowerDrain.Value) currentPowerUsage.Value += 10;

        base.UpdatePowerUsage();
    }

    public override bool IsPlayerVulnerable(Node currentNode)
    {
        if (currentNode != rightDoor.linkedNode && currentNode != leftDoor.linkedNode) return false;

        if (currentNode == leftDoor.linkedNode && leftDoor.isDoorClosed.Value)
        {
            return false;
        }

        if (currentNode == rightDoor.linkedNode && rightDoor.isDoorClosed.Value)
        {
            return false;
        }

        return true;
    }

    public override void PowerOn()
    {
        base.PowerOn();

        GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", false, 0.5f, true);
        GameAudioManager.Instance.PlaySfxInterruptable("fan", false, 0.2f, true);
    }

    public override void PowerOff()
    {
        base.PowerOff();
    }

    public override void Update()
    {
        base.Update();

        RoomLight.enabled = isPlayerPoweredOn.Value && PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.SecurityOffice);
        flashLight.enabled = !isPlayerPoweredOn.Value && PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.SecurityOffice) || isGettingJumpscared;

        if (keypadSystem.isc)
    }

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        if (!isPlayerAlive.Value) yield break;
        isGettingJumpscared = true;

        flashLight.enabled = true;

        GameAudioManager.Instance.StopAllSfx();
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream, false);

        float elapsedTime = 0;

        while (elapsedTime < .6f)
        {
            cameraController.LerpTowardsDeathView();
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        GameAudioManager.Instance.StopSfx(audioSource);
        isGettingJumpscared = false;
    }

    [ClientRpc]
    public override void PlayDoorKnockAudioClientRpc(int indexOfCurrentNode, bool ferociousBanging)
    {
        if (!IsOwner) return;
        if (!isPlayerAlive.Value) return;

        Node animatronic_currentNode = AnimatronicManager.Instance.Nodes[indexOfCurrentNode];

        string audioClip = ferociousBanging ? "ferocious banging" : "door knock";
        AudioSource knocking = GameAudioManager.Instance.PlaySfxInterruptable(audioClip, true);
        knocking.panStereo = leftDoor.linkedNode == animatronic_currentNode ? -0.5f : 0.5f;
    }

    public override IEnumerator WaitUntilKillConditionsAreMet(Node currentNode)
    {
        float forceDeathTime = Time.time + Random.Range(1, timeToWaitBeforeKill);

        AudioSource moaningNoDiddy = GameAudioManager.Instance.PlaySfxInterruptable("moan", true);
        moaningNoDiddy.panStereo = leftDoor.linkedNode == currentNode ? -0.9f : 0.9f;

        if (playerComputer.isMonitorUp.Value)
        {
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || !playerComputer.isMonitorUp.Value || !isPlayerAlive.Value;
            });

            yield break;
        }
        else playerComputer.Lock();

        if (currentNode == leftDoor.linkedNode)
        {
            leftDoor.Lock();
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || cameraController.playerView.eulerAngles.y > 200 || !isPlayerAlive.Value;
            });

            yield break;
        }
        else if (currentNode == rightDoor.linkedNode)
        {
            rightDoor.Lock();
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || cameraController.playerView.eulerAngles.y < 160 || !isPlayerAlive.Value;
            });

            yield break;
        }
    }

    public override Node GetDoorwayNode(Node AttackingNode)
    {
        if (AttackingNode == leftDoor.linkedNode) return LeftDoorwayNode;
        if (AttackingNode == rightDoor.linkedNode) return RightDoorwayNode;

        return base.GetDoorwayNode(AttackingNode);
    }

    public override bool IsAnimatronicCloseToAttack(Node currentNode)
    {
        if (currentNode == leftDoor.linkedNode || currentNode == rightDoor.linkedNode) return true;

        return false;
    }

    public override bool CanGoldenFreddySpawnIn()
    {
        return playerComputer.isMonitorUp.Value;
    }

    public override bool HasSpottedGoldenFreddy()
    {
        return !playerComputer.isMonitorUp.Value;
    }

    public override bool HasLookedAwayFromGoldenFreddy()
    {
        return playerComputer.isMonitorUp.Value;
    }

    public override bool HasBlockedFoxy()
    {
        return leftDoor.isDoorClosed.Value;
    }

    public override IEnumerator IsFoxyReadyToAttack(Node hallwayNode, float definitiveAttackTime)
    {
        yield return new WaitUntil(() => !hallwayNode.isOccupied.Value && (Time.time > definitiveAttackTime || GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(hallwayNode)));
    }
}
