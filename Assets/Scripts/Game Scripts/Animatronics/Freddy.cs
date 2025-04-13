using System.Collections.Generic;
using UnityEngine;

public class Freddy : Animatronic
{
    public override void Initialise()
    {
        base.Initialise();

        var difficultyData = new Dictionary<GameNight, (float difficulty, float waitTimeToMove, float increment)>
        {
            { GameNight.One, (1f, 10f, 0f) },
            { GameNight.Two, (3f, 10.5f, 0.5f) },
            { GameNight.Three, (5f, 9f, 1f) },
            { GameNight.Four, (7f, 8f, 1.5f) },
            { GameNight.Five, (9f, 7f, 2f) },
            { GameNight.Six, (16f, 6f, 2.5f) },
            { GameNight.Seven, (20f, 5f, 3f) },
        };

        if (difficultyData.ContainsKey(GameManager.Instance.gameNight))
        {
            var (difficulty, waitTimeToMove, increment) = difficultyData[GameManager.Instance.gameNight];
            currentDifficulty.Value = difficulty;
            currentMovementWaitTime.Value = waitTimeToMove;
            hourlyDifficultyIncrementAmount = increment;
        }

    }

    private protected override bool MovementCondition()
    {
        return base.MovementCondition() && !GlobalCameraSystem.Instance.IsSomeoneWatchingNode(currentNode);
    }

    private protected override void HandleMovementAudio(bool makeNoise)
    {
        if (makeNoise) GameAudioManager.Instance.PlaySfxInterruptable($"freddy laugh {Random.Range(1, 3 + 1)}");
    }

    private protected override void Blocked(PlayerBehaviour playerBehaviour)
    {
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);
        ulong blockId = MultiplayerManager.Instance.GetPlayerDataFromPlayerRole(playerBehaviour.playerRole).clientId;
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