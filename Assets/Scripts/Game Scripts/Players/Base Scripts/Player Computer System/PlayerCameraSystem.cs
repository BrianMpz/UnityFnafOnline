using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCameraSystem : NetworkBehaviour
{
    [SerializeField] private PlayerComputer playerComputer;
    public NetworkVariable<bool> isWatchingFoxy = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<CameraName> currentCameraName = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private CameraStatic cameraStatic;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RawImage cameraOutputScreen;
    [SerializeField] private TMP_Text cameraDistrubanceText;
    [SerializeField] private TMP_Text accessDeniedText;
    [SerializeField] private TMP_Text audioOnlyText;

    private AudioSource cameraBootUpAudio;
    public event Action<CameraName> OnCameraViewChanged;
    private bool isHidingCurrentCamera;

    private void Start()
    {
        GameManager.Instance.OnAnimatronicMoved += GameManager_OnAnimatronicMoved;
        GameManager.Instance.OnFoxyStatusChanged += GameManager_OnFoxyStatusChanged;
        GameManager.Instance.OnFoxyAttacking += GameManager_OnFoxyAttacking;
        GlobalCameraSystem.Instance.OnCameraVisibilityChanged += GlobalCameraSystem_OnCameraVisibilityChanged;
    }

    public void Initialise(Camera playerCamera)
    {
        canvas.worldCamera = playerCamera;
        currentCameraName.Value = CameraName.One;
        Disable();
    }

    private void Update()
    {
        if (!GameManager.Instance.isPlaying || !IsOwner) return;

        CheckIsWatchingFoxy();
    }

    public void Enable()
    {
        if (!IsOwner) return;
        cameraStatic.staticaudio = GameAudioManager.Instance.PlaySfxInterruptable("static audio", 0, true);
        cameraStatic.disturbanceAudio = GameAudioManager.Instance.PlaySfxInterruptable("camera disturbance", 1, true);

        canvas.enabled = true;
        cameraOutputScreen.enabled = true;

        EnableServerRpc(); // for spectators

        BootUpCameras();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableServerRpc(ServerRpcParams serverRpcParams = default)
    => EnableClientRpc(serverRpcParams.Receive.SenderClientId);


    public void Disable()
    {
        if (!IsOwner) return;
        GameAudioManager.Instance.StopSfx(cameraBootUpAudio);
        GameAudioManager.Instance.StopSfx(cameraStatic.staticaudio);
        GameAudioManager.Instance.StopSfx(cameraStatic.disturbanceAudio);

        canvas.enabled = false;
        cameraOutputScreen.enabled = false;

        GlobalCameraSystem.Instance.DisableLights();

        DisableServerRpc(); // for spectators
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableServerRpc(ServerRpcParams serverRpcParams = default)
    => DisableClientRpc(serverRpcParams.Receive.SenderClientId);

    private void BootUpCameras()
    {
        cameraBootUpAudio = GameAudioManager.Instance.PlaySfxInterruptable("camera boot up");
        RefreshCameras();
    }

    public void SetCamera(CameraName cameraName)
    {
        if (!IsOwner) return;

        currentCameraName.Value = cameraName;

        CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(cameraName);
        PlayerRoles playerRole = playerComputer.playerBehaviour.playerRole;

        bool canSeeAnyCamera = playerRole == PlayerRoles.SecurityOffice;
        isHidingCurrentCamera = cameraData.isAudioOnly || (!canSeeAnyCamera && (cameraData.isCurrentlyHidden || cameraData.isSecurityOfficeOnly));

        UpdateCameraUI(cameraData);

        CheckIsWatchingFoxy();

        SetCameraServerRpc(cameraName); // for spectators

        EnableCurrentLights(cameraData);

        if (cameraStatic.disturbanceAudio != null)
            cameraStatic.disturbanceAudio.mute = !isHidingCurrentCamera;
    }

    public void EnableCurrentLights(CameraData cameraData)
    {
        PlayerRoles playerRole = playerComputer.playerBehaviour.playerRole;
        bool canSeeAnyCamera = playerRole == PlayerRoles.SecurityOffice;

        GlobalCameraSystem.Instance.DisableLights();
        cameraData.cameraFlashlight.enabled = true;

        if (canSeeAnyCamera)
        {
            cameraData.cameraFlashlight.intensity = cameraData.startingIntensity * 3;
            cameraOutputScreen.color = Color.green;
            cameraData.cameraFlashlight.range = cameraData.startingRange * 5f;
        }
        else
        {
            cameraData.cameraFlashlight.intensity = cameraData.startingIntensity;
            cameraOutputScreen.color = Color.white;
            cameraData.cameraFlashlight.range = cameraData.startingRange;
        }
    }

    private void CheckIsWatchingFoxy()
    {
        bool isOnCameraSystem = playerComputer.currentComputerScreen.Value == ComputerScreen.Cameras;
        bool isViewingPiratesCove = currentCameraName.Value == CameraName.Three;

        isWatchingFoxy.Value = playerComputer.isMonitorUp.Value && isOnCameraSystem && !isHidingCurrentCamera && isViewingPiratesCove;
    }

    private void UpdateCameraUI(CameraData cameraData)
    {
        cameraOutputScreen.texture = cameraData.GetRenderTexture();
        cameraDistrubanceText.enabled = false;
        accessDeniedText.enabled = false;
        audioOnlyText.enabled = false;

        cameraStatic.RefreshMonitorStatic(isHidingCurrentCamera);

        if (isHidingCurrentCamera)
        {
            if (cameraData.isSecurityOfficeOnly)
            {
                accessDeniedText.enabled = true;
            }
            else if (cameraData.isAudioOnly)
            {
                audioOnlyText.enabled = true;
            }
            else // isCurrentlyHidden
            {
                cameraDistrubanceText.enabled = true;
            }
        }

        OnCameraViewChanged?.Invoke(cameraData.GetCameraName());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCameraServerRpc(CameraName cameraName, ServerRpcParams serverRpcParams = default)
    => SetCameraClientRpc(cameraName, serverRpcParams.Receive.SenderClientId);

    private void GameManager_OnAnimatronicMoved(Node fromNode, Node toNode)
    {
        if (!playerComputer.isMonitorUp.Value) return;

        if (AreNodesVisibleOnCamera(fromNode, toNode))
        {
            RefreshCameras();
        }
    }

    private void GameManager_OnFoxyStatusChanged()
    {
        if (!playerComputer.isMonitorUp.Value) return;

        if (currentCameraName.Value == CameraName.Three) RefreshCameras();
    }

    private void GameManager_OnFoxyAttacking(Node startPositionNode)
    {
        if (!playerComputer.isMonitorUp.Value) return;

        if (IsNodeVisibleOnCamera(startPositionNode)) RefreshCameras();
    }

    private void GlobalCameraSystem_OnCameraVisibilityChanged(CameraName changedCameraName)
    {
        if (!playerComputer.isMonitorUp.Value) return;

        if (currentCameraName.Value == changedCameraName) RefreshCameras();
    }

    public bool CheckIfAnyoneWatchingHallwayNode(Node startPositionNode)
    {
        if (!playerComputer.isMonitorUp.Value) return false;

        return IsNodeVisibleOnCamera(startPositionNode);
    }

    private bool AreNodesVisibleOnCamera(Node fromNode, Node toNode)
    {
        if (isHidingCurrentCamera) return false;
        CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(currentCameraName.Value);

        // Check if either node is visible in the current camera's visible nodes
        return cameraData.nodesVisibleOnCamera.Contains(fromNode) || cameraData.nodesVisibleOnCamera.Contains(toNode);
    }

    private bool IsNodeVisibleOnCamera(Node node)
    {
        if (isHidingCurrentCamera) return false;
        CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(currentCameraName.Value);

        // Check if node is visible in the current camera's visible nodes
        return cameraData.nodesVisibleOnCamera.Contains(node);
    }

    private void RefreshCameras()
    {
        SetCamera(currentCameraName.Value);
    }

    /// for spectators

    [ClientRpc]
    private void EnableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        canvas.enabled = true;
        cameraOutputScreen.enabled = true;
    }

    [ClientRpc]
    private void DisableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        canvas.enabled = false;
        cameraOutputScreen.enabled = false;

        if (!GameManager.Instance.IsSpectating || SpectatorUI.Instance.GetCurrentSpectator() != playerComputer.playerBehaviour) return;
        GlobalCameraSystem.Instance.DisableLights();
    }

    [ClientRpc]
    public void SetCameraClientRpc(CameraName cameraName, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(cameraName);

        bool isHidden = cameraData.isCurrentlyHidden;
        bool canSeeAnyCamera = MultiplayerManager.Instance.GetPlayerDataFromClientId(ignoreId).role == PlayerRoles.SecurityOffice;
        isHidingCurrentCamera = (!canSeeAnyCamera && (isHidden || cameraData.isSecurityOfficeOnly)) || cameraData.isAudioOnly;

        UpdateCameraUI(cameraData);

        if (!GameManager.Instance.IsSpectating || SpectatorUI.Instance.GetCurrentSpectator() != playerComputer.playerBehaviour) return;

        EnableCurrentLights(cameraData);
    }
}
