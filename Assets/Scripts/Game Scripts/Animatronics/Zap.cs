using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Zap : Animatronic
{
    private const float baseMovementDuration = 300f; // takes 300s to kill at the slowest
    [SerializeField] private PlayerNode playerNode;
    [SerializeField] private Vector3 startingPosition;
    [SerializeField] private Vector3 endingPosition;
    [SerializeField] private float moveSpeed;
    [SerializeField] private NetworkVariable<bool> isBeingWatched;
    [SerializeField] private NetworkVariable<bool> isApproachingPlayer;
    public NetworkVariable<float> movementProgressValue;
    Coroutine movementProgress;

    public override void Initialise() // dont call base class
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;

        GetAnimatronicData();
        moveSpeed = Mathf.Lerp(0, 10, currentDifficulty.Value / 20);

        movementProgress = StartCoroutine(ApproachPlayer());
    }

    private IEnumerator ApproachPlayer()
    {
        isApproachingPlayer.Value = true;
        animatronicModel.eulerAngles = Vector3.zero;

        movementProgressValue.Value = 0;
        float elapsedTime = 0;
        float duration = Mathf.Infinity;

        while (elapsedTime < duration)
        {
            yield return null;

            if (isBeingWatched.Value)
            {
                continue;
            }

            elapsedTime += Time.deltaTime;
            movementProgressValue.Value = elapsedTime / duration; // server authoritative

            duration = baseMovementDuration / moveSpeed;
        }

        isApproachingPlayer.Value = false;

        if (IsServer)
        {
            int indexOfTargetNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
            if (playerNode.IsAlive) ConfirmKillServerRpc(indexOfTargetNode);

            yield return new WaitForSeconds(waitTimeToStartMoving);

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
        if (PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.Backstage))
        {
            MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(); // black out for a second or 2
            GameAudioManager.Instance.PlaySfxOneShot("controlled shock", false);
        }
        // play zap animation later
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckIsBeingWatchedServerRpc(bool isBeingWatched)
    {
        this.isBeingWatched.Value = isBeingWatched;
    }
}
