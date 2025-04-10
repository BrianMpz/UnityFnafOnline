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
    [SerializeField] private ScreenSelectButton[] screenSelectButtons;

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
        DisableComputerSystem();

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

        if (isMonitorAlwaysUp)
        {
            isMonitorUp.Value = true;
            EnableComputerSystem();
        }
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
        if (isMonitorUp.Value && !isMonitorAlwaysUp) FlipCamera();
    }

    private void FlipCamera()
    {
        GameAudioManager.Instance.PlaySfxOneShot("camera flip");
        isMonitorUp.Value = !isMonitorUp.Value;

        TriggerFlipAnimation(isMonitorUp.Value);
        isWaitingForAnimationToFinish = true;
    }

    public void TriggerFlipAnimation(bool flipUp)
    {
        if (animator == null) return;

        animator.SetBool("FlipUp", flipUp);
        TriggerFlipAnimationServerRpc(flipUp);
    }

    [ServerRpc(RequireOwnership = true)]
    private void TriggerFlipAnimationServerRpc(bool flipUp, ServerRpcParams serverRpcParams = default) => TriggerFlipAnimationClientRpc(flipUp, serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TriggerFlipAnimationClientRpc(bool flipUp, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        if (animator == null) return;

        animator.SetBool("FlipUp", flipUp);
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
        if (isMonitorUp.Value) // flip up
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
        SetComputerScreen(currentComputerScreen.Value);
    }

    public void DisableComputerSystem()
    {
        DisableAllComputerScreens();
    }

    public void SetComputerScreen(ComputerScreen computerScreen)
    {
        if (!IsOwner) return;

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
