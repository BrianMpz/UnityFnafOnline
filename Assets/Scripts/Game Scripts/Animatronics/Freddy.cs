using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Freddy : Animatronic
{
    private protected override IEnumerator SecondaryMovementCondition()
    {
        yield return new WaitUntil(() => !GlobalCameraSystem.Instance.IsSomeoneWatchingNode(currentNode));
    }

    private protected override void HandleMovementAudio(bool makeNoise)
    {
        if (makeNoise) GameAudioManager.Instance.PlaySfxInterruptable($"freddy laugh {Random.Range(1, 3 + 1)}", true);
    }

    private protected override void Blocked(PlayerBehaviour playerBehaviour)
    {
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);
        ulong blockId = MultiplayerManager.Instance.GetPlayerDataFromPlayerRole(playerBehaviour.playerRole).clientId;

        playerBehaviour.currentPower.Value--;
        playerBehaviour.PlayDoorKnockAudioClientRpc(indexOfCurrentNode, true, MultiplayerManager.NewClientRpcSendParams(blockId));

        if (playerBehaviour.playerRole == PlayerRoles.Janitor)
        {
            SetNode(startNode, false, false);
        }
        else
        {
            // freddy doesnt get sent back
        }

    }
}