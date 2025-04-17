using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Zap : Animatronic
{
    private const float baseMovementDuration = 354f; // takes 354s to kill at the slowest
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


        var difficultyData = new Dictionary<GameNight, (float difficulty, float waitTime, float increment, float moveSpeed)>
        {
            { GameNight.One, (1f, 3f, 3f, 1) },
            { GameNight.Two, (3f, 3f, 3.25f, 3) },
            { GameNight.Three, (5f, 3f, 3.5f, 5) },
            { GameNight.Four, (7f, 3f, 3.75f, 7) },
            { GameNight.Five, (9f, 3f, 4f, 9) },
            { GameNight.Six, (16f, 3f, 4.25f, 10) },
            { GameNight.Seven, (20f, 3f, 4.5f, 12) },
        };

        if (difficultyData.ContainsKey(GameManager.Instance.gameNight))
        {
            var (difficulty, waitTime, increment, movementSpeed) = difficultyData[GameManager.Instance.gameNight];
            currentDifficulty.Value = difficulty;
            currentMovementWaitTime.Value = waitTime;
            hourlyDifficultyIncrementAmount = increment;
            moveSpeed = movementSpeed;
        }

        movementProgress = StartCoroutine(ApproachPlayer());
    }

    private IEnumerator ApproachPlayer()
    {
        isApproachingPlayer.Value = true;
        transform.eulerAngles = Vector3.zero;

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

            yield return new WaitForSeconds(Mathf.Lerp(5, 60, 1 - (currentDifficulty.Value / 20f))); // wait a random amount of time before entering the facility

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
