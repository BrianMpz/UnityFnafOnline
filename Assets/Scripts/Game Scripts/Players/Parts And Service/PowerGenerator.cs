using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PowerGenerator : NetworkSingleton<PowerGenerator>
{
    public PartsAndServiceBehaviour partsAndServiceBehaviour;
    public PartsAndServiceUI partsAndServiceUI;

    public NetworkVariable<bool> isChargingSomeone = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> SecurityOffice_Charging = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> PartsAndService_Charging = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> Backstage_Charging = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> Janitor_Charging = new(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] private List<GeneratorPlayerObject> generatorPlayerObjects;
    [SerializeField] private GameObject generatorOS;
    [SerializeField] private TMP_Text GeneratorDownText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerRoleManager.Instance.partsAndServiceBehaviour.OnPowerOn += PowerOn;
        PlayerRoleManager.Instance.partsAndServiceBehaviour.OnPowerDown += PowerOff;
        generatorPlayerObjects.ForEach(obj => obj.chargeButton.GetComponentInChildren<TMP_Text>().text = "");

        Maintenance.Instance.powerGeneratorState.OnValueChanged += PowerGeneratorStateChanged;
        GeneratorDownText.enabled = false;
    }

    private void PowerGeneratorStateChanged(State _, State newValue)
    {
        GeneratorDownText.enabled = newValue != State.ONLINE;

        if (!IsOwner) return;

        if (GeneratorDownText.enabled)
        {
            generatorPlayerObjects.ForEach(obj => obj.StopChargingPlayer());
        }
    }

    private void PowerOn()
    {
        generatorOS.SetActive(true);
        generatorPlayerObjects.ForEach(obj => obj.chargeButton.GetComponentInChildren<TMP_Text>().text = "Charge");
    }

    private void PowerOff()
    {
        generatorOS.SetActive(false);
        generatorPlayerObjects.ForEach(obj => obj.StopChargingPlayer());
    }

    public NetworkVariable<bool> GetIsCharging(PlayerRoles playerRole)
    {
        return playerRole switch
        {
            PlayerRoles.SecurityOffice => SecurityOffice_Charging,
            PlayerRoles.PartsAndService => PartsAndService_Charging,
            PlayerRoles.Backstage => Backstage_Charging,
            PlayerRoles.Janitor => Janitor_Charging,
            _ => default,
        };
    }

    public void ChargePlayer(PlayerRoles playerRole)
    {
        if (!IsOwner) return;

        isChargingSomeone.Value = true;

        switch (playerRole)
        {
            case PlayerRoles.SecurityOffice:
                SecurityOffice_Charging.Value = true;
                break;
            case PlayerRoles.PartsAndService:
                PartsAndService_Charging.Value = true;
                break;
            case PlayerRoles.Backstage:
                Backstage_Charging.Value = true;
                break;
            case PlayerRoles.Janitor:
                Janitor_Charging.Value = true;
                break;
        }
    }

    public void StopChargingPlayers()
    {
        if (!IsOwner) return;

        isChargingSomeone.Value = false;

        SecurityOffice_Charging.Value = false;
        PartsAndService_Charging.Value = false;
        Backstage_Charging.Value = false;
        Janitor_Charging.Value = false;
    }
}
