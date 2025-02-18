using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerComputer : NetworkBehaviour
{
    public PlayerCameraSystem playerCameraSystem;
    public PlayerCommunicationSystem playerCommunicationSystem;
    public NetworkVariable<ComputerScreen> currentComputerScreen = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private PlayerBehaviour playerBehaviour;
    [SerializeField] private Animator animator;
    [SerializeField] private Canvas screenSelectorCanvas;

    public NetworkVariable<bool> isMonitorUp = new(writePerm: NetworkVariableWritePermission.Owner);
    public Action<bool> OnMonitorFlipFinished;
    public Action<ComputerScreen> OnComputerScreenChanged;

    public bool isLocked;
    private bool isWaitingForAnimationToFinish;

    private void Start()
    {
        playerBehaviour.OnInitialise += Initialise;
        playerBehaviour.OnPowerOn += PlayerBehaviour_OnPowerOn;
        playerBehaviour.OnPowerDown += PlayerBehaviour_OnPowerDown;
        playerBehaviour.OnKill += PlayerBehaviour_OnKill;
    }

    public void Initialise()
    {
        playerCameraSystem.Initialise();
        playerCommunicationSystem.Initialise();
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

    public void PlayerBehaviour_OnKill()
    {
        DisableComputerSystem();
        ForceMonitorDown();
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
        if (isMonitorUp.Value) FlipCamera();
    }

    private void FlipCamera()
    {
        isWaitingForAnimationToFinish = true;
        GameAudioManager.Instance.PlaySfxOneShot("camera flip");
        TriggerFlipAnimation(!isMonitorUp.Value);
    }

    public void TriggerFlipAnimation(bool flip)
    {
        if (animator != null) animator.SetBool("FlipUp", flip);
        TriggerFlipAnimationServerRpc(flip);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerFlipAnimationServerRpc(bool flip, ServerRpcParams serverRpcParams = default)
        => TriggerFlipAnimationClientRpc(flip, serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TriggerFlipAnimationClientRpc(bool flip, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        if (animator != null) animator.SetBool("FlipUp", flip);
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
        screenSelectorCanvas.enabled = true;
        SetComputerScreen(currentComputerScreen.Value);
    }

    public void DisableComputerSystem()
    {
        isMonitorUp.Value = false;
        screenSelectorCanvas.enabled = false;
        DisableAllComputerScreens();
    }

    public void SetComputerScreen(ComputerScreen computerScreen)
    {
        DisableAllComputerScreens();

        currentComputerScreen.Value = computerScreen;

        switch (currentComputerScreen.Value)
        {
            case ComputerScreen.Cameras:
                playerCameraSystem.Enable();
                break;
            case ComputerScreen.Comms:
                playerCommunicationSystem.Enable();
                break;
        }

        OnComputerScreenChanged?.Invoke(currentComputerScreen.Value);
    }

    private void DisableAllComputerScreens()
    {
        playerCameraSystem.Disable();
        playerCommunicationSystem.Disable();
    }
}

public enum ComputerScreen
{
    Cameras,
    Comms
}
