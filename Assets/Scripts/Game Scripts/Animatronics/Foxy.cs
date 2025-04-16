using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Foxy : Animatronic
{
    [SerializeField] private bool isLocked = false; // Whether Foxy is locked by camera observation
    [SerializeField] private bool exponentialPowerDrain = true;
    [SerializeField] private NetworkVariable<float> currentAttackAttempt;
    public float foxyProgress;
    [SerializeField] private float lockCoolDownTime = 5f;  // Minimum time for lock duration
    private Coroutine unlockCoroutine;
    private AudioSource foxyRunAudio;
    private AudioSource foxyTauntAudio;
    private AudioSource thunkAudio;

    [SerializeField] private FoxyWarningSign foxyWarningSign;
    [SerializeField] private Animation foxyAnimation;
    public event Action<PlayerRoles, float> OnFoxyPowerDrain;

    [SerializeField] private Node securityOfficeHallwayNode;
    [SerializeField] private Node janitorHallwayNode;
    [SerializeField] private Node partsAndServiceHallwayNode;
    [SerializeField] private Node backstageHallwayNode;

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
        if (exponentialPowerDrain) return Mathf.Pow(currentAttackAttempt.Value, 2);
        else return 1;
    }

    private void GameManager_OnPlayerPowerDown(PlayerRoles playerRole)
    {
        StartCoroutine(TargetPoweredDownPlayer(playerRole));
    }

    private IEnumerator TargetPoweredDownPlayer(PlayerRoles playerRole)
    {
        yield return new WaitUntil(() => !isWaitingOnClient);

        if (gameplayLoop != null) StopCoroutine(gameplayLoop);

        PlayerNode playerNode = AnimatronicManager.Instance.GetPlayerNodeFromPlayerRole(playerRole);

        gameplayLoop = StartCoroutine(GameplayLoop(true, playerNode));
    }

    // Called when players change their view on Foxy in the cameras
    private void CameraSystem_OnPlayersWatchingFoxyUpdated(int playersWatchingFoxy)
    {
        if (playersWatchingFoxy > 0)
        {
            LockFoxy();
        }
        else unlockCoroutine ??= StartCoroutine(UnlockAfterDelay());
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
    private IEnumerator GameplayLoop(bool shouldBeAggrivated, PlayerNode specificTarget = null)
    {
        SetAggrivation(shouldBeAggrivated || GlobalCameraSystem.Instance.timeSinceLastFoxyCheck > 30, 20, 2);

        yield return new WaitForSeconds(waitTimeToStartMoving); // Initial wait before starting movement

        currentAttackAttempt.Value++;

        if (specificTarget != null)
        {
            currentTarget = specificTarget;
        }
        else
        {
            TargetRandomPlayer();

            if (currentTarget == null) yield break; // everyone is dead
        }

        PlayerNode playerNode = currentTarget.GetComponent<PlayerNode>();

        SetFoxyProgressClientRpc(0, playerNode.playerBehaviour.playerRole);

        while (GameManager.Instance.isPlaying)
        {
            yield return new WaitForSeconds(currentMovementWaitTime.Value); // Delay before each movement attempt

            if (UnityEngine.Random.Range(1, 20 + 1) <= currentDifficulty.Value)
            {
                if (!isLocked)
                {
                    yield return MovementOpportunity(playerNode);

                    if (isWaitingOnClient) yield break;
                }
                else
                {
                    //Debug.Log("Foxy is locked by camera.");
                }
            }
        }
    }

    private IEnumerator MovementOpportunity(PlayerNode playerNode)
    {
        float newFoxyProgress = foxyProgress + 1; // Increase Foxy's progress towards attack
        SetFoxyProgressClientRpc(newFoxyProgress, playerNode.playerBehaviour.playerRole);
        //Debug.Log($"Foxy is now at stage {newFoxyProgress}!");

        // If Foxy reaches phase 5 (attack phase)
        if (newFoxyProgress >= 5)
        {
            SetFoxyProgressClientRpc(0, playerNode.playerBehaviour.playerRole);
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

    private IEnumerator WaitToAttack(PlayerNode playerNode) // expand for more roles
    {
        float definitiveAttackTime = Time.time + 5;
        if (!isCurrentlyAggrivated.Value) definitiveAttackTime += UnityEngine.Random.Range(0, 30);

        int indexOfPlayerNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
        PlayerBehaviour targetPlayerBehaviour = playerNode.playerBehaviour;
        Node targetPlayerHallwayNode = GetHallwayNodeFromPlayerRole(targetPlayerBehaviour.playerRole);

        yield return targetPlayerBehaviour.IsFoxyReadyToAttack(targetPlayerHallwayNode, definitiveAttackTime);
        SetNode(targetPlayerHallwayNode, false);

        InitiateAttackClientRpc(indexOfPlayerNode);
        isWaitingOnClient = true;
    }

    private Node GetHallwayNodeFromPlayerRole(PlayerRoles playerRole)
    {
        return playerRole switch
        {
            PlayerRoles.SecurityOffice => securityOfficeHallwayNode,
            PlayerRoles.Janitor => janitorHallwayNode,// same as security office
            PlayerRoles.PartsAndService => partsAndServiceHallwayNode,
            PlayerRoles.Backstage => backstageHallwayNode,
            _ => null,
        };
    }

    [ClientRpc]
    private void InitiateAttackClientRpc(int indexOfPlayerNode)
    {
        animatronicModel.gameObject.SetActive(true);
        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes[indexOfPlayerNode];

        PlayAttackVisual(playerNode);
        StartCoroutine(PlayAttackAudio(playerNode));

        // if foxy is targeting the local player handle the logic
        if (playerNode.playerBehaviour == GameManager.localPlayerBehaviour) StartCoroutine(CheckPlayerCondition(playerNode));
    }

    private IEnumerator CheckPlayerCondition(PlayerNode playerNode)
    {
        int indexOfPlayerNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
        PlayerBehaviour targetPlayerBehaviour = playerNode.playerBehaviour;

        yield return new WaitForSeconds(1.7f);

        if (targetPlayerBehaviour.HasBlockedFoxy())
        {
            Blocked(indexOfPlayerNode, targetPlayerBehaviour);
        }
        else if (targetPlayerBehaviour.isPlayerAlive.Value)
        {
            KillPlayerServerRpc(indexOfPlayerNode);
        }
        else
        {
            ResetFoxyServerRpc();
        }
    }

    private void Blocked(int indexOfPlayerNode, PlayerBehaviour targetPlayerBehaviour)
    {
        bool playBlockAudio = targetPlayerBehaviour.playerRole != PlayerRoles.Janitor;

        PlayThunkAudio(targetPlayerBehaviour);
        OnFoxyPowerDrain.Invoke(targetPlayerBehaviour.playerRole, CalculatePowerDrain());
        ResetFoxyServerRpc(playBlockAudio, indexOfPlayerNode: indexOfPlayerNode);
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

        PlayScreamClientRpc(indexOfPlayerNode);
        playerNode.playerBehaviour.DieClientRpc(killerName, deathScream, MultiplayerManager.NewClientRpcSendParams(playerNode.playerBehaviour.OwnerClientId));

        SetNode(playerNode, true);

        yield return new WaitForSeconds(10);

        ResetFoxyServerRpc(indexOfPlayerNode: indexOfPlayerNode);
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
            foxyRunAudio = GameAudioManager.Instance.PlaySfxInterruptable("foxy run", true, 0.7f);
            yield return new WaitForSeconds(0.3f);
            foxyTauntAudio = GameAudioManager.Instance.PlaySfxInterruptable(isCurrentlyAggrivated.Value ? "foxy taunt" : "fire in the hole", false, 0.3f);

            foxyRunAudio.pitch = 1f + 0.01f * currentAttackAttempt.Value;
            foxyTauntAudio.pitch = 1f + 0.01f * currentAttackAttempt.Value;

            float elapsedTime = 0;

            while (elapsedTime < audioDuration)
            {
                if (foxyTauntAudio != null) foxyTauntAudio.volume = 1 - (elapsedTime / audioDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            foxyRunAudio = GameAudioManager.Instance.PlaySfxInterruptable("foxy run", true, 0);
            yield return new WaitForSeconds(0.3f);
            foxyTauntAudio = GameAudioManager.Instance.PlaySfxInterruptable(isCurrentlyAggrivated.Value ? "foxy taunt" : "fire in the hole", false, 0f);

            foxyRunAudio.pitch = 1f + 0.01f * currentAttackAttempt.Value;
            foxyTauntAudio.pitch = 1f + 0.01f * currentAttackAttempt.Value;

            float elapsedTime = 0;

            while (elapsedTime < audioDuration)
            {
                // start quiet and slowly get louder
                if (foxyRunAudio != null) foxyRunAudio.volume = (elapsedTime / audioDuration) + 0.3f;
                if (foxyTauntAudio != null) foxyTauntAudio.volume = (elapsedTime / audioDuration) + 0.2f;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void PlayAttackVisual(PlayerNode playerNode)
    {
        PlayerBehaviour playerBehaviour = playerNode.playerBehaviour;

        foxyAnimation.enabled = true;

        switch (playerBehaviour.playerRole) // expand for more roles
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
            case PlayerRoles.Janitor:
                GameManager.Instance.OnFoxyAttacking?.Invoke(securityOfficeHallwayNode);
                foxyAnimation.Play("Foxy Janitor Run");
                break;
        }
    }

    [ClientRpc]
    private void PlayThunkClientRpc(int indexOfPlayerNode = -1)
    {
        // Safety check: invalid index
        if (indexOfPlayerNode < 0 || indexOfPlayerNode >= AnimatronicManager.Instance.PlayerNodes.Count)
        {
            Debug.LogWarning($"[PlayThunkClientRpc] Invalid player node index: {indexOfPlayerNode}");
            return;
        }

        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes[indexOfPlayerNode];

        // Skip for owner of this animatronic or if spectating
        if (playerNode.playerBehaviour.IsOwner || GameManager.Instance.IsSpectating) return;

        PlayThunkAudio(playerNode.playerBehaviour, 0.3f);
    }

    public void PlayThunkAudio(PlayerBehaviour playerBehaviour, float volume = 1f)
    {
        // Stop conflicting SFX first
        GameAudioManager.Instance.StopSfx(foxyRunAudio);
        GameAudioManager.Instance.StopSfx(foxyTauntAudio);

        // If this is the Janitor and alive, and the thunk is happening to the Janitor
        bool localIsJanitor = GameManager.localPlayerBehaviour?.playerRole == PlayerRoles.Janitor;
        bool localIsAlive = GameManager.localPlayerBehaviour?.isPlayerAlive.Value == true;
        bool targetIsJanitor = playerBehaviour.playerRole == PlayerRoles.Janitor;

        if (localIsJanitor && localIsAlive && targetIsJanitor)
        {
            MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(1f);
        }

        // Only play thunk if it’s not already playing
        if (thunkAudio == null)
        {
            thunkAudio = GameAudioManager.Instance.PlaySfxInterruptable("thunk", true, volume);
        }
    }


    [ClientRpc]
    private void PlayScreamClientRpc(int indexOfPlayerNode = -1)
    {
        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes[indexOfPlayerNode];

        if (playerNode.playerBehaviour.IsOwner) return;
        if (GameManager.Instance.IsSpectating) return;

        GameAudioManager.Instance.PlaySfxInterruptable(deathScream, true, 0.1f); // exclude the person getting jumpscared
    }
}
