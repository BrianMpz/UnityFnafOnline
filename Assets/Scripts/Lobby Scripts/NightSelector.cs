using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NightSelector : MonoBehaviour
{
    [SerializeField] private Button previousNightButton;
    [SerializeField] private Button nextNightButton;
    [SerializeField] private TMP_Text nightText;

    [SerializeField] private bool isDemo;

    private GameNight CurrentNight
    {
        get
        {
            return MultiplayerManager.Instance.gameNight.Value;
        }

        set
        {
            MultiplayerManager.Instance.gameNight.Value = value;
        }
    }

    private void Start()
    {
        MultiplayerManager.Instance.gameNight.OnValueChanged += (previousValue, newValue) => { UpdateNightText(newValue); };
        UpdateNightText(newValue: CurrentNight);

        if (!NetworkManager.Singleton.IsServer)
        {
            previousNightButton.gameObject.SetActive(false);
            nextNightButton.gameObject.SetActive(false);
            return;
        }

        if (isDemo)
        {
            previousNightButton.enabled = false;
            nextNightButton.enabled = false;
        }

        previousNightButton.onClick.AddListener(GoToPreviousNight);
        nextNightButton.onClick.AddListener(GoToNextNight);

        CurrentNight = GetHighestAvailableNight();
    }

    private void UpdateNightText(GameNight newValue)
    {
        nightText.text = $"Night {newValue}";
    }

    private void GoToPreviousNight()
    {
        GameNight[] nights = (GameNight[])Enum.GetValues(typeof(GameNight));
        int previousIndex = (int)CurrentNight - 1;

        // Always allow Night One
        if (previousIndex < 0)
        {
            Debug.Log("Already at the first night.");
            return;
        }

        // Keep going back until we find a completed night
        while (previousIndex > 0 && PlayerPrefs.GetInt("CompletedNight_" + previousIndex, 0) == 0)
        {
            previousIndex--;
        }

        CurrentNight = nights[previousIndex];
    }

    private void GoToNextNight()
    {
        GameNight[] nights = (GameNight[])Enum.GetValues(typeof(GameNight));
        int nextIndex = (int)CurrentNight + 1;

        // If we're at the last night, don't go further
        if (nextIndex >= nights.Length)
        {
            Debug.Log("Already at the last night.");
            return;
        }

        // Keep going forward until we find an available night
        while (nextIndex < nights.Length && PlayerPrefs.GetInt("CompletedNight_" + nextIndex, 0) == 0)
        {
            nextIndex++;
        }

        // Ensure we don't go out of bounds
        if (nextIndex < nights.Length)
        {
            CurrentNight = nights[nextIndex];
        }
        else
        {
            Debug.Log("No further nights available.");
        }
    }

    private GameNight GetHighestAvailableNight()
    {
        GameNight[] nights = (GameNight[])Enum.GetValues(typeof(GameNight));

        for (int i = nights.Length - 1; i >= 0; i--) // Start from the highest night
        {
            if (PlayerPrefs.GetInt("CompletedNight_" + i, i == 0 ? 1 : 0) == 1)
            {
                return nights[i]; // Return the highest unlocked night
            }
        }

        return GameNight.One; // Default to Night 1 if no other is available
    }

    public void UnlockAllNights()
    {
        // Get all nights from the enum
        GameNight[] nights = (GameNight[])Enum.GetValues(typeof(GameNight));

        // Loop through each night and mark it as completed
        foreach (GameNight night in nights)
        {
            int nightIndex = (int)night;
            PlayerPrefs.SetInt("CompletedNight_" + nightIndex, 1);
        }

        // Save the changes to disk
        PlayerPrefs.Save();
        Debug.Log("All nights have been unlocked!");

        CurrentNight = nights[(int)GameNight.Seven];
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha5))
        {
            UnlockAllNights();
        }
    }
}
