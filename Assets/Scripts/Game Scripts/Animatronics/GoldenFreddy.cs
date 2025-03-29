using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GoldenFreddy : Animatronic
{
    public override void Initialise() // dont call base class
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 1;
                currentMovementWaitTime.Value = 60f;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 2;
                currentMovementWaitTime.Value = 50f;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 4;
                currentMovementWaitTime.Value = 40f;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 7;
                currentMovementWaitTime.Value = 30f;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 11;
                currentMovementWaitTime.Value = 20f;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 10f;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 10f;
                break;
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
            yield return new WaitForSeconds(waitTimeToStartMoving);
            if (UnityEngine.Random.Range(1, 20 + 1) > currentDifficulty.Value) continue;

            TargetRandomPlayer();
            PlayerBehaviour targetPlayer = target.GetComponent<PlayerNode>().playerBehaviour;
            transform.position = target.GetComponent<PlayerNode>().transform.position;

            // Wait until the player is NOT in a position to see Golden Freddy spawn
            yield return new WaitUntil(() => targetPlayer.CanGoldenFreddySpawnIn() || !targetPlayer.isPlayerAlive.Value);
            yield return new WaitForSeconds(0.2f);

            SpawnGoldenFreddyClientRpc(targetPlayer.playerRole);

            // Wait until the player spots Golden Freddy
            yield return new WaitUntil(() => targetPlayer.HasSpottedGoldenFreddy() || !targetPlayer.isPlayerAlive.Value);

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
    private void SpawnGoldenFreddyClientRpc(PlayerRoles playerRole)
    {
        PlayerBehaviour targetPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole);
        targetPlayer.SpawnGoldenFreddy();

        if (GameManager.Instance.IsSpectating && PlayerRoleManager.Instance.IsSpectatingPlayer(PlayerRoles.SecurityOffice)) MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(0.5f);
    }

    [ClientRpc]
    private void DespawnGoldenFreddyClientRpc(PlayerRoles playerRole)
    {
        PlayerBehaviour targetPlayer = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole);
        targetPlayer.DespawnGoldenFreddy();

        if (GameManager.Instance.IsSpectating && PlayerRoleManager.Instance.IsSpectatingPlayer(PlayerRoles.SecurityOffice)) MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(0.5f);
    }
}
