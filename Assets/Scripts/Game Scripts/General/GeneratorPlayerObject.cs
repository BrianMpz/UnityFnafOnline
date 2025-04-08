using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GeneratorPlayerObject : MonoBehaviour
{
    [SerializeField] private PlayerRoles playerRole;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Image powerBar;
    public EventTrigger chargeButton;
    private AudioSource chargingSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AddListener(chargeButton, EventTriggerType.PointerDown, ChargePlayer);
        AddListener(chargeButton, EventTriggerType.PointerUp, StopChargingPlayer);
        chargeButton.GetComponentInChildren<TMP_Text>().text = "Charge";

        PowerGenerator.Instance.GetIsCharging(playerRole).OnValueChanged += OnChargeChanged;
        PowerGenerator.Instance.isChargingSomeone.OnValueChanged += OnIsChargingChanged;

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

    private void OnChargeChanged(bool _, bool isBeingCharged) // called on all players
    {
        if (isBeingCharged == true)
        {
            powerText.color = Color.green;
            chargeButton.GetComponentInChildren<TMP_Text>().text = "Charging...";
            if (PlayerRoleManager.Instance.IsControllingPlayer(PlayerRoles.PartsAndService)) chargingSound = GameAudioManager.Instance.PlaySfxInterruptable("charging");
        }
        else
        {
            powerText.color = Color.white;
            chargeButton.GetComponentInChildren<TMP_Text>().text = "Charge";
            GameAudioManager.Instance.StopSfx(chargingSound);

            if (PowerGenerator.Instance.isChargingSomeone.Value && playerRole == PlayerRoles.PartsAndService) powerText.color = Color.red;
        }
    }

    private void OnIsChargingChanged(bool _, bool isChargingSomeone)
    {
        if (playerRole != PlayerRoles.PartsAndService) return;
        bool isChargingSomeoneElse = isChargingSomeone && !PowerGenerator.Instance.GetIsCharging(playerRole).Value;

        if (isChargingSomeoneElse) powerText.color = Color.red;
        else if (isChargingSomeone) powerText.color = Color.green;
        else powerText.color = Color.white;
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
        PowerGenerator.Instance.StopChargingPlayers();
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

        powerBar.fillAmount = powerValue / 100f;
        powerBar.color = Color.Lerp(Color.red, Color.green, powerBar.fillAmount);

        string powerString = powerValue.ToString("F1"); // Rounds to 1 decimal place
        powerText.text = powerString + "%";
    }

}
