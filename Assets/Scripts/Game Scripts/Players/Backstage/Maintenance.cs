using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Maintenance : NetworkSingleton<Maintenance>
{
    [SerializeField] private BackstagePlayerBehaviour playerBehaviour;
    [SerializeField] private BackstageCameraController backstageCameraController;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button rebootCommunicationButton;
    [SerializeField] private Button rebootCamerasButton;
    [SerializeField] private Button rebootPowerGeneratorButton;
    [SerializeField] private Button rebootAllButton;
    [SerializeField] private TMP_Text communicationStatus;
    [SerializeField] private TMP_Text cameraStatus;
    [SerializeField] private TMP_Text powerGeneratorStatus;
    private float currentDifficulty;

    public NetworkVariable<State> communicationsState = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<State> camerasState = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<State> powerGeneratorState = new(writePerm: NetworkVariableWritePermission.Owner);

    private Coroutine restoreSystemCoroutine;

    private bool isRebooting = false;

    void Start()
    {
        GameManager.Instance.OnGameStarted += Initialise;
        communicationsState.OnValueChanged += CommunicationsStateChanged;
        camerasState.OnValueChanged += CamerasStateChanged;
        powerGeneratorState.OnValueChanged += PowerGeneratorStateChanged;
    }

    void Initialise()
    {
        if (!IsOwner) return;

        communicationsState.Value = State.OFFLINE;
        camerasState.Value = State.OFFLINE;
        powerGeneratorState.Value = State.OFFLINE;

        playerBehaviour.OnPowerOn += PowerOn;
        playerBehaviour.OnPowerDown += PowerOff;

        rebootCommunicationButton.onClick.AddListener(() => restoreSystemCoroutine = StartCoroutine(RebootSystem(SystemType.Comms, Random.Range(8, 12))));
        rebootCamerasButton.onClick.AddListener(() => restoreSystemCoroutine = StartCoroutine(RebootSystem(SystemType.Cameras, Random.Range(6, 15))));
        rebootPowerGeneratorButton.onClick.AddListener(() => restoreSystemCoroutine = StartCoroutine(RebootSystem(SystemType.PowerGenerator, Random.Range(5, 12))));
        rebootAllButton.onClick.AddListener(() => restoreSystemCoroutine = StartCoroutine(RebootAllSystems(Random.Range(12, 24))));

        backstageCameraController.ViewChanged += CancelReboot;

        PowerOn();
        EnableButtons();

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty = 1;
                break;
            case GameNight.Two:
                currentDifficulty = 4;
                break;
            case GameNight.Three:
                currentDifficulty = 7;
                break;
            case GameNight.Four:
                currentDifficulty = 10;
                break;
            case GameNight.Five:
                currentDifficulty = 13;
                break;
            case GameNight.Six:
                currentDifficulty = 16;
                break;
            case GameNight.Seven:
                currentDifficulty = 20;
                break;
        }
        StartCoroutine(RandomlyDisableSystems());
    }

    private void CancelReboot()
    {
        if (!isRebooting) return;

        StopCoroutine(restoreSystemCoroutine);
        isRebooting = false;

        EnableButtons();
        CancelRebootingStates();

        GameAudioManager.Instance.PlaySfxOneShot("failed sfx");
    }

    private void CommunicationsStateChanged(State _, State newValue)
    {
        UpdateStatus(newValue, communicationStatus);
    }

    private void CamerasStateChanged(State _, State newValue)
    {
        UpdateStatus(newValue, cameraStatus);
    }

    private void PowerGeneratorStateChanged(State _, State newValue)
    {
        UpdateStatus(newValue, powerGeneratorStatus);
    }

    private void UpdateStatus(State state, TMP_Text statusText)
    {
        if (state == State.ONLINE)
        {
            statusText.text = "ONLINE";
            statusText.color = Color.green;
        }
        else if (state == State.REBOOTING)
        {
            statusText.text = "REBOOTING...";
            statusText.color = Color.yellow;
        }
        else if (state == State.OFFLINE)
        {
            statusText.text = "CORRUPTED";
            statusText.color = Color.red;
        }
    }

    private IEnumerator RebootSystem(SystemType systemType, float rebootLength)
    {
        if (isRebooting) yield break; // Prevent multiple reboots at the same time

        GameAudioManager.Instance.PlaySfxOneShot("camera blip");
        SetSystemState(systemType, State.REBOOTING);
        DisableButtons();
        isRebooting = true;

        yield return new WaitForSeconds(rebootLength);

        GameAudioManager.Instance.PlaySfxOneShot("camera blip");
        SetSystemState(systemType, State.ONLINE);
        EnableButtons();
        isRebooting = false;

        //Debug.Log($"[Maintenance] {systemType} restored.");
    }

    private IEnumerator RebootAllSystems(float rebootLength)
    {
        if (isRebooting) yield break; // Prevent multiple reboots at the same time

        GameAudioManager.Instance.PlaySfxOneShot("camera blip");
        SetAllSystemsState(State.REBOOTING);
        DisableButtons();
        isRebooting = true;

        yield return new WaitForSeconds(rebootLength);

        GameAudioManager.Instance.PlaySfxOneShot("camera blip");
        SetAllSystemsState(State.ONLINE);

        EnableButtons();
        isRebooting = false;

        //Debug.Log($"[Maintenance] All sytems restored.");
    }

    private void SetSystemState(SystemType systemType, State newState)
    {
        switch (systemType)
        {
            case SystemType.Comms:
                communicationsState.Value = newState;
                break;
            case SystemType.Cameras:
                camerasState.Value = newState;
                break;
            case SystemType.PowerGenerator:
                powerGeneratorState.Value = newState;
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)] public void SetAllSystemsStateServerRpc(State newState) { SetAllSystemsStateClientRpc(newState, MultiplayerManager.NewClientRpcSendParams(OwnerClientId)); }
    [ClientRpc] private void SetAllSystemsStateClientRpc(State newState, ClientRpcParams clientRpcParams) => SetAllSystemsState(newState);

    private void SetAllSystemsState(State newState)
    {
        SetSystemState(SystemType.Cameras, newState);
        SetSystemState(SystemType.Comms, newState);
        SetSystemState(SystemType.PowerGenerator, newState);
    }

    private void CancelRebootingStates()
    {
        if (communicationsState.Value == State.REBOOTING) SetSystemState(SystemType.Comms, State.OFFLINE);
        if (camerasState.Value == State.REBOOTING) SetSystemState(SystemType.Cameras, State.OFFLINE);
        if (powerGeneratorState.Value == State.REBOOTING) SetSystemState(SystemType.PowerGenerator, State.OFFLINE);
    }

    private State GetSystemState(SystemType systemType)
    {
        return systemType switch
        {
            SystemType.Comms => communicationsState.Value,
            SystemType.Cameras => camerasState.Value,
            SystemType.PowerGenerator => powerGeneratorState.Value,
            _ => State.ONLINE,
        };
    }

    private void DisableButtons()
    {
        rebootCommunicationButton.interactable = false;
        rebootCamerasButton.interactable = false;
        rebootPowerGeneratorButton.interactable = false;
        rebootAllButton.interactable = false;
    }

    private void EnableButtons()
    {
        rebootCommunicationButton.interactable = true;
        rebootCamerasButton.interactable = true;
        rebootPowerGeneratorButton.interactable = true;
        rebootAllButton.interactable = true;
    }

    private IEnumerator RandomlyDisableSystems()
    {
        while (GameManager.Instance.isPlaying)
        {
            yield return new WaitForSeconds(Mathf.Lerp(120f, 30f, currentDifficulty / 20));

            if (communicationsState.Value == State.ONLINE && Random.value < 0.1f)
            {
                SetSystemState(SystemType.Comms, State.OFFLINE);
                StartCoroutine(AutoRestoreAfterDelay(SystemType.Comms, 60f));
            }

            if (camerasState.Value == State.ONLINE && Random.value < 0.7f)
            {
                SetSystemState(SystemType.Cameras, State.OFFLINE);
                StartCoroutine(AutoRestoreAfterDelay(SystemType.Cameras, 60f));
            }

            if (powerGeneratorState.Value == State.ONLINE && Random.value < 0.2f)
            {
                SetSystemState(SystemType.PowerGenerator, State.OFFLINE);
                StartCoroutine(AutoRestoreAfterDelay(SystemType.PowerGenerator, 60f));
            }
        }
    }

    private IEnumerator AutoRestoreAfterDelay(SystemType systemType, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Restore the system to ONLINE only if it is still offline
        if (GetSystemState(systemType) == State.OFFLINE)
        {
            SetSystemState(systemType, State.ONLINE);
            Debug.Log($"[Maintenance] {systemType} auto-restored to ONLINE after {delay} seconds.");
        }
    }

    private void PowerOn()
    {
        canvas.enabled = true;

        communicationsState.Value = State.ONLINE;
        camerasState.Value = State.ONLINE;
        powerGeneratorState.Value = State.ONLINE;
    }

    private void PowerOff()
    {
        canvas.enabled = false;
    }

}

public enum SystemType
{
    Comms,
    Cameras,
    PowerGenerator
}

public enum State
{
    ONLINE,
    OFFLINE,
    REBOOTING
}
