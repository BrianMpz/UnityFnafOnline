using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GoldenFreddy : Animatronic
{
    public override void Initialise() // dont call base class
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;

        GetAnimatronicData();

        StartCoroutine(WaitForJanitorToDie());
    }

    private protected override void GetAnimatronicData()
    {
        string animatronicName = gameObject.name;
        GameNight currentNight = GameManager.Instance.gameNight;

        if (AnimatronicManager.Instance.CanFindNightData(currentNight, animatronicName, out AnimatronicData animatronicData))
        {
            waitTimeToStartMoving = (int)animatronicData.waitTimeToStartMoving;
            currentDifficulty.Value = (float)animatronicData.startingDifficulty;
            hourlyDifficultyIncrementAmount = (float)animatronicData.hourlyDifficultyIncrementAmount;
        }
        else
        {
            Debug.LogWarning($"Difficulty data not found for {animatronicName} on night {currentNight}");
        }
    }

    private IEnumerator WaitForJanitorToDie()
    {
        yield return new WaitForSeconds(waitTimeToStartMoving);
        yield return new WaitUntil(() => !PlayerRoleManager.Instance.janitorBehaviour.isPlayerAlive.Value);
        hasStartedMoving = false;
        yield return new WaitForSeconds(waitTimeToStartMoving);
        hasStartedMoving = true;
        StartCoroutine(GameplayLoop());
    }

    private new IEnumerator GameplayLoop()
    {
        while (GameManager.Instance.isPlaying)
        {
            GetComponent<Image>().enabled = false;
            yield return new WaitForSeconds(Mathf.Lerp(20, 60, 1 - (currentDifficulty.Value / 20f)));
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
