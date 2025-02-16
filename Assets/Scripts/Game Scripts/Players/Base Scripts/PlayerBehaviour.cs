using System;
using System.Collections;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class PlayerBehaviour : NetworkBehaviour
{
    public PlayerComputer playerComputer;
    public PlayerRoles playerRole;
    public PlayerUI playerUI;
    public Camera cam;
    public NetworkVariable<bool> isAlive = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> power = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> powerUsage = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> poweredOn = new(writePerm: NetworkVariableWritePermission.Owner);

    public event Action OnPowerDown;
    public event Action OnPowerOn;
    public event Action OnInitialise;
    public event Action OnDisable;
    public event Action OnDeath;
    public event Action OnKill;
    public event Action OnFoxyPowerDrain;
    public bool hasADoorWay;

    public abstract void SetUsage();
    public abstract void SetCameraView();
    public abstract bool IsCameraUp();
    public abstract bool IsVulnerable(Node currentNode);
    public virtual Node GetDoorwayNode(Node AttackingNode) { return default; }
    public abstract IEnumerator WaitToKill(Node currentNode);
    private protected abstract IEnumerator DeathAnimation();
    [ClientRpc] public virtual void KnockOnDoorClientRpc(int indexOfCurrentNode, ClientRpcParams clientRpcParams) { }

    public void Initialise()
    {
        if (!IsOwner) return;

        isAlive.Value = true;
        OnInitialise?.Invoke();

        power.Value = 100f;

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

        if (previousSign >= 0 && newSign < 0)
        {
            PowerOff();
        }
        else if (previousSign < 0 && newSign >= 0)
        {
            PowerOn();
        }
    }

    private void Update()
    {
        if (GameManager.localPlayerBehaviour != this || !isAlive.Value) return;

        SetCameraView();
        SetUsage();

        if (!poweredOn.Value) return;

        DrainPower();
    }

    protected float CalculatePowerDrain()
    {
        return 100f * Time.deltaTime / 800f;
    }

    private void DrainPower()
    {
        float drainRate = CalculatePowerDrain();

        power.Value = Mathf.Max(-1, power.Value - (drainRate * powerUsage.Value));
    }

    public void FoxyDrainPower(float drainAmount)
    {
        power.Value -= drainAmount;
        OnFoxyPowerDrain.Invoke();
    }

    [ClientRpc]
    public void DieClientRpc(FixedString64Bytes killer, ClientRpcParams clientRpcParams) => StartCoroutine(Die(killer.ToString()));

    private IEnumerator Die(string killer)
    {
        if (GameManager.localPlayerBehaviour != this) yield break;

        OnKill?.Invoke();

        GameAudioManager.Instance.StopAllSfx();

        yield return DeathAnimation();

        HandleDeath(killer);

        yield return new WaitForSeconds(0.3f);

        if (MultiplayerManager.isPlayingOnline) VivoxManager.Instance.SwitchToLobbyChat();
    }

    public void HandleDeath(string killer)
    {
        StartCoroutine(GameManager.Instance.HandleDeath(killer));

        StartCoroutine(DeathCleanUp(false));
    }

    public IEnumerator DeathCleanUp(bool disconnection)
    {
        yield return new WaitUntil(() => { return IsOwner; });
        if (disconnection) GameManager.Instance.RelayDeathServerRpc("disconnection");

        Disable();
        OnDeath?.Invoke();
    }
}
