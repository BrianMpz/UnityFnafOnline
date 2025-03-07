using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartsAndServiceUI : PlayerUI
{
    [SerializeField] private PartsAndServiceCameraController cameraController;

    [SerializeField] private EventTrigger monitorFlip;
    [SerializeField] private EventTrigger monitorToggle;

    [SerializeField] private EventTrigger laptopToDoorTrigger;
    [SerializeField] private EventTrigger doorToLaptopTrigger;

    [SerializeField] private EventTrigger laptopToGeneratorTrigger;
    [SerializeField] private EventTrigger generatorToLaptopTrigger;
    public Action OnViewChanged;

    private float cameraFlipCooldownTime = 0.3f;
    private float timeSinceLastCameraFlip;

    private void Awake()
    {
        AddListener(monitorToggle, EventTriggerType.PointerEnter, CameraToggle);
        AddListener(monitorFlip, EventTriggerType.PointerEnter, MonitorFlip);

        AddListener(laptopToDoorTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(LaptopToDoorTrigger)); });
        AddListener(doorToLaptopTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(DoorToLaptopTrigger)); });

        AddListener(laptopToGeneratorTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(LaptopToGeneratorTrigger)); });
        AddListener(generatorToLaptopTrigger, EventTriggerType.PointerEnter, () => { StartCoroutine(WaitThenChangeTriggers(GeneratorToLaptopTrigger)); });

        playerBehaviour.OnPowerOn += EnableFlip;
        playerBehaviour.OnPowerDown += DisableFlip;

        DoorToLaptopTrigger();
        EnableFlip();
    }

    private void EnableFlip()
    {
        if (cameraController.CurrentView == cameraController.LaptopView)
        {
            monitorFlip.enabled = true;
            monitorToggle.enabled = false;
            monitorToggle.GetComponent<Image>().raycastTarget = false;
        }
    }

    private void DisableFlip()
    {
        monitorFlip.enabled = false;
        monitorToggle.enabled = true;
        monitorToggle.GetComponent<Image>().raycastTarget = true;
    }

    private void HideFlip()
    {
        monitorFlip.gameObject.SetActive(false);
        monitorToggle.gameObject.SetActive(false);
    }

    public override void Update()
    {
        base.Update();
        timeSinceLastCameraFlip += Time.deltaTime;
    }

    private void MonitorFlip()
    {
        if (timeSinceLastCameraFlip < cameraFlipCooldownTime) return;
        if (playerBehaviour.playerComputer.isLocked)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }

        timeSinceLastCameraFlip = 0;

        playerBehaviour.playerComputer.ToggleMonitorFlip();
        playerBehaviour.playerComputer.OnMonitorFlipFinished += PlayerComputer_OnMonitorFlipFinished;

        DisableFlip();
        DisableLookTriggers();
    }

    private void PlayerComputer_OnMonitorFlipFinished(bool isMonitorUp)
    {
        if (isMonitorUp)
        {
            DisableLookTriggers();
        }
        else
        {
            DoorToLaptopTrigger();
        }
    }

    public void CameraToggle()
    {
        EnableFlip();
    }

    private IEnumerator WaitThenChangeTriggers(Action action)
    {
        OnViewChanged?.Invoke();

        action();

        laptopToDoorTrigger.enabled = false;
        doorToLaptopTrigger.enabled = false;
        laptopToGeneratorTrigger.enabled = false;
        generatorToLaptopTrigger.enabled = false;

        yield return new WaitForSeconds(0.3f);

        laptopToDoorTrigger.enabled = true;
        doorToLaptopTrigger.enabled = true;
        laptopToGeneratorTrigger.enabled = true;
        generatorToLaptopTrigger.enabled = true;

    }

    private void LaptopToDoorTrigger()
    {
        monitorFlip.GetComponent<Image>().enabled = false;
        monitorToggle.GetComponent<Image>().enabled = false;

        cameraController.SetCameraView(cameraController.DoorView);

        laptopToDoorTrigger.GetComponent<Image>().enabled = false;
        doorToLaptopTrigger.GetComponent<Image>().enabled = true;

        laptopToGeneratorTrigger.GetComponent<Image>().enabled = false;
        generatorToLaptopTrigger.GetComponent<Image>().enabled = false;
    }

    private void LaptopToGeneratorTrigger()
    {
        monitorFlip.GetComponent<Image>().enabled = false;
        monitorToggle.GetComponent<Image>().enabled = false;

        cameraController.SetCameraView(cameraController.GeneratorView);

        laptopToDoorTrigger.GetComponent<Image>().enabled = false;
        doorToLaptopTrigger.GetComponent<Image>().enabled = false;

        laptopToGeneratorTrigger.GetComponent<Image>().enabled = false;
        generatorToLaptopTrigger.GetComponent<Image>().enabled = true;
    }

    private void DoorToLaptopTrigger()
    {
        monitorFlip.GetComponent<Image>().enabled = true;
        monitorToggle.GetComponent<Image>().enabled = true;

        EnableFlip();

        cameraController.SetCameraView(cameraController.LaptopView);

        laptopToDoorTrigger.GetComponent<Image>().enabled = true;
        doorToLaptopTrigger.GetComponent<Image>().enabled = false;

        laptopToGeneratorTrigger.GetComponent<Image>().enabled = true;
        generatorToLaptopTrigger.GetComponent<Image>().enabled = false;
    }

    private void GeneratorToLaptopTrigger()
    {
        monitorFlip.GetComponent<Image>().enabled = true;
        monitorToggle.GetComponent<Image>().enabled = true;

        EnableFlip();

        cameraController.SetCameraView(cameraController.LaptopView);

        laptopToDoorTrigger.GetComponent<Image>().enabled = true;
        doorToLaptopTrigger.GetComponent<Image>().enabled = false;

        laptopToGeneratorTrigger.GetComponent<Image>().enabled = true;
        generatorToLaptopTrigger.GetComponent<Image>().enabled = false;
    }

    private void DisableLookTriggers()
    {
        laptopToDoorTrigger.GetComponent<Image>().enabled = false;
        doorToLaptopTrigger.GetComponent<Image>().enabled = false;
        laptopToGeneratorTrigger.GetComponent<Image>().enabled = false;
        generatorToLaptopTrigger.GetComponent<Image>().enabled = false;
    }

    public override void Show()
    {
        base.Show();
        if (GameManager.Instance.IsSpectating)
        {
            DisableLookTriggers();
            HideFlip();
        }
    }
}
