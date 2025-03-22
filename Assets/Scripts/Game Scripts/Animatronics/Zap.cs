using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Zap : Animatronic
{
    [SerializeField] private PlayerNode playerNode;
    [SerializeField] private Vector3 startingPosition;
    [SerializeField] private Vector3 endingPosition;
    [SerializeField] private float moveSpeed;
    [SerializeField] private NetworkVariable<bool> isBeingWatched = new(writePerm: NetworkVariableWritePermission.Server);
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
                moveSpeed = 1;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 2;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 3;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 4;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 5;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 7;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 7;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 11;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 7;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 9;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 11;
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
        animatronicModel.position = startingPosition;
        float elapsedTime = 0;
        float duration = Mathf.Infinity;

        while (elapsedTime < duration)
        {
            yield return null;

            if (isBeingWatched.Value)
            {
                continue;
            }

            Vector3 newPostion = Vector3.Lerp(startingPosition, endingPosition, elapsedTime / duration);
            animatronicModel.localPosition = newPostion;

            elapsedTime += Time.deltaTime;
            duration = 354f / moveSpeed;
        }

        if (IsServer)
        {
            int indexOfTargetNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
            ConfirmKillServerRpc(indexOfTargetNode);

            yield return new WaitForSeconds(UnityEngine.Random.Range(5, 60)); // wait a random amount of time before entering the pizerria

            gameplayLoop = StartCoroutine(GameplayLoop()); // is released
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

        if (PlayerRoleManager.Instance.IsSpectatingPlayer(PlayerRoles.Backstage)) MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckIsBeingWatchedServerRpc(bool isBeingWatched)
    {
        this.isBeingWatched.Value = isBeingWatched;
    }
}
