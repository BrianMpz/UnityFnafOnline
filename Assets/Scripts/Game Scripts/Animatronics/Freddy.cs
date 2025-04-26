using System.Collections;
using UnityEngine;

public class Freddy : Animatronic
{
    const float stallGracePeriod = 0.5f;

    private protected override IEnumerator SecondaryMovementCondition()
    {
        float unobservedTimer = 0f;

        while (true)
        {
            if (!GlobalCameraSystem.Instance.IsSomeoneWatchingNode(currentNode))
            {
                unobservedTimer += Time.deltaTime;

                if (unobservedTimer >= stallGracePeriod) yield break; // Node has been unobserved long enough, allow movement
            }
            else
            {
                unobservedTimer = 0f; // Reset timer if someone looks again
            }

            yield return null;
        }
    }

    private protected override void HandleMovementAudio(bool makeNoise)
    {
        if (makeNoise) GameAudioManager.Instance.PlaySfxInterruptable($"freddy laugh {Random.Range(1, 3 + 1)}", true);
    }

    private protected override void Blocked(PlayerBehaviour playerBehaviour)
    {
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);

        playerBehaviour.currentPower.Value--;
        playerBehaviour.PlayDoorKnockAudioClientRpc(indexOfCurrentNode, true);

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