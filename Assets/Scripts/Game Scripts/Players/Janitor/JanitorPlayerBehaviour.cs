using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class JanitorPlayerBehaviour : PlayerBehaviour
{

    [Header("Specialised Variables")]
    [SerializeField] private JanitorCameraController janitorCameraController;
    public Animator mask;
    public NetworkVariable<float> oxygenLevels = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> recognitionPossibility = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isMaskDown = new(writePerm: NetworkVariableWritePermission.Owner);
    public bool canToggle;
    [SerializeField] private float triggerCooldownTime = 0.3f;
    [SerializeField] private float timeSinceLastTrigger;
    [SerializeField] private Light RoomLight;
    private AudioSource breathingSfx;
    public Node insideNode;

    public override bool IsPlayerVulnerable(Node currentNode)
    {
        return false; // handled differently for janitor
    }

    public override IEnumerator WaitUntilKillConditionsAreMet(Node currentNode)
    {
        yield break;
        // kill straight away
    }

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        if (!isPlayerAlive.Value) yield break;

        GameAudioManager.Instance.StopAllSfx();

        mask.SetBool("down", false);
        TriggerMaskAnimationServerRpc(false);

        oxygenLevels.Value = 100;
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream, false);

        float elapsedTime = 0;

        while (elapsedTime < .55f)
        {
            janitorCameraController.LerpTowardsDeathView();
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        GameAudioManager.Instance.StopSfx(audioSource);
    }

    private protected override void UpdatePowerUsage()
    {
        currentPowerUsage.Value = 0;

        if (playerComputer.isMonitorUp.Value) currentPowerUsage.Value += 1;
        if (playerComputer.playerMotionDetectionSystem.IsTracking) currentPowerUsage.Value += 2;

        if (PowerGenerator.Instance.GetIsCharging(playerRole).Value) currentPowerUsage.Value -= 5;

        if (ultraPowerDrain.Value) currentPowerUsage.Value += 49;

        base.UpdatePowerUsage();
    }

    private protected override void UpdateCameraView()
    {
        // camera view is static so dont implement
    }

    public override void PowerOff()
    {
        if (!IsOwner) return;
        if (!isPlayerAlive.Value) return;

        OnPowerDown?.Invoke();
        isPlayerPoweredOn.Value = false;
        GameManager.Instance.OnPlayerPowerDownServerRpc(playerRole);
    }

    public override void PowerOn()
    {
        base.PowerOn();
        GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", false, 0.5f, true);
    }

    public override void Update()
    {
        base.Update();

        if (!GameManager.Instance.isPlaying) return;

        RoomLight.enabled = PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.Janitor);

        if (!IsOwner) return;
        if (!isPlayerAlive.Value) return;

        if (timeSinceLastTrigger < triggerCooldownTime)
            timeSinceLastTrigger += Time.deltaTime;
        else canToggle = true;

        if (isMaskDown.Value)
        {
            oxygenLevels.Value -= 10f * Time.deltaTime;
        }
        else
        {
            oxygenLevels.Value += 0.5f * Time.deltaTime;
        }

        oxygenLevels.Value = Mathf.Max(oxygenLevels.Value, 0f);

    }

    public void ResetCooldown()
    {
        timeSinceLastTrigger = triggerCooldownTime; // Instantly allows toggle
        canToggle = true;
    }

    public void MonitorTrigger()
    {
        if (timeSinceLastTrigger < triggerCooldownTime || !canToggle) return;

        timeSinceLastTrigger = 0f;
        canToggle = false;

        if (playerComputer.isLocked)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error", true);
            return;
        }

        playerComputer.ToggleMonitorFlip();
    }

    public void MaskTrigger()
    {
        if (timeSinceLastTrigger < triggerCooldownTime || !canToggle) return;

        timeSinceLastTrigger = 0f;
        canToggle = false;
        isMaskDown.Value = !isMaskDown.Value;

        if (isMaskDown.Value)
        {
            mask.SetBool("down", true);
            TriggerMaskAnimationServerRpc(true);
            GameAudioManager.Instance.PlaySfxOneShot("mask down", true);
            breathingSfx = GameAudioManager.Instance.PlaySfxInterruptable("deep breaths", false, 0.7f, true);
        }
        else
        {
            mask.SetBool("down", false);
            TriggerMaskAnimationServerRpc(false);
            GameAudioManager.Instance.PlaySfxOneShot("mask up", true);
            GameAudioManager.Instance.StopSfx(breathingSfx);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerMaskAnimationServerRpc(bool b, ServerRpcParams serverRpcParams = default)
        => TriggerMaskAnimationClientRpc(b, serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TriggerMaskAnimationClientRpc(bool b, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        mask.SetBool("down", b);
    }

    public override void Initialise()
    {
        base.Initialise();

        if (!IsOwner) return;

        oxygenLevels.Value = 100;
        oxygenLevels.OnValueChanged += CheckOxygenValue;

        insideNode.isOccupied.OnValueChanged += CheckInsideNodeForAnimatronicEntry;
        insideNode.isOccupied.OnValueChanged += CheckInsideNodeForFreddyEntry;
    }

    private void CheckInsideNodeForFreddyEntry(bool previousValue, bool newValue)
    {
        if (!isPlayerAlive.Value) return;

        if (newValue)
        {
            Freddy freddy = AnimatronicManager.Instance.freddy;
            if (insideNode.occupier == freddy) Hallucinations.Instance.StartHallucination(freddy.currentMovementWaitTime.Value + 1f);
        }
    }

    private void CheckInsideNodeForAnimatronicEntry(bool previousValue, bool newValue)
    {
        if (!isPlayerAlive.Value) return;

        if (newValue)
        {
            GameAudioManager.Instance.PlaySfxOneShot("janitor door open", true);
        }
    }

    [ClientRpc]
    public override void PlayDoorKnockAudioClientRpc(int _, bool _0, ClientRpcParams _2)
    {
        if (!isPlayerAlive.Value) return;

        GameAudioManager.Instance.PlaySfxOneShot("janitor door close", true);
        MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(1f);
        recognitionPossibility.Value = 0; // 0.5s grace period
    }

    public IEnumerator HandleRecognitionLogic(float difficulty, RecognitionResult result)
    {
        float timeToLeaveWithMask = Mathf.Lerp(2.5f, 12, difficulty / 20) + UnityEngine.Random.Range(-2f, 2f);
        float leniency = Mathf.Lerp(3, 0.5f, difficulty / 20);

        Debug.Log($"[Recognition] Started with difficulty {difficulty:F2}, timeToLeaveWithMask: {timeToLeaveWithMask:F2}, leniency: {leniency:F2}");

        yield return new WaitForSeconds(leniency);
        Debug.Log($"[Recognition] Grace period ended after {leniency:F2} seconds");

        float elapsedTimeWithoutMask = 0;
        float elapsedTimeWithMask = 0;

        bool canKill = false;
        bool shouldLeave = false;

        while (!canKill && !shouldLeave)
        {
            yield return null;

            if (isMaskDown.Value)
            {
                elapsedTimeWithMask += Time.deltaTime;

                if (elapsedTimeWithMask >= timeToLeaveWithMask)
                {
                    shouldLeave = true;
                }
            }
            else
            {
                elapsedTimeWithoutMask += Time.deltaTime;
                recognitionPossibility.Value = Mathf.Pow(elapsedTimeWithoutMask / (3 * leniency), 2);

                if (recognitionPossibility.Value > 1)
                {
                    canKill = true;
                }
            }
        }

        if (shouldLeave)
        {
            float rng = UnityEngine.Random.value;
            canKill = rng <= recognitionPossibility.Value;
            Debug.Log($"[Recognition] Animatronic chose to leave. RNG: {rng:F2} vs Possibility: {recognitionPossibility.Value:F2} => {(canKill ? "KILL" : "SPARED")}");
        }

        result.Value = canKill;

        if (canKill)
        {
            Debug.Log("[Recognition] Final result: Player will be killed.");
        }
        else
        {
            Debug.Log("[Recognition] Final result: Player survived the encounter.");
        }
    }

    private void CheckOxygenValue(float previousValue, float newValue)
    {
        // if the sign changes from pos to neg then power off
        if (previousValue > 0 && newValue <= 0)
        {
            StartCoroutine(Die("Golden Freddy"));
        }
    }

    public override bool IsAnimatronicCloseToAttack(Node currentNode)
    {
        if (currentNode == insideNode) return true;

        return false;
    }

    public override bool CanGoldenFreddySpawnIn()
    {
        throw new Exception("Golden Freddy cant be active while the Janitor is alive!");
    }

    public override bool HasSpottedGoldenFreddy()
    {
        throw new Exception("Golden Freddy cant be active while the Janitor is alive!");
    }

    public override bool HasLookedAwayFromGoldenFreddy()
    {
        throw new Exception("Golden Freddy cant be active while the Janitor is alive!");
    }

    public override bool HasBlockedFoxy()
    {
        return isMaskDown.Value;
    }

    public override IEnumerator IsFoxyReadyToAttack(Node hallwayNode, float definitiveAttackTime)
    {
        yield return new WaitUntil(() => !hallwayNode.isOccupied.Value && (Time.time > definitiveAttackTime || GlobalCameraSystem.Instance.CheckIfAnyoneWatchingHallwayNode(hallwayNode)));
    }

    public override void GetGameCollectable()
    {
        oxygenLevels.Value += 5;
    }
}
