using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Door : NetworkBehaviour
{
    public PlayerBehaviour playerBehaviour;
    public DoorButton doorButton;
    public DoorLight doorLight;
    [SerializeField] private Animator animator;
    public NetworkVariable<bool> isDoorClosed = new(writePerm: NetworkVariableWritePermission.Owner);
    private float timeSinceLastDoorToggle;
    private float doorToggleCooldownTime = 0.2f;
    public bool isLocked;
    public Node linkedNode;

    private void Awake()
    {
        playerBehaviour.OnPowerOn += PlayerBehaviour_OnPowerOn;
        playerBehaviour.OnPowerDown += PlayerBehaviour_OnPowerDown;
        playerBehaviour.OnPlayerDeath += PlayerBehaviour_OnDeath;
        playerBehaviour.OnInitialise += Initialise;
    }

    private void Initialise()
    {
        ToggleDoor(false, false);
        doorLight.DisableLights();
        doorButton.TurnOff();
    }

    private void PlayerBehaviour_OnPowerOn()
    {
        Unlock();
    }

    private void PlayerBehaviour_OnPowerDown()
    {
        ToggleDoor(false);
        doorLight.DisableLights();
        Lock();
    }

    private void PlayerBehaviour_OnDeath()
    {
        ToggleDoor(false, false);
        Lock();
    }

    private void Update()
    {
        if (!GameManager.Instance.isPlaying) return;

        timeSinceLastDoorToggle += Time.deltaTime;
    }

    public void ToggleDoor()
    {
        if (isLocked)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }

        if (timeSinceLastDoorToggle < doorToggleCooldownTime) return;

        ToggleDoor(!isDoorClosed.Value);
    }

    private void ToggleDoor(bool close, bool playAudio = true)
    {
        if (!IsOwner) return;

        if (playAudio) GameAudioManager.Instance.PlaySfxOneShot("door toggle");

        isDoorClosed.Value = close;
        timeSinceLastDoorToggle = 0f;

        TriggerDoorAnimation(close);

        if (close)
        {
            doorButton.TurnOn();
        }
        else
        {
            doorButton.TurnOff();
        }
    }

    public void TriggerDoorAnimation(bool close)
    {
        animator.SetBool("Closed", close);
        TriggerDoorAnimationServerRpc(close);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerDoorAnimationServerRpc(bool close, ServerRpcParams serverRpcParams = default)
        => TriggerDoorAnimationClientRpc(close, serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TriggerDoorAnimationClientRpc(bool close, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        animator.SetBool("Closed", close);
    }

    public void Lock()
    {
        isLocked = true;
    }

    private void Unlock()
    {
        isLocked = false;
    }

}
