using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCameraSystem : NetworkBehaviour
{
    [SerializeField] private PlayerBehaviour playerBehaviour;
    public NetworkVariable<bool> isWatchingFoxy = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<CameraName> currentCameraName = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private CameraStatic cameraStatic;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RawImage cameraOutputScreen;
    [SerializeField] private PlayerComputer playerComputer;
    [SerializeField] private TMP_Text cameraDistrubanceText;
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

    public void Initialise()
    {
        currentCameraName.Value = CameraName.One;
        Disable();
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

        isWatchingFoxy.Value = false;
        GlobalCameraSystem.Instance.CountPlayersWatchingFoxyServerRpc();

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
        cameraOutputScreen.texture = cameraData.GetRenderTexture();
        OnCameraViewChanged?.Invoke(cameraName);

        bool isHidden = cameraData.isHidden;
        bool canSeeAnyCamera = playerBehaviour.playerRole == PlayerRoles.SecurityOffice;
        isHidingCurrentCamera = !canSeeAnyCamera && isHidden;

        UpdateCameraUI();

        isWatchingFoxy.Value = !isHidingCurrentCamera && currentCameraName.Value == CameraName.Three;
        GlobalCameraSystem.Instance.CountPlayersWatchingFoxyServerRpc();

        SetCameraServerRpc(cameraName, isHidden); // for spectators

        GlobalCameraSystem.Instance.DisableLights();
        cameraData.cameraFlashlight.enabled = true;
    }

    private void UpdateCameraUI()
    {
        cameraStatic.RefreshMonitorStatic(isHidingCurrentCamera);
        cameraDistrubanceText.enabled = isHidingCurrentCamera;
        if (cameraStatic.disturbanceAudio != null)
            cameraStatic.disturbanceAudio.mute = !isHidingCurrentCamera;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCameraServerRpc(CameraName cameraName, bool isHidden, ServerRpcParams serverRpcParams = default)
    => SetCameraClientRpc(cameraName, isHidden, serverRpcParams.Receive.SenderClientId);

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

        if (!GameManager.Instance.IsSpectating || SpectatorUI.Instance.GetCurrentSpectator() != playerBehaviour) return;
        GlobalCameraSystem.Instance.DisableLights();
    }

    [ClientRpc]
    public void SetCameraClientRpc(CameraName cameraName, bool isHidden, ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        bool canSeeAnyCamera = MultiplayerManager.Instance.GetPlayerDataFromClientId(ignoreId).role == PlayerRoles.SecurityOffice;

        CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(cameraName);
        cameraOutputScreen.texture = cameraData.GetRenderTexture();

        bool isHidingCurrentCamera = !canSeeAnyCamera && isHidden;

        cameraStatic.RefreshMonitorStatic(isHidingCurrentCamera);
        cameraDistrubanceText.enabled = isHidingCurrentCamera;
        OnCameraViewChanged?.Invoke(cameraName);

        if (!GameManager.Instance.IsSpectating || SpectatorUI.Instance.GetCurrentSpectator() != playerBehaviour) return;

        GlobalCameraSystem.Instance.DisableLights();
        cameraData.cameraFlashlight.enabled = true;
    }
}
