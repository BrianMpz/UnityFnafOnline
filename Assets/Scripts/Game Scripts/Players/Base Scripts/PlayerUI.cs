using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private protected PlayerBehaviour playerBehaviour;
    [SerializeField] private protected Canvas UICanvas;
    [SerializeField] private protected TMP_Text hourText;
    [SerializeField] private protected TMP_Text nightText;
    [SerializeField] private protected TMP_Text powerText;
    [SerializeField] private protected TMP_Text usageText;

    private void Start()
    {
        GameManager.Instance.currentHour.OnValueChanged += UpdateGameTimeText;
        GameManager.Instance.OnGameStarted += SetNightText;
        playerBehaviour.OnInitialise += Initialise;
        playerBehaviour.OnDisable += Disable;
        playerBehaviour.OnPlayerJumpscare += Hide;
        AnimatronicManager.Instance.foxy.OnFoxyPowerDrain += OnPowerDrain;
        Hide();
    }

    virtual public void Initialise()
    {
        Show();
    }

    virtual public void Disable()
    {
        Hide();
    }

    virtual public void Show() => UICanvas.enabled = true;

    public void Hide() => UICanvas.enabled = false;

    public virtual void Update()
    {
        if (!playerBehaviour.isPlayerAlive.Value) return;

        UpdatePowerText();
    }

    public virtual void UpdatePowerText()
    {
        powerText.text = $"Power:{Mathf.Round(playerBehaviour.currentPower.Value)}%";
        usageText.text = $"Usage:{Mathf.Round(playerBehaviour.currentPowerUsage.Value)} Units";

        if (PowerGenerator.Instance.GetIsCharging(playerBehaviour.playerRole).Value)
            powerText.color = Color.green;
        else
            powerText.color = Color.white;
    }

    private void SetNightText()
    {
        nightText.text = $"Night {GameManager.Instance.gameNight}";
    }

    private void UpdateGameTimeText(int previousHour, int currentHour)
    {
        hourText.text = $"{currentHour}AM";
    }

    private protected void OnPowerDrain(PlayerRoles playerRole, float _)
    {
        if (playerBehaviour.playerRole != playerRole) return;

        StartCoroutine(ShowPowerDrain());
    }

    private IEnumerator ShowPowerDrain()
    {
        powerText.color = Color.red;
        float elapsedTime = 0f;
        float endTime = 0.5f;

        while (elapsedTime < endTime)
        {
            elapsedTime += Time.deltaTime;

            powerText.color = Color.Lerp(Color.red, Color.white, elapsedTime / endTime);

            yield return null;
        }

        powerText.color = Color.white;
    }

    public void AddListener(EventTrigger eventTrigger, EventTriggerType triggerType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new() { eventID = triggerType };
        entry.callback.AddListener(_ => callback());
        eventTrigger.triggers.Add(entry);
    }
}
