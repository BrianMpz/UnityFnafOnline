using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JanitorUI : PlayerUI
{
    [SerializeField] private JanitorPlayerBehaviour janitorPlayerBehaviour;
    [SerializeField] private EventTrigger maskTrigger;
    [SerializeField] private EventTrigger monitorTrigger;
    [SerializeField] private EventTrigger triggerToggle;
    [SerializeField] private Image oxygenBlackout;

    private void Awake()
    {
        AddListener(triggerToggle, EventTriggerType.PointerEnter, ResetCooldown);
        AddListener(maskTrigger, EventTriggerType.PointerEnter, MaskTrigger);
        AddListener(monitorTrigger, EventTriggerType.PointerEnter, MonitorTrigger);

        oxygenBlackout.color = new(0, 0, 0, 0);
    }

    private void ResetCooldown()
    {
        janitorPlayerBehaviour.ResetCooldown();
        UpdateTriggerState();
    }

    private void MaskTrigger()
    {
        janitorPlayerBehaviour.MaskTrigger();
        UpdateTriggerState();
    }

    private void MonitorTrigger()
    {
        janitorPlayerBehaviour.MonitorTrigger();
        UpdateTriggerState();
    }

    private void UpdateTriggerState()
    {
        bool isMonitorUp = janitorPlayerBehaviour.isMonitorUp;
        bool canToggle = janitorPlayerBehaviour.canToggle;
        bool isWearingMask = janitorPlayerBehaviour.isWearingMask;

        maskTrigger.enabled = canToggle;
        monitorTrigger.enabled = canToggle;

        maskTrigger.gameObject.SetActive(!isMonitorUp);
        monitorTrigger.gameObject.SetActive(!isWearingMask); ;
        triggerToggle.gameObject.SetActive(!canToggle); ;
    }

    public override void UpdatePowerText()
    {
        base.UpdatePowerText();

        powerText.text = $"Battery:{Mathf.Round(playerBehaviour.currentPower.Value)}%";
    }

    public override void Update()
    {
        base.Update();

        oxygenBlackout.color = new(0, 0, 0, 1f - (janitorPlayerBehaviour.oxygenLevels.Value / 100f));
    }
}
