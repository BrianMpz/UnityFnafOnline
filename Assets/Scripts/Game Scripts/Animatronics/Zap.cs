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
    [SerializeField] private NetworkVariable<bool> isApproachingPlayer = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> movementProgressValue = new(writePerm: NetworkVariableWritePermission.Server);
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
                moveSpeed = 9;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 10;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 4f;
                moveSpeed = 10;
                break;
        }

        movementProgress = StartCoroutine(ApproachPlayer());
    }

    private IEnumerator ApproachPlayer()
    {
        isApproachingPlayer.Value = true;

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

            movementProgressValue.Value = elapsedTime / duration; // server authoritative

            elapsedTime += Time.deltaTime;
            duration = 354f / moveSpeed;
        }

        isApproachingPlayer.Value = false;

        if (IsServer)
        {
            int indexOfTargetNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
            ConfirmKillServerRpc(indexOfTargetNode);

            yield return new WaitForSeconds(Mathf.Lerp(5, 60, 1 - currentDifficulty.Value / 20f)); // wait a random amount of time before entering the facility

            gameplayLoop = StartCoroutine(GameplayLoop()); // is released into facility
        }
    }

    private void Update()
    {
        if (!isApproachingPlayer.Value) return;

        Vector3 newPostion = Vector3.Lerp(startingPosition, endingPosition, movementProgressValue.Value);
        animatronicModel.localPosition = newPostion;
    }

    public void GetZapped()
    {
        if (movementProgress != null)
        {
            StopCoroutine(movementProgress);
            isApproachingPlayer.Value = false;

            movementProgress = StartCoroutine(ApproachPlayer());
        }

        GetZappedClientRpc();
    }

    [ClientRpc]
    public void GetZappedClientRpc()
    {
        if (PlayerRoleManager.Instance.IsSpectatingPlayer(PlayerRoles.Backstage)) MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut();
        // play zap animation later
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckIsBeingWatchedServerRpc(bool isBeingWatched)
    {
        this.isBeingWatched.Value = isBeingWatched;
    }
}
