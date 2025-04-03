using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackstageUI : PlayerUI
{
    [SerializeField] private BackstagePlayerBehaviour backstagePlayerBehaviour;
    [SerializeField] private BackstageCameraController cameraController;
    [SerializeField] private EventTrigger turnLeftTrigger;
    [SerializeField] private EventTrigger turnRightTrigger;

    private void Awake()
    {
        AddListener(turnLeftTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(TurnLeft)); });
        AddListener(turnRightTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(TurnRight)); });
    }

    private void TurnRight()
    {
        BackstageCameraController_View viewToSet = default;

        if (cameraController.currentView.Value == BackstageCameraController_View.MonitorView) viewToSet = BackstageCameraController_View.MaintenanceView;
        else if (cameraController.currentView.Value == BackstageCameraController_View.MaintenanceView) viewToSet = BackstageCameraController_View.ShockView;
        else if (cameraController.currentView.Value == BackstageCameraController_View.ShockView) viewToSet = BackstageCameraController_View.DoorView;
        else if (cameraController.currentView.Value == BackstageCameraController_View.DoorView) viewToSet = BackstageCameraController_View.MonitorView;

        cameraController.SetCameraView(viewToSet);
    }

    private void TurnLeft()
    {
        BackstageCameraController_View viewToSet = default;

        if (cameraController.currentView.Value == BackstageCameraController_View.MonitorView) viewToSet = BackstageCameraController_View.DoorView;
        else if (cameraController.currentView.Value == BackstageCameraController_View.MaintenanceView) viewToSet = BackstageCameraController_View.MonitorView;
        else if (cameraController.currentView.Value == BackstageCameraController_View.ShockView) viewToSet = BackstageCameraController_View.MaintenanceView;
        else if (cameraController.currentView.Value == BackstageCameraController_View.DoorView) viewToSet = BackstageCameraController_View.ShockView;

        cameraController.SetCameraView(viewToSet);
    }

    private IEnumerator WaitThenChangeTriggers(Action action)
    {
        action();

        turnLeftTrigger.enabled = false;
        turnRightTrigger.enabled = false;

        yield return new WaitForSeconds(0.3f);

        turnLeftTrigger.enabled = true;
        turnRightTrigger.enabled = true;
    }
}
