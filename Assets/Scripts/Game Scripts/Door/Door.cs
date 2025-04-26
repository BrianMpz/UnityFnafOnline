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
    public Node doorwayNode; // null if the player doesnt have a doorway

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
            GameAudioManager.Instance.PlaySfxOneShot("button error", true);
            return;
        }

        if (timeSinceLastDoorToggle < doorToggleCooldownTime) return;

        ToggleDoor(!isDoorClosed.Value);
    }

    private void ToggleDoor(bool isClosingDoor, bool playAudio = true)
    {
        if (!IsOwner) return;

        if (playAudio) GameAudioManager.Instance.PlaySfxOneShot("door toggle", true);

        isDoorClosed.Value = isClosingDoor;
        timeSinceLastDoorToggle = 0f;

        TriggerDoorAnimation(isClosingDoor);

        if (isClosingDoor)
        {
            doorButton.TurnOn();
        }
        else
        {
            doorButton.TurnOff();
        }
    }

    public void TriggerDoorAnimation(bool isClosingDoor)
    {
        bool isAnimatronicInDoorway = isClosingDoor && doorwayNode != null && doorwayNode.isOccupied.Value;
        if (isAnimatronicInDoorway) return;

        animator.SetBool("Closed", isClosingDoor);
        TriggerDoorAnimationServerRpc(isClosingDoor);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerDoorAnimationServerRpc(bool isClosingDoor, ServerRpcParams serverRpcParams = default)
        => TriggerDoorAnimationClientRpc(isClosingDoor, serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void TriggerDoorAnimationClientRpc(bool isClosingDoor, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        animator.SetBool("Closed", isClosingDoor);
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
