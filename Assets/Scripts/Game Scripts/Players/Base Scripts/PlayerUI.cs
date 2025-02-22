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

    private void Awake()
    {
        GameManager.Instance.currentHour.OnValueChanged += UpdateGameTimeText;
        GameManager.Instance.OnGameStarted += SetNightText;
        playerBehaviour.OnInitialise += Initialise;
        playerBehaviour.OnDisable += Disable;
        playerBehaviour.OnKill += Hide;
        playerBehaviour.OnFoxyPowerDrain += OnPowerDrain;
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

    public void HideInSpectator() => UICanvas.enabled = false;

    public void ShowInSpectator() => UICanvas.enabled = true;

    public virtual void Update()
    {
        if (!playerBehaviour.isAlive.Value) return;

        UpdatePowerText();
    }

    public virtual void UpdatePowerText()
    {
        powerText.text = $"Power:{Mathf.Round(playerBehaviour.power.Value)}%";
        usageText.text = $"Usage:{Mathf.Round(playerBehaviour.powerUsage.Value)} Units";

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

    private protected void OnPowerDrain()
    {
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
