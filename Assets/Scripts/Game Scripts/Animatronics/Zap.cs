using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Zap : Animatronic
{
    [SerializeField] private PlayerNode playerNode;
    [SerializeField] private float startingPositionZ;
    [SerializeField] private float endingPositionZ;
    [SerializeField] private float moveSpeed;
    private NetworkVariable<bool> isBeingWatched = new(writePerm: NetworkVariableWritePermission.Server);
    Coroutine movementProgress;

    public override void Initialise() // dont call base class
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 1;
                currentMovementWaitTime.Value = 4;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 2;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 4;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 7;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 11;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 4f;
                break;
        }

        ApproachPlayerClientRpc();
    }

    [ClientRpc]
    private void ApproachPlayerClientRpc()
    {
        movementProgress = StartCoroutine(ApproachPlayer());
    }

    private IEnumerator ApproachPlayer()
    {
        animatronicModel.position = new(animatronicModel.position.x, animatronicModel.position.y, startingPositionZ);
        float elapsedTime = 0;
        float duration = Mathf.Infinity;

        while (elapsedTime < duration)
        {
            if (isBeingWatched.Value)
            {
                yield return null;
                continue;
            }

            float newZpostion = Mathf.Lerp(startingPositionZ, endingPositionZ, elapsedTime / duration);
            animatronicModel.position = new(animatronicModel.position.x, animatronicModel.position.y, newZpostion);

            yield return null;

            elapsedTime += Time.deltaTime;
            duration = 900 / currentDifficulty.Value / moveSpeed;
        }

        if (IsServer)
        {
            int indexOfTargetNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
            ConfirmKillServerRpc(indexOfTargetNode);

            yield return new WaitForSeconds(UnityEngine.Random.Range(5, 60));

            gameplayLoop = StartCoroutine(GameplayLoop(false));
        }
    }

    public void ZapAnimatronic()
    {
        ZapAnimatronicClientRpc();
    }

    [ClientRpc]
    public void ZapAnimatronicClientRpc()
    {
        if (movementProgress != null)
        {
            StopCoroutine(movementProgress);
            movementProgress = StartCoroutine(ApproachPlayer());
        }

        if (!GameManager.Instance.IsSpectating || SpectatorUI.Instance.GetCurrentSpectator() != playerNode.playerBehaviour) return;
        MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckIsBeingWatchedServerRpc(bool isBeingWatched)
    {
        this.isBeingWatched.Value = isBeingWatched;
    }
}
