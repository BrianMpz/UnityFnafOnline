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
    }

    private void ResetCooldown()
    {
        janitorPlayerBehaviour.ResetCooldown();
        UpdateUIState();
    }

    private void MaskTrigger()
    {
        janitorPlayerBehaviour.MaskTrigger();
        UpdateUIState();
    }

    private void MonitorTrigger()
    {
        janitorPlayerBehaviour.MonitorTrigger();
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        maskTrigger.gameObject.SetActive(!janitorPlayerBehaviour.isMonitorUp && janitorPlayerBehaviour.canToggle);
        monitorTrigger.gameObject.SetActive(!janitorPlayerBehaviour.isWearingMask && janitorPlayerBehaviour.canToggle);
    }

    public override void Update()
    {
        base.Update();

        oxygenBlackout.color = new(0, 0, 0, 1.1f - (janitorPlayerBehaviour.oxygenLevels.Value / 100f));
    }
}
