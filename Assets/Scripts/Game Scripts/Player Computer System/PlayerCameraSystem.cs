using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCameraSystem : NetworkBehaviour
{
    [SerializeField] private PlayerComputer playerComputer;
    public NetworkVariable<CameraName> currentCameraName = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private CameraStatic cameraStatic;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RawImage cameraOutputScreen;
    [SerializeField] private TMP_Text cameraDistrubanceText;
    [SerializeField] private TMP_Text accessDeniedText;
    [SerializeField] private TMP_Text audioOnlyText;
    [SerializeField] private TMP_Text currentCameraNameText;

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

        cameraOutputScreen.enabled = PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(playerComputer.playerBehaviour.playerRole);
        cameraStatic.enabled = PlayerRoleManager.Instance.IsSpectatingOrControllingPlayer(playerComputer.playerBehaviour.playerRole);
    }

    public void Enable()
    {
        if (!IsOwner) return;
        if (!gameObject.activeSelf) return;

        cameraStatic.staticaudio = GameAudioManager.Instance.PlaySfxInterruptable("static audio", false, 0, true);
        cameraStatic.disturbanceAudio = GameAudioManager.Instance.PlaySfxInterruptable("camera disturbance", true, 1, true);

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
        if (!gameObject.activeSelf) return;

        GameAudioManager.Instance.StopSfx(cameraBootUpAudio);
        GameAudioManager.Instance.StopSfx(cameraStatic.staticaudio);
        GameAudioManager.Instance.StopSfx(cameraStatic.disturbanceAudio);

        canvas.enabled = false;
        cameraOutputScreen.enabled = false;

        GlobalCameraSystem.Instance.DisableAllCameraComponents();

        DisableServerRpc(); // for spectators
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableServerRpc(ServerRpcParams serverRpcParams = default)
    => DisableClientRpc(serverRpcParams.Receive.SenderClientId);

    private void BootUpCameras()
    {
        cameraBootUpAudio = GameAudioManager.Instance.PlaySfxInterruptable("camera boot up", false);
        RefreshCameras();
    }

    public void SetCamera(CameraName cameraName)
    {
        if (!IsOwner) return;
        if (!gameObject.activeSelf) return;

        currentCameraName.Value = cameraName;

        CameraData cameraData = GlobalCameraSystem.Instance.GetCameraDataFromCameraName(cameraName);
        PlayerRoles playerRole = playerComputer.playerBehaviour.playerRole;

        bool canSeeAnyCamera = playerRole == PlayerRoles.SecurityOffice;
        isHidingCurrentCamera = cameraData.isAudioOnly || (!canSeeAnyCamera && (cameraData.isCurrentlyHidden || cameraData.isSecurityOfficeOnly));

        UpdateCameraUI(cameraData);

        SetCameraServerRpc(cameraName); // for spectators

        EnableCamera(cameraData, canSeeAnyCamera);

        if (cameraStatic.disturbanceAudio != null)
            cameraStatic.disturbanceAudio.mute = !isHidingCurrentCamera;
    }

    private void EnableCamera(CameraData cameraData, bool canSeeAnyCamera)
    {
        GlobalCameraSystem.Instance.EnableCameraComponent(cameraData);

        if (canSeeAnyCamera)
        {
            cameraData.cameraFlashlight.intensity = cameraData.startingIntensity * 2;
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

    private void UpdateCameraUI(CameraData cameraData)
    {
        cameraOutputScreen.texture = cameraData.GetRenderTexture();
        cameraDistrubanceText.enabled = false;
        accessDeniedText.enabled = false;
        audioOnlyText.enabled = false;
        currentCameraNameText.text = $"-{cameraData.room}-";

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

    public bool CanSeeCameras(bool canMonitorBeDown = false)
    {
        bool isOnCameraSystem = playerComputer.currentComputerScreen.Value == ComputerScreen.Cameras;
        bool isMonitorUp = playerComputer.isMonitorUp.Value;
        bool isPlayerAlive = playerComputer.playerBehaviour.isPlayerAlive.Value;

        return isPlayerAlive && isOnCameraSystem && !isHidingCurrentCamera && (isMonitorUp || canMonitorBeDown);
    }

    public bool IsWatchingCamera(CameraName targetCamera)
    {
        bool isViewingTargetCamera = currentCameraName.Value == targetCamera;

        return CanSeeCameras() && isViewingTargetCamera;
    }

    private void GameManager_OnAnimatronicMoved(Node fromNode, Node toNode)
    {
        if (IsNodeVisibleOnCamera(fromNode) || IsNodeVisibleOnCamera(toNode)) RefreshCameras();
    }

    private void GameManager_OnFoxyStatusChanged()
    {
        if (IsWatchingCamera(CameraName.Three)) RefreshCameras();
    }

    private void GameManager_OnFoxyAttacking(Node startPositionNode)
    {
        if (IsNodeVisibleOnCamera(startPositionNode)) RefreshCameras();
    }

    private void GlobalCameraSystem_OnCameraVisibilityChanged(CameraName changedCameraName)
    {
        if (IsWatchingCamera(changedCameraName)) RefreshCameras();
    }

    public bool CheckIfAnyoneWatchingHallwayNode(Node startPositionNode)
    {
        return IsNodeVisibleOnCamera(startPositionNode);
    }

    public bool IsNodeVisibleOnCamera(Node node, bool canMonitorBeDown = false)
    {
        if (!CanSeeCameras(canMonitorBeDown)) return false;

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

        if (PlayerRoleManager.Instance.IsSpectatingPlayer(playerComputer.playerBehaviour.playerRole)) GlobalCameraSystem.Instance.DisableAllCameraComponents();
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

        if (PlayerRoleManager.Instance.IsSpectatingPlayer(playerComputer.playerBehaviour.playerRole)) GlobalCameraSystem.Instance.EnableCameraComponent(cameraData);
    }
}
