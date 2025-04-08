using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerComputer : NetworkBehaviour
{
    /* ───────────────────────────────── COMPONENTS ───────────────────────────────── */

    public PlayerBehaviour playerBehaviour;

    [Header("Computer Systems")]
    public PlayerCameraSystem playerCameraSystem;
    public PlayerCommunicationSystem playerCommunicationSystem;
    public PlayerMotionDetectionSystem playerMotionDetectionSystem;
    public PlayerAudioLureSystem playerAudioLureSystem;
    public PlayerGameSystem playerGameSystem;
    public PlayerManual playerManual;

    [Header("UI & Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private Canvas selectorCanvas;
    [SerializeField] private Canvas defaultCanvas;

    /* ───────────────────────────────── NETWORK VARIABLES ───────────────────────────────── */

    public NetworkVariable<ComputerScreen> currentComputerScreen = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isMonitorUp = new(writePerm: NetworkVariableWritePermission.Owner);

    /* ───────────────────────────────── STATE VARIABLES ───────────────────────────────── */

    public bool isMonitorAlwaysUp;
    public bool isLocked;
    private bool isWaitingForAnimationToFinish;

    /* ───────────────────────────────── EVENTS ───────────────────────────────── */

    public Action<bool> OnMonitorFlipFinished;
    public Action<ComputerScreen> OnComputerScreenChanged;

    private void Start()
    {
        selectorCanvas.enabled = true;
        defaultCanvas.enabled = false;

        playerBehaviour.OnInitialise += Initialise;
        playerBehaviour.OnPowerOn += PlayerBehaviour_OnPowerOn;
        playerBehaviour.OnPowerDown += PlayerBehaviour_OnPowerDown;
        playerBehaviour.OnPlayerJumpscare += PlayerBehaviour_OnJumpscare;
    }

    public void Initialise()
    {
        selectorCanvas.worldCamera = playerBehaviour.playerCamera;
        selectorCanvas.enabled = false;
        defaultCanvas.enabled = false;

        playerCameraSystem.Initialise(playerBehaviour.playerCamera);
        playerCommunicationSystem.Initialise(playerBehaviour.playerCamera);
        playerMotionDetectionSystem.Initialise(playerBehaviour.playerCamera);
        playerAudioLureSystem.Initialise(playerBehaviour.playerCamera);
        playerGameSystem.Initialise(playerBehaviour.playerCamera);
        playerManual.Initialise(playerBehaviour.playerCamera);

        currentComputerScreen.Value = ComputerScreen.Manual;
    }

    public void PlayerBehaviour_OnPowerOn()
    {
        Unlock();
    }

    public void PlayerBehaviour_OnPowerDown()
    {
        ForceMonitorDown();
        Lock();
    }

    public void PlayerBehaviour_OnJumpscare()
    {
        ForceMonitorDown();
        DisableComputerSystem();
    }

    public void ToggleMonitorFlip()
    {
        if (isLocked)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }

        FlipCamera();
    }

    private void ForceMonitorDown()
    {
        if (isMonitorAlwaysUp) return;

        if (isMonitorUp.Value) FlipCamera();
    }

    private void FlipCamera()
    {
        TriggerFlipAnimation(!isMonitorUp.Value);
        isWaitingForAnimationToFinish = true;
        GameAudioManager.Instance.PlaySfxOneShot("camera flip");
    }

    public void TriggerFlipAnimation(bool flip)
    {
        if (animator == null) return;
        animator.SetBool("FlipUp", flip);
        TriggerFlipAnimationServerRpc(flip);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerFlipAnimationServerRpc(bool flip, ServerRpcParams serverRpcParams = default)
        => TriggerFlipAnimationClientRpc(flip, serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TriggerFlipAnimationClientRpc(bool flip, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        if (animator == null) return;

        animator.SetBool("FlipUp", flip);
    }

    // after an animation
    public void MonitorFlipFinished()
    {
        if (!IsOwner) return;
        if (!isWaitingForAnimationToFinish) return;
        isWaitingForAnimationToFinish = false;

        OnMonitorFlip();
        OnMonitorFlipFinished?.Invoke(isMonitorUp.Value);
    }

    private void OnMonitorFlip()
    {
        if (!isMonitorUp.Value) // flip up
        {
            EnableComputerSystem();
        }
        else // flip down
        {
            DisableComputerSystem();
        }
    }

    public void Lock()
    {
        isLocked = true;
    }

    private void Unlock()
    {
        isLocked = false;
    }

    public void EnableComputerSystem()
    {
        isMonitorUp.Value = true;
        SetComputerScreen(currentComputerScreen.Value);
    }

    public void DisableComputerSystem()
    {
        isMonitorUp.Value = isMonitorAlwaysUp;
        DisableAllComputerScreens();
    }

    public void SetComputerScreen(ComputerScreen computerScreen)
    {
        DisableAllComputerScreens();

        currentComputerScreen.Value = computerScreen;

        selectorCanvas.enabled = true;
        defaultCanvas.enabled = true;

        switch (currentComputerScreen.Value)
        {
            case ComputerScreen.Cameras:
                playerCameraSystem.Enable();
                break;
            case ComputerScreen.Comms:
                playerCommunicationSystem.Enable();
                break;
            case ComputerScreen.MotionDetection:
                playerMotionDetectionSystem.Enable();
                break;
            case ComputerScreen.AudioLure:
                playerAudioLureSystem.Enable();
                break;
            case ComputerScreen.Games:
                playerGameSystem.Enable();
                break;
            case ComputerScreen.Manual:
                playerManual.Enable();
                break;
        }

        OnComputerScreenChanged?.Invoke(currentComputerScreen.Value);
    }

    private void DisableAllComputerScreens()
    {
        playerCameraSystem.Disable();
        playerCommunicationSystem.Disable();
        playerMotionDetectionSystem.Disable();
        playerAudioLureSystem.Disable();
        playerGameSystem.Disable();
        playerManual.Disable();

        selectorCanvas.enabled = false;
        defaultCanvas.enabled = false;
    }
}

public enum ComputerScreen
{
    Cameras,
    Comms,
    MotionDetection,
    AudioLure,
    Games,
    Manual,
}
