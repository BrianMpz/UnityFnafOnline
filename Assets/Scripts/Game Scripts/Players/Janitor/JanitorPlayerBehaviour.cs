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
    public NetworkVariable<float> animatronicRecognitionPossibility = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isMaskDown = new(writePerm: NetworkVariableWritePermission.Owner);
    public bool canToggle;
    [SerializeField] private float triggerCooldownTime = 0.3f;
    [SerializeField] private float timeSinceLastTrigger;
    [SerializeField] private Light RoomLight;
    private AudioSource breathingSfx;
    public Node insideNode;

    public override bool IsPlayerVulnerable(Node currentNode)
    {
        return !isMaskDown.Value || UnityEngine.Random.Range(0f, 1f) <= animatronicRecognitionPossibility.Value;
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
        mask.gameObject.SetActive(false);
        oxygenLevels.Value = 100;
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable(deathScream);

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
        GameAudioManager.Instance.PlaySfxInterruptable("ambiance 1", 0.5f, true);
    }

    public override void Update()
    {
        base.Update();

        RoomLight.enabled = PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(PlayerRoles.Janitor);

        if (!IsOwner) return;
        if (!isPlayerAlive.Value) return;

        if (timeSinceLastTrigger < triggerCooldownTime)
            timeSinceLastTrigger += Time.deltaTime;
        else canToggle = true;

        if (isMaskDown.Value)
        {
            oxygenLevels.Value -= 8 * Time.deltaTime;
        }
        else
        {
            oxygenLevels.Value += 0.1f * Time.deltaTime;
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
            GameAudioManager.Instance.PlaySfxOneShot("button error");
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
            GameAudioManager.Instance.PlaySfxOneShot("mask down");
            breathingSfx = GameAudioManager.Instance.PlaySfxInterruptable("deep breaths", 0.7f, true);
        }
        else
        {
            mask.SetBool("down", false);
            TriggerMaskAnimationServerRpc(false);
            GameAudioManager.Instance.PlaySfxOneShot("mask up");
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
    }

    private void CheckInsideNodeForAnimatronicEntry(bool previousValue, bool newValue)
    {
        if (!isPlayerAlive.Value) return;
        if (newValue)
        {
            GameAudioManager.Instance.PlaySfxOneShot("janitor door open");
            StartCoroutine(CalculateRecognitionPossibility());
        }
    }

    private IEnumerator CalculateRecognitionPossibility()
    {
        for (float timeElapsed = 0f; timeElapsed < 4.99f; timeElapsed += Time.deltaTime)
        {
            // Check if the denominator is zero to avoid division by zero
            if (Mathf.Approximately(timeElapsed - 5f, 0f))
            {
                yield break; // Exit the coroutine if denominator would be zero
            }

            if (isMaskDown.Value)
            {
                continue;
            }

            // Calculate the recognition possibility using the given formula
            animatronicRecognitionPossibility.Value = Mathf.Clamp((0.3f / Mathf.Pow(timeElapsed - 5f, 2)) - 0.01481f, 0, 1); // 0.5s grace period

            yield return null;
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

    [ClientRpc]
    public override void PlayDoorKnockAudioClientRpc(int _, ClientRpcParams _0)
    {
        GameAudioManager.Instance.PlaySfxOneShot("janitor door close");
        MiscellaneousGameUI.Instance.gameFadeInUI.FadeOut(1f);
        animatronicRecognitionPossibility.Value = 0; // 0.5s grace period
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
