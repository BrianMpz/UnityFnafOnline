using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackstageUI : PlayerUI
{
    [SerializeField] private BackstageCameraController cameraController;
    [SerializeField] private EventTrigger turnLeftTrigger;
    [SerializeField] private EventTrigger turnRightTrigger;

    private void Start()
    {
        AddListener(turnLeftTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(TurnLeft)); });
        AddListener(turnRightTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(TurnRight)); });
    }

    private void TurnRight()
    {
        Transform viewToSet = null;

        if (cameraController.CurrentView == cameraController.MonitorView) viewToSet = cameraController.MaintenanceView;
        else if (cameraController.CurrentView == cameraController.MaintenanceView) viewToSet = cameraController.ShockView;
        else if (cameraController.CurrentView == cameraController.ShockView) viewToSet = cameraController.DoorView;
        else if (cameraController.CurrentView == cameraController.DoorView) viewToSet = cameraController.MonitorView;

        cameraController.SetCameraView(viewToSet);
    }

    private void TurnLeft()
    {
        Transform viewToSet = null;

        if (cameraController.CurrentView == cameraController.MonitorView) viewToSet = cameraController.DoorView;
        else if (cameraController.CurrentView == cameraController.MaintenanceView) viewToSet = cameraController.MonitorView;
        else if (cameraController.CurrentView == cameraController.ShockView) viewToSet = cameraController.MaintenanceView;
        else if (cameraController.CurrentView == cameraController.DoorView) viewToSet = cameraController.ShockView;

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
