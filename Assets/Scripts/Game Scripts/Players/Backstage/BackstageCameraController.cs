using System;
using Unity.Netcode;
using UnityEngine;

public class BackstageCameraController : CameraController
{
    [SerializeField] private BackstagePlayerBehaviour backstagePlayerBehaviour;
    public NetworkVariable<BackstageCameraController_View> currentView = new(writePerm: NetworkVariableWritePermission.Owner);
    private Transform CurrentView;
    [SerializeField] private Transform MonitorView;
    [SerializeField] private Transform DoorView;
    [SerializeField] private Transform MaintenanceView;
    [SerializeField] private Transform ShockView;
    [SerializeField] private Transform DeathView;
    [SerializeField] private float cameraLerpSpeed;
    public Action ViewChanged;

    public void SetCameraView(BackstageCameraController_View view)
    {
        backstagePlayerBehaviour.door.doorLight.DisableLights();
        currentView.Value = view;

        Transform viewTransform = GetViewFromEnum(view);
        CurrentView = viewTransform;

        ViewChanged?.Invoke();
        backstagePlayerBehaviour.zap.CheckIsBeingWatchedServerRpc(view == BackstageCameraController_View.ShockView);
    }

    private Transform GetViewFromEnum(BackstageCameraController_View view)
    {
        return view switch
        {
            BackstageCameraController_View.MonitorView => MonitorView,
            BackstageCameraController_View.ShockView => ShockView,
            BackstageCameraController_View.MaintenanceView => MaintenanceView,
            BackstageCameraController_View.DoorView => DoorView,
            _ => null,
        };
    }

    public override void SetCameraView()
    {
        if (!canPlayerControlCamera || CurrentView == null) return;
        Vector3 currentRotation = playerView.transform.eulerAngles;
        currentRotation.z = Mathf.LerpAngle(currentRotation.z, CurrentView.eulerAngles.z, Time.deltaTime * cameraLerpSpeed);
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, CurrentView.eulerAngles.y, Time.deltaTime * cameraLerpSpeed);
        currentRotation.x = Mathf.LerpAngle(currentRotation.x, CurrentView.eulerAngles.x, Time.deltaTime * cameraLerpSpeed);
        playerView.eulerAngles = currentRotation;

        Vector3 currentPosition = playerView.transform.position;
        currentPosition.z = Mathf.Lerp(currentPosition.z, CurrentView.position.z, Time.deltaTime * cameraLerpSpeed);
        currentPosition.y = Mathf.Lerp(currentPosition.y, CurrentView.position.y, Time.deltaTime * cameraLerpSpeed);
        currentPosition.x = Mathf.Lerp(currentPosition.x, CurrentView.position.x, Time.deltaTime * cameraLerpSpeed);
        playerView.position = currentPosition;
    }

    public override void LerpTowardsDeathView()
    {
        Vector3 currentRotation = playerView.transform.eulerAngles;
        currentRotation.z = Mathf.LerpAngle(currentRotation.z, DeathView.eulerAngles.z, Time.deltaTime * cameraLerpSpeed);
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, DeathView.eulerAngles.y, Time.deltaTime * cameraLerpSpeed);
        currentRotation.x = Mathf.LerpAngle(currentRotation.x, DeathView.eulerAngles.x, Time.deltaTime * cameraLerpSpeed);
        playerView.eulerAngles = currentRotation;

        Vector3 currentPosition = playerView.transform.position;
        currentPosition = Vector3.Lerp(currentPosition, DeathView.position, Time.deltaTime * cameraLerpSpeed);
        playerView.position = currentPosition;
    }

    private protected override void Initialise()
    {
        base.Initialise();
        CurrentView = MonitorView;
        cam.fieldOfView = 60;
    }
}

public enum BackstageCameraController_View
{
    MonitorView,
    ShockView,
    MaintenanceView,
    DoorView,
}
