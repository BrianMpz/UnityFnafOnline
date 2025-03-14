using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class for all player roles, handling power, cameras, and death logic.
/// </summary>
public abstract class PlayerBehaviour : NetworkBehaviour
{
    [Header("Constant Attributes")]
    // constant attributes
    public PlayerRoles playerRole;
    public PlayerComputer playerComputer;
    public CameraController cameraController;
    public Camera playerCamera;
    public Camera spectatorCamera;
    public bool animatronicsCanStandInDoorway;
    [SerializeField] private protected float timeToWaitBeforeKill;


    [Header("Dynamic Attributes")]
    // dynamic attributes
    public NetworkVariable<float> currentPower = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> currentPowerUsage = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isPlayerAlive = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private protected NetworkVariable<bool> isPlayerPoweredOn = new(writePerm: NetworkVariableWritePermission.Owner);

    // subscribable events
    public Action OnPowerDown;
    public event Action OnPowerOn;
    public event Action OnInitialise;
    public event Action OnDisable;
    public event Action OnPlayerDeath;
    public event Action OnPlayerJumpscare;

    // abstract classes
    private protected abstract void UpdateCameraView();
    private protected abstract IEnumerator PlayDeathAnimation(string deathScream);
    public abstract IEnumerator WaitUntilKillConditionsAreMet(Node currentNode);
    public abstract bool IsPlayerVulnerable(Node currentNode);
    public abstract bool IsAnimatronicCloseToAttack(Node currentNode);

    void Start()
    {
        spectatorCamera.enabled = false; // make sure its only enabled when spectating this player

        if (!IsServer) return;
        currentPower.Value = 100f;
    }

    public virtual void Initialise()
    {
        if (!IsOwner) return;

        isPlayerAlive.Value = true;
        OnInitialise?.Invoke();

        PowerOn();

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        currentPower.OnValueChanged += CheckPowerValue;
        AnimatronicManager.Instance.foxy.OnFoxyPowerDrain += FoxyDrainPowerServerRpc;
    }

    public void Disable()
    {
        if (!IsOwner) return;

        isPlayerAlive.Value = false;
        OnDisable?.Invoke();
    }

    public virtual void Update()
    {
        HandlePlayerUpdate();
    }

    private void HandlePlayerUpdate()
    {
        if (IsServer) DrainPower();

        if (!isPlayerAlive.Value) return;

        if (!IsOwner) return;

        UpdateCameraView();
        UpdatePowerUsage();
    }

    public virtual void PowerOn()
    {
        if (!IsOwner) return;

        OnPowerOn?.Invoke();
        isPlayerPoweredOn.Value = true;
    }

    public virtual void PowerOff()
    {
        if (!IsOwner) return;
        if (!isPlayerAlive.Value) return;

        OnPowerDown?.Invoke();
        GameManager.Instance.OnPlayerPowerDownServerRpc(playerRole);

        GameAudioManager.Instance.StopAllSfx();
        GameAudioManager.Instance.PlaySfxInterruptable("power down");

        isPlayerPoweredOn.Value = false;
    }

    private void CheckPowerValue(float previousValue, float newValue)
    {
        // if the sign changes from pos to neg then power off
        if (previousValue > 0 && newValue <= 0)
        {
            PowerOff();
        }
    }

    protected float CalculatePowerDrain()
    {
        // this coefficient is arbitruary
        float fixedDrainCoefficient = 100f / 900f;
        return Time.deltaTime * fixedDrainCoefficient;
    }

    private void DrainPower()
    {
        if (!isPlayerPoweredOn.Value) return;

        float drainRate = CalculatePowerDrain();

        // drain power
        currentPower.Value -= drainRate * currentPowerUsage.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void FoxyDrainPowerServerRpc(PlayerRoles playerRole, float drainAmount)
    {
        if (this.playerRole != playerRole) return;
        if (this.playerRole == PlayerRoles.Janitor) return; // janitor doest have a door

        // drain power
        currentPower.Value -= drainAmount;
    }

    private IEnumerator PlayGoldenFreddyDeathAnimation()
    {
        if (!isPlayerAlive.Value) yield break;
        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();

        yield return MiscellaneousGameUI.Instance.gfJumpscareImage.PlayJumpscare();
    }

    [ClientRpc]
    public void DieClientRpc(FixedString64Bytes killer, FixedString64Bytes deathScream, ClientRpcParams _) => StartCoroutine(Die(killer, deathScream));
    public IEnumerator Die(FixedString64Bytes killer, FixedString64Bytes deathScream = default)
    {
        string killerName = killer.ToString();

        OnPlayerJumpscare?.Invoke();

        if (killerName == "Golden Freddy") yield return PlayGoldenFreddyDeathAnimation();
        else yield return PlayDeathAnimation(deathScream.ToString()); // each individual subclass determine this behaviour

        HandleDeath(killerName);

        // wait a sec before cutting mic
        yield return new WaitForSeconds(0.3f);
        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
    }

    public void HandleDeath(string killer)
    {
        // clean up after player death

        GameAudioManager.Instance.StopAllSfx();
        Disable();
        OnPlayerDeath?.Invoke();

        StartCoroutine(GameManager.Instance.HandleDeath(killer));
    }

    public IEnumerator DisconnectionDeathCleanUp()
    {
        // the player has disconnected during the came and needs to be cleaned up

        yield return new WaitUntil(() => { return IsOwner; });
        GameManager.Instance.RelayDeathServerRpc("disconnection");

        Disable();
        OnPlayerDeath?.Invoke();
        GameManager.Instance.CheckForGameOverServerRpc();
    }

    [ClientRpc]
    public virtual void PlayDoorKnockAudioClientRpc(int _, ClientRpcParams _0)
    {
        // by default just play the knocking sound without panning
        GameAudioManager.Instance.PlaySfxOneShot("door knock");
    }

    public virtual Node GetDoorwayNode(Node AttackingNode)
    {
        // if this base method is called then animatronicsCanStandInDoorway is set to true without setting a doorway node
        throw new Exception("animatronicsCanStandInDoorway is set to true without setting a doorway node!");
    }


    private protected virtual void UpdatePowerUsage()
    {
        if (!isPlayerAlive.Value) currentPowerUsage.Value = 1;
    }
}
