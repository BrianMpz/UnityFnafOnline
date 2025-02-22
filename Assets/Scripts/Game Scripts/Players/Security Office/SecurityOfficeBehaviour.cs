using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SecurityOfficeBehaviour : PlayerBehaviour
{
    public SecurityOfficeCameraController cameraController;
    public Door leftDoor;
    public Door rightDoor;
    [SerializeField] private Light RoomLight;
    [SerializeField] private Light flashLight;
    [SerializeField] private float timeToWaitBeforeKill;

    [SerializeField] private Node LeftAttackingNode;
    [SerializeField] private Node LeftDoorwayNode;

    [SerializeField] private Node RightAttackingNode;
    [SerializeField] private Node RightDoorwayNode;

    public override void SetCameraView()
    {
        if (playerComputer.isMonitorUp.Value || !isAlive.Value) return;

        cameraController.SetCameraView();
    }

    public override void SetUsage()
    {
        powerUsage.Value = 1;

        if (leftDoor.isDoorClosed.Value) powerUsage.Value += 2;
        if (leftDoor.doorLight.isFlashingLight.Value) powerUsage.Value++;

        if (rightDoor.isDoorClosed.Value) powerUsage.Value++;
        if (rightDoor.doorLight.isFlashingLight.Value) powerUsage.Value++;

        if (playerComputer.isMonitorUp.Value) powerUsage.Value++;
        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) powerUsage.Value -= 5;
    }

    public override bool IsVulnerable(Node currentNode)
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
        RoomLight.enabled = true;
        flashLight.enabled = false;
        PowerOnServerRpc();
        AudioSource ambiance = GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", 0.5f, true);
        AudioSource fan = GameAudioManager.Instance.PlaySfxInterruptable("fan", 0.2f, true);
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

    [ClientRpc]
    public override void KnockOnDoorClientRpc(int indexOfCurrentNode, ClientRpcParams clientRpcParams)
    {
        Node animatronic_currentNode = AnimatronicManager.Instance.Nodes[indexOfCurrentNode];

        AudioSource knocking = GameAudioManager.Instance.PlaySfxInterruptable("door knock");
        knocking.panStereo = leftDoor.linkedNode == animatronic_currentNode ? -0.5f : 0.5f;
    }

    public override IEnumerator WaitToKill(Node currentNode)
    {
        float forceDeathTime = Time.time + Random.Range(1, timeToWaitBeforeKill);

        AudioSource moaningNoDiddy = GameAudioManager.Instance.PlaySfxInterruptable("moan");
        moaningNoDiddy.panStereo = leftDoor.linkedNode == currentNode ? -0.5f : 0.5f;

        if (playerComputer.isMonitorUp.Value)
        {
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || !playerComputer.isMonitorUp.Value || !isAlive.Value;
            });

            yield break;
        }
        else playerComputer.Lock();

        if (currentNode == leftDoor.linkedNode)
        {
            leftDoor.Lock();
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || cameraController.playerView.eulerAngles.y > 200 || !isAlive.Value || !poweredOn.Value;
            });

            yield break;
        }
        else if (currentNode == rightDoor.linkedNode)
        {
            rightDoor.Lock();
            yield return new WaitUntil(() =>
            {
                return Time.time > forceDeathTime || cameraController.playerView.eulerAngles.y < 160 || !isAlive.Value || !poweredOn.Value;
            });

            yield break;
        }
    }

    public override Node GetDoorwayNode(Node AttackingNode)
    {
        if (AttackingNode == LeftAttackingNode) return LeftDoorwayNode;
        if (AttackingNode == RightAttackingNode) return RightDoorwayNode;

        throw new System.Exception("Something aint right...");
    }

    public override bool IsCameraUp()
    {
        return playerComputer.isMonitorUp.Value;
    }
}
