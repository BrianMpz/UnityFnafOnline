using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Foxy : Animatronic
{
    [SerializeField] private NetworkVariable<float> currentAttackAttempt;
    public float foxyProgress;
    [SerializeField] private bool isLocked = false; // Whether Foxy is locked by camera observation
    [SerializeField] private float lockCoolDownTime = 5f;  // Minimum time for lock duration
    private Coroutine unlockCoroutine;
    private AudioSource foxyRunAudio;
    private AudioSource foxyTauntAudio;
    private AudioSource foxyThunkAudio;

    [SerializeField] private Node securityOfficeHallwayNode;
    [SerializeField] private Node partsAndServiceHallwayNode;
    [SerializeField] private Node backstageHallwayNode;
    [SerializeField] private FoxyWarningSign foxyWarningSign;
    [SerializeField] private Animation foxyAnimation;
    public event Action<PlayerRoles, float> OnFoxyPowerDrain;

    public override void Initialise()
    {
        if (!IsServer) return;

        foxyAnimation.enabled = false;
        currentAttackAttempt.Value = 0;

        GlobalCameraSystem.Instance.OnPlayersWatchingFoxyUpdate += CameraSystem_OnPlayersWatchingFoxyUpdated;
        GameManager.Instance.OnPlayerPowerDown += GameManager_OnPlayerPowerDown;

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 1;
                currentMovementWaitTime.Value = 10;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 4;
                currentMovementWaitTime.Value = 8;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 7;
                currentMovementWaitTime.Value = 6;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 10;
                currentMovementWaitTime.Value = 5;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 13;
                currentMovementWaitTime.Value = 4;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 3;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 3;
                break;
        }

        ResetFoxyServerRpc();

        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;
    }

    public float CalculatePowerDrain()
    {
        return Mathf.Pow(currentAttackAttempt.Value, 2);
    }

    private void GameManager_OnPlayerPowerDown(PlayerRoles playerRole)
    {
        StartCoroutine(TargetPoweredDownPlayer(playerRole));
    }

    private IEnumerator TargetPoweredDownPlayer(PlayerRoles playerRole)
    {
        yield return new WaitUntil(() =>
            {
                return isWaitingOnClient == false;
            }
        );

        if (gameplayLoop != null) StopCoroutine(gameplayLoop);

        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes.FirstOrDefault(x =>
            x.playerBehaviour != null && x.playerBehaviour.playerRole == playerRole && x.playerBehaviour.isPlayerAlive.Value);

        gameplayLoop = StartCoroutine(GameplayLoop(true));
    }

    // Called when players change their view on Foxy in the cameras
    private void CameraSystem_OnPlayersWatchingFoxyUpdated(int playersWatchingFoxy)
    {
        if (playersWatchingFoxy > 0)
        {
            LockFoxy();
        }
        else if (unlockCoroutine == null)
        {
            unlockCoroutine = StartCoroutine(UnlockAfterDelay());
        }
    }

    void LockFoxy()
    {
        isLocked = true;
        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }
    }

    private IEnumerator UnlockAfterDelay()
    {
        yield return new WaitForSeconds(lockCoolDownTime);
        isLocked = false;
        unlockCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetFoxyServerRpc(bool playThunk = false, int indexOfPlayerNode = -1)
    {
        DisableFoxyAnimationClientRpc();

        isWaitingOnClient = false;

        if (playThunk)
        {
            PlayThunkClientRpc(indexOfPlayerNode);
        }

        SetNode(startNode, false, false);

        // targetedPlayer = null;

        foxyProgress = 0;
        gameplayLoop = StartCoroutine(GameplayLoop(false));
    }

    // The main loop where Foxy's movements and attacks are handled
    private protected override IEnumerator GameplayLoop(bool isAggro)
    {
        isAggrivated.Value = isAggro;

        if (isAggro)
        {
            currentMovementWaitTime.Value /= 2;
            currentDifficulty.Value += 10;
        }
        else yield return new WaitForSeconds(waitTimeToStartMoving); // Initial wait before starting movement

        currentAttackAttempt.Value++;

        TargetRandomPlayer();

        if (targetedPlayer != null) SetFoxyProgressClientRpc(0, targetedPlayer.playerBehaviour.playerRole);

        while (GameManager.Instance.isPlaying)
        {
            yield return new WaitForSeconds(currentMovementWaitTime.Value); // Delay before each movement attempt

            if (targetedPlayer == null) continue;

            if (UnityEngine.Random.Range(1, 21) <= currentDifficulty.Value)
            {
                if (!isLocked)
                {
                    yield return MovementOpportunity(targetedPlayer);
                }
                else
                {
                    //Debug.Log("Foxy is locked by camera.");
                }
            }
            else
            {
                //Debug.Log("Movement Opportunity failed.");
            }
        }
    }

    private IEnumerator MovementOpportunity(PlayerNode playerNode)
    {
        float newFoxyProgress = foxyProgress + 1; // Increase Foxy's progress towards attack
        SetFoxyProgressClientRpc(newFoxyProgress, targetedPlayer.playerBehaviour.playerRole);
        //Debug.Log($"Foxy is now at stage {newFoxyProgress}!");

        // If Foxy reaches phase 5 (attack phase)
        if (newFoxyProgress >= 5)
        {
            SetFoxyProgressClientRpc(0, targetedPlayer.playerBehaviour.playerRole);
            yield return WaitToAttack(playerNode);
            //Debug.Log("Foxy is in attack phase!");
        }
    }

    [ClientRpc]
    private void SetFoxyProgressClientRpc(float newProgress, PlayerRoles playerRole)
    {
        if (foxyProgress >= 5)
        {
            animatronicModel.gameObject.SetActive(false);
            foxyWarningSign.UpdateWarningSign("Im Coming...");
        }
        else
        {
            animatronicModel.gameObject.SetActive(true);
            foxyWarningSign.UpdateWarningSign(playerRole);
        }

        foxyProgress = newProgress;

        Vector3 currentRotation = animatronicModel.eulerAngles;
        currentRotation.y = Mathf.Lerp(90f, -90f, foxyProgress / 4);
        animatronicModel.eulerAngles = currentRotation;

        GameManager.Instance.OnFoxyStatusChanged?.Invoke();
    }

    private IEnumerator WaitToAttack(PlayerNode playerNode)
    {
        float definitiveAttackTime = Time.time + UnityEngine.Random.Range(10, 30);
        int indexOfPlayerNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);

        switch (playerNode.playerBehaviour.playerRole)
        {
            case PlayerRoles.SecurityOffice:

                yield return new WaitUntil(() =>
                {
                    return !securityOfficeHallwayNode.isOccupied &&
                    (
                        Time.time > definitiveAttackTime ||
                        GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(securityOfficeHallwayNode)
                    );
                });

                SetNode(securityOfficeHallwayNode);
                break;

            case PlayerRoles.PartsAndService:

                yield return new WaitUntil(() =>
                {
                    return !partsAndServiceHallwayNode.isOccupied &&
                    (
                        Time.time > definitiveAttackTime ||
                        GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(partsAndServiceHallwayNode) ||
                        PlayerRoleManager.Instance.partsAndServiceBehaviour.door.doorLight.isFlashingLight.Value
                    );
                });

                SetNode(partsAndServiceHallwayNode);
                break;

            case PlayerRoles.Backstage:

                yield return new WaitUntil(() =>
                {
                    return !backstageHallwayNode.isOccupied &&
                    (
                        Time.time > definitiveAttackTime ||
                        GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(backstageHallwayNode) ||
                        PlayerRoleManager.Instance.backstagePlayerBehaviour.door.doorLight.isFlashingLight.Value
                    );
                });

                SetNode(backstageHallwayNode);
                break;
        }

        InitiateAttackClientRpc(indexOfPlayerNode);

        if (gameplayLoop != null) StopCoroutine(gameplayLoop);

        isWaitingOnClient = true;

    }

    [ClientRpc]
    private void InitiateAttackClientRpc(int indexOfPlayerNode)
    {
        animatronicModel.gameObject.SetActive(true);
        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes[indexOfPlayerNode];

        PlayAttackVisual(playerNode);
        StartCoroutine(PlayAttackAudio(playerNode));
        if (targetedPlayer.playerBehaviour == GameManager.localPlayerBehaviour) StartCoroutine(CheckPlayerCondition(playerNode));
    }

    private IEnumerator CheckPlayerCondition(PlayerNode playerNode)
    {
        int indexOfPlayerNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
        PlayerBehaviour pb = playerNode.playerBehaviour;

        yield return new WaitForSeconds(1.7f);

        switch (pb.playerRole)
        {
            case PlayerRoles.SecurityOffice:
                SecurityOfficeBehaviour securityOfficeBehaviour = PlayerRoleManager.Instance.securityOfficeBehaviour;

                if (securityOfficeBehaviour.leftDoor.isDoorClosed.Value)
                {
                    Blocked(indexOfPlayerNode, pb);
                }
                else if (pb.isPlayerAlive.Value)
                {
                    KillPlayerServerRpc(indexOfPlayerNode);
                }
                else
                {
                    ResetFoxyServerRpc(false, indexOfPlayerNode);
                }
                break;
            case PlayerRoles.PartsAndService:
                PartsAndServiceBehaviour partsAndServiceBehaviour = PlayerRoleManager.Instance.partsAndServiceBehaviour;

                if (partsAndServiceBehaviour.door.isDoorClosed.Value)
                {
                    Blocked(indexOfPlayerNode, pb);
                }
                else if (pb.isPlayerAlive.Value)
                {
                    KillPlayerServerRpc(indexOfPlayerNode);
                }
                else
                {
                    ResetFoxyServerRpc(false, indexOfPlayerNode);
                }
                break;
            case PlayerRoles.Backstage:
                BackstagePlayerBehaviour backstagePlayerBehaviour = PlayerRoleManager.Instance.backstagePlayerBehaviour;

                if (backstagePlayerBehaviour.door.isDoorClosed.Value)
                {
                    Blocked(indexOfPlayerNode, pb);
                }
                else if (pb.isPlayerAlive.Value)
                {
                    KillPlayerServerRpc(indexOfPlayerNode);
                }
                else
                {
                    ResetFoxyServerRpc(false, indexOfPlayerNode);
                }
                break;
        }
    }

    private void Blocked(int indexOfPlayerNode, PlayerBehaviour pb)
    {
        PlayThunkAudio(1);
        OnFoxyPowerDrain.Invoke(pb.playerRole, CalculatePowerDrain());
        ResetFoxyServerRpc(true, indexOfPlayerNode);
    }

    [ServerRpc(RequireOwnership = false)]
    private void KillPlayerServerRpc(int indexOfPlayerNode)
    {
        StartCoroutine(KillPlayer(indexOfPlayerNode));
    }

    private IEnumerator KillPlayer(int indexOfPlayerNode)
    {
        DisableFoxyAnimationClientRpc();
        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes[indexOfPlayerNode];
        string killerName = gameObject.name;

        playerNode.playerBehaviour.DieClientRpc(killerName, deathScream, MultiplayerManager.NewClientRpcSendParams(playerNode.playerBehaviour.OwnerClientId));

        SetNode(playerNode, true);

        yield return new WaitForSeconds(10);

        ResetFoxyServerRpc();
    }

    [ClientRpc]
    private void DisableFoxyAnimationClientRpc()
    {
        foxyAnimation.enabled = false;
        foxyWarningSign.UpdateWarningSign("...");
    }

    private IEnumerator PlayAttackAudio(PlayerNode playerNode)
    {
        if (PlayerRoleManager.Instance.IsPlayerDead(GameManager.localPlayerBehaviour)) yield break;

        float audioDuration = 1.7f;

        if (!playerNode.playerBehaviour.IsOwner)
        {
            foxyRunAudio = GameAudioManager.Instance.PlaySfxInterruptable("foxy run", 0.7f);
            yield return new WaitForSeconds(0.2f);
            foxyTauntAudio = GameAudioManager.Instance.PlaySfxInterruptable(isAggrivated.Value ? "foxy taunt" : "fire in the hole", 0.3f);

            float elapedTime = 0;

            while (elapedTime < audioDuration)
            {
                if (foxyTauntAudio != null) foxyTauntAudio.volume = 1 - (elapedTime / audioDuration);
                elapedTime += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            foxyRunAudio = GameAudioManager.Instance.PlaySfxInterruptable("foxy run", 0);
            yield return new WaitForSeconds(0.2f);
            foxyTauntAudio = GameAudioManager.Instance.PlaySfxInterruptable(isAggrivated.Value ? "foxy taunt" : "fire in the hole", 0f);

            float elapedTime = 0;

            while (elapedTime < audioDuration)
            {
                // start quiet and slowly get louder
                if (foxyRunAudio != null) foxyRunAudio.volume = (elapedTime / audioDuration) + 0.3f;
                if (foxyTauntAudio != null) foxyTauntAudio.volume = (elapedTime / audioDuration) + 0.2f;
                elapedTime += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void PlayAttackVisual(PlayerNode playerNode)
    {
        PlayerBehaviour playerBehaviour = playerNode.playerBehaviour;

        foxyAnimation.enabled = true;

        switch (playerBehaviour.playerRole)
        {
            case PlayerRoles.SecurityOffice:
                GameManager.Instance.OnFoxyAttacking?.Invoke(securityOfficeHallwayNode);
                foxyAnimation.Play("Foxy Security Office Run");
                break;
            case PlayerRoles.PartsAndService:
                GameManager.Instance.OnFoxyAttacking?.Invoke(partsAndServiceHallwayNode);
                foxyAnimation.Play("Foxy Parts And Service Run");
                break;
            case PlayerRoles.Backstage:
                GameManager.Instance.OnFoxyAttacking?.Invoke(backstageHallwayNode);
                foxyAnimation.Play("Foxy Backstage Run");
                break;
        }
    }

    [ClientRpc]
    private void PlayThunkClientRpc(int indexOfPlayerNode = -1)
    {
        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes[indexOfPlayerNode];

        if (playerNode.playerBehaviour.IsOwner) return;
        if (GameManager.Instance.IsSpectating) return;

        PlayThunkAudio(0.3f); // set for players that arent the target
    }

    public void PlayThunkAudio(float volume)
    {
        GameAudioManager.Instance.StopSfx(foxyRunAudio);
        GameAudioManager.Instance.StopSfx(foxyTauntAudio);
        GameAudioManager.Instance.PlaySfxInterruptable("thunk", volume);
    }
}
