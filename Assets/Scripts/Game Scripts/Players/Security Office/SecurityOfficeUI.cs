using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SecurityOfficeUI : PlayerUI
{
    [SerializeField] private EventTrigger monitorFlip;
    [SerializeField] private EventTrigger monitorToggle;

    private float cameraFlipCooldownTime = 0.3f;
    private float timeSinceLastCameraFlip;

    private void Start()
    {
        AddListener(monitorFlip, EventTriggerType.PointerEnter, MonitorFlip);
        AddListener(monitorToggle, EventTriggerType.PointerEnter, CameraToggle);

        playerBehaviour.OnPowerOn += EnableFlip;
        playerBehaviour.OnPowerDown += DisableFlip;

        EnableFlip();
    }

    private void EnableFlip()
    {
        monitorFlip.enabled = true;
        monitorToggle.enabled = false;
        monitorToggle.GetComponent<Image>().raycastTarget = false;
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
        timeSinceLastCameraFlip += Time.deltaTime;
        base.Update();
    }

    private void MonitorFlip()
    {
        if (playerBehaviour.playerComputer.isLocked)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }
        if (timeSinceLastCameraFlip < cameraFlipCooldownTime) return;

        timeSinceLastCameraFlip = 0;

        playerBehaviour.playerComputer.ToggleMonitorFlip();
        DisableFlip();
    }

    public void CameraToggle()
    {
        EnableFlip();
    }

    public override void Show()
    {
        base.Show();
        if (GameManager.Instance.IsSpectating)
        {
            HideFlip();
        }
    }
}
