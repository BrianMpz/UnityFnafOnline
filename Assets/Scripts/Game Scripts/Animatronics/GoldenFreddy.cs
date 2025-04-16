using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GoldenFreddy : Animatronic
{
    public override void Initialise() // dont call base class
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;

        var difficultyData = new Dictionary<GameNight, (float difficulty, float increment)>
        {
            { GameNight.One, (1f, 0.5f) },
            { GameNight.Two, (3f, 1f) },
            { GameNight.Three, (5f, 1.5f) },
            { GameNight.Four, (7f, 2f) },
            { GameNight.Five, (9f, 2.5f) },
            { GameNight.Six, (16f, 3f) },
            { GameNight.Seven, (20f, 3.5f) },
        };

        if (difficultyData.ContainsKey(GameManager.Instance.gameNight))
        {
            var (difficulty, increment) = difficultyData[GameManager.Instance.gameNight];
            currentDifficulty.Value = difficulty;
            hourlyDifficultyIncrementAmount = increment;
        }

        StartCoroutine(WaitForJanitorToDie());
    }

    private IEnumerator WaitForJanitorToDie()
    {
        yield return new WaitForSeconds(waitTimeToStartMoving);
        yield return new WaitUntil(() => !PlayerRoleManager.Instance.janitorBehaviour.isPlayerAlive.Value);
        StartCoroutine(GameplayLoop());
    }

    private new IEnumerator GameplayLoop()
    {
        while (GameManager.Instance.isPlaying)
        {
            GetComponent<Image>().enabled = false;
            yield return new WaitForSeconds(Mathf.Lerp(10, 60, 1 - (currentDifficulty.Value / 20f)));
            if (UnityEngine.Random.Range(1, 20 + 1) > currentDifficulty.Value) continue;

            TargetRandomPlayer();
            if (currentTarget == null) goto ContinueOuterLoop; // Jump to the start of the outer loop
            GetComponent<Image>().enabled = true;
            PlayerBehaviour targetPlayer = currentTarget.GetComponent<PlayerNode>().playerBehaviour;
            transform.position = currentTarget.GetComponent<PlayerNode>().transform.position;

            // Wait until the player is NOT in a position to see Golden Freddy spawn
            yield return new WaitUntil(() => targetPlayer.CanGoldenFreddySpawnIn() || !targetPlayer.isPlayerAlive.Value);
            yield return new WaitForSeconds(0.2f);

            SpawnGoldenFreddyClientRpc(targetPlayer.playerRole);

            // Wait until the player spots Golden Freddy
            yield return new WaitUntil(() => targetPlayer.HasSpottedGoldenFreddy() || !targetPlayer.isPlayerAlive.Value);
            PlayLaughClientRpc(MultiplayerManager.NewClientRpcSendParams(targetPlayer.OwnerClientId));

            // Start the kill countdown
            float killTimer = Mathf.Lerp(2f, 1f, currentDifficulty.Value / 20);
            while (killTimer > 0f)
            {
                if (targetPlayer.HasLookedAwayFromGoldenFreddy() || !targetPlayer.isPlayerAlive.Value) // Stop if player looks away
                {
                    yield return new WaitForSeconds(0.2f);
                    DespawnGoldenFreddyClientRpc(targetPlayer.playerRole);
                    goto ContinueOuterLoop; // Jump to the start of the outer loop
                }

                killTimer -= Time.deltaTime;
                yield return null;
            }

            targetPlayer.DieClientRpc("Golden Freddy", default, MultiplayerManager.NewClientRpcSendParams(targetPlayer.OwnerClientId));

        ContinueOuterLoop:; // Label for the outer loop to continue
        }
    }

    [ClientRpc]
    private void PlayLaughClientRpc(ClientRpcParams clientRpcParams)
    {
        if (PlayerRoleManager.Instance.GetLocalPlayerBehaviour().isPlayerAlive.Value) GameAudioManager.Instance.PlaySfxInterruptable("freddy laugh normal speed", false);
    }

    [ClientRpc]
    private void SpawnGoldenFreddyClientRpc(PlayerRoles playerRole)
    {
        PlayerBehaviour targetPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole);
        targetPlayer.SpawnGoldenFreddy();

        if (GameManager.Instance.IsSpectating && PlayerRoleManager.Instance.IsSpectatingPlayer(playerRole)) MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(0.5f, false);
    }

    [ClientRpc]
    private void DespawnGoldenFreddyClientRpc(PlayerRoles playerRole)
    {
        PlayerBehaviour targetPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole);
        targetPlayer.DespawnGoldenFreddy();

        if (GameManager.Instance.IsSpectating && PlayerRoleManager.Instance.IsSpectatingPlayer(playerRole)) MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(0.5f, false);
    }
}
