using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class GeneratorPlayerObject : MonoBehaviour
{
    public PlayerRoles playerRole;
    [SerializeField] private TMP_Text power;
    public EventTrigger chargeButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AddListener(chargeButton, EventTriggerType.PointerDown, ChargePlayer);
        AddListener(chargeButton, EventTriggerType.PointerUp, StopChargingPlayer);
        chargeButton.GetComponentInChildren<TMP_Text>().text = "Charge";

        PowerGenerator.Instance.GetIsCharging(playerRole).OnValueChanged += OnChargeChanged;

        DisableIfRoleIsNotPlaying();

        PowerGenerator.Instance.partsAndServiceUI.OnViewChanged += StopChargingPlayer;
    }

    private void DisableIfRoleIsNotPlaying()
    {
        gameObject.SetActive(false);

        foreach (PlayerData playerData in MultiplayerManager.Instance.playerDataList)
        {
            if (playerData.role == playerRole) gameObject.SetActive(true);
        }
    }

    private void OnChargeChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            power.color = Color.green;
            chargeButton.GetComponentInChildren<TMP_Text>().text = "Charging...";
        }
        else
        {
            power.color = Color.white;
            chargeButton.GetComponentInChildren<TMP_Text>().text = "Charge";
        }
    }

    private void ChargePlayer()
    {
        if (Maintenance.Instance.powerGeneratorState.Value != State.ONLINE)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error");
            return;
        }

        PowerGenerator.Instance.ChargePlayer(playerRole);
    }

    public void StopChargingPlayer()
    {
        PowerGenerator.Instance.StopChargingPlayers(playerRole);
    }

    public void AddListener(EventTrigger eventTrigger, EventTriggerType triggerType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new() { eventID = triggerType };
        entry.callback.AddListener(_ => callback());
        eventTrigger.triggers.Add(entry);
    }

    private void Update()
    {
        float powerValue = PlayerRoleManager.Instance.GetPlayerBehaviourFromRole(playerRole).currentPower.Value;
        powerValue = Mathf.Max(powerValue, 0);

        string powerText = powerValue.ToString("F1"); // Rounds to 1 decimal place
        power.text = powerText + "%";
    }

}
