using System;
using System.Collections;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public abstract class PlayerBehaviour : NetworkBehaviour
{
    public PlayerRoles playerRole;
    public PlayerComputer playerComputer;
    public Camera playerCamera;
    public Camera spectatorCamera;
    public NetworkVariable<bool> isAlive = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> power = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> powerUsage = new(writePerm: NetworkVariableWritePermission.Owner);
    private protected NetworkVariable<bool> poweredOn = new(writePerm: NetworkVariableWritePermission.Owner);

    public event Action OnPowerDown;
    public event Action OnPowerOn;
    public event Action OnInitialise;
    public event Action OnDisable;
    public event Action OnDeath;
    public event Action OnKill;
    public Action OnFoxyPowerDrain;
    public bool hasADoorWay;

    public abstract void SetUsage();
    public abstract void SetCameraView();
    public abstract bool IsCameraUp();
    public abstract bool IsVulnerable(Node currentNode);
    public virtual Node GetDoorwayNode(Node AttackingNode) { return default; }
    public abstract IEnumerator WaitToKill(Node currentNode);
    private protected abstract IEnumerator DeathAnimation(string deathScream);
    [ClientRpc] public virtual void KnockOnDoorClientRpc(int indexOfCurrentNode, ClientRpcParams clientRpcParams) { }

    void Start()
    {
        if (IsServer) power.Value = 100f;
        spectatorCamera.enabled = false;
    }

    public void Initialise()
    {
        if (!IsOwner) return;

        isAlive.Value = true;
        OnInitialise?.Invoke();

        PowerOn();
        power.OnValueChanged += CheckPowerValue;
    }

    public void Disable()
    {
        if (!IsOwner) return;

        isAlive.Value = false;
        OnDisable?.Invoke();
    }

    public virtual void PowerOn()
    {
        if (!IsOwner) return;

        OnPowerOn?.Invoke();
        poweredOn.Value = true;
    }

    public virtual void PowerOff()
    {
        if (!IsOwner) return;

        OnPowerDown?.Invoke();
        GameManager.Instance.OnPlayerPowerDownServerRpc(playerRole);
        GameAudioManager.Instance.StopAllSfx();
        GameAudioManager.Instance.PlaySfxInterruptable("power down");

        poweredOn.Value = false;
    }

    private void CheckPowerValue(float previousValue, float newValue)
    {
        float previousSign = Mathf.Sign(previousValue);
        float newSign = Mathf.Sign(newValue);

        if (previousSign > 0 && newSign <= 0)
        {
            PowerOff();
        }
    }

    public virtual void Update()
    {
        if (IsServer) DrainPower();

        if (GameManager.localPlayerBehaviour != this || !isAlive.Value) return;

        SetCameraView();
        SetUsage();
    }

    protected float CalculatePowerDrain()
    {
        return 100f * Time.deltaTime / 800f;
    }

    private void DrainPower()
    {
        if (!poweredOn.Value) return;

        float drainRate = CalculatePowerDrain();

        power.Value = Mathf.Max(0, power.Value - (drainRate * powerUsage.Value));
    }

    [ServerRpc(RequireOwnership = false)]
    public void FoxyDrainPowerServerRpc(float drainAmount)
    {
        power.Value -= drainAmount;
    }

    [ClientRpc]
    public void DieClientRpc(FixedString64Bytes killer, FixedString64Bytes deathScream, ClientRpcParams clientRpcParams) => StartCoroutine(Die(killer.ToString(), deathScream.ToString()));

    private IEnumerator Die(string killer, string deathScream)
    {
        if (GameManager.localPlayerBehaviour != this) yield break;

        OnKill?.Invoke();

        GameAudioManager.Instance.StopAllSfx();

        yield return DeathAnimation(deathScream);

        HandleDeath(killer);

        yield return new WaitForSeconds(0.3f);

        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
    }

    public void HandleDeath(string killer)
    {
        GameAudioManager.Instance.StopAllSfx();
        StartCoroutine(DeathCleanUp(false));
        StartCoroutine(GameManager.Instance.HandleDeath(killer));
    }

    public IEnumerator DeathCleanUp(bool disconnection)
    {
        yield return new WaitUntil(() => { return IsOwner; });
        if (disconnection) GameManager.Instance.RelayDeathServerRpc("disconnection");

        Disable();
        OnDeath?.Invoke();
    }

    public void X()
    {
        // Do something
        XServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void XServerRpc(ServerRpcParams serverRpcParams = default) => XClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void XClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        // do something
    }
}
