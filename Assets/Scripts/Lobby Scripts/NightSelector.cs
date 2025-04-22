using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NightSelector : MonoBehaviour
{
    public static GameNight lastPlayedNight;
    public static bool hasPlayedAtLeastAGame;
    [SerializeField] private Button previousNightButton;
    [SerializeField] private Button nextNightButton;
    [SerializeField] private TMP_Text nightText;

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
        MultiplayerManager.Instance.gameNight.OnValueChanged += (previousValue, newValue) =>
        {
            UpdateNightText(newValue);
            lastPlayedNight = newValue;
        };

        UpdateNightText(newValue: CurrentNight);

        if (!NetworkManager.Singleton.IsServer)
        {
            previousNightButton.gameObject.SetActive(false);
            nextNightButton.gameObject.SetActive(false);
            return;
        }

        previousNightButton.onClick.AddListener(GoToPreviousNight);
        nextNightButton.onClick.AddListener(GoToNextNight);

        CurrentNight = hasPlayedAtLeastAGame ? lastPlayedNight : GetHighestAvailableNight();
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
            // Already at the first night
            return;
        }

        // Keep going back until we find a completed night
        while (previousIndex > 0 && PlayerPrefs.GetInt("HasCompletedNight_" + previousIndex, 0) == 0)
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
            // Already at the last night
            return;
        }

        // Keep going forward until we find an available night
        while (nextIndex < nights.Length && PlayerPrefs.GetInt("HasCompletedNight_" + nextIndex, 0) == 0)
        {
            nextIndex++;
        }

        // Ensure we don't go out of bounds
        if (nextIndex < nights.Length)
        {
            CurrentNight = nights[nextIndex];
        }
    }

    private GameNight GetHighestAvailableNight()
    {
        GameNight[] nights = (GameNight[])Enum.GetValues(typeof(GameNight));

        for (int i = nights.Length - 1; i >= 0; i--) // Start from the highest night
        {
            if (PlayerPrefs.GetInt("HasCompletedNight_" + i, i == 0 ? 1 : 0) == 1)
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
            PlayerPrefs.SetInt("HasCompletedNight_" + nightIndex, 1);
        }

        // Save the changes to disk
        PlayerPrefs.Save();
        Debug.Log("All nights have been unlocked!");

        CurrentNight = nights[(int)GameNight.Seven];
    }

    void Update()
    {
        if (!DebugUI.CanDebug) return;

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.Alpha5))
        {
            UnlockAllNights();
        }
    }
}
