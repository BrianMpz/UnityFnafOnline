using System;
using UnityEngine;
using UnityEngine.UI;

public class TrackerButton : MonoBehaviour
{
    [SerializeField] private PlayerMotionDetectionSystem playerMotionDetectionSystem;
    public TrackerNode[] encompassingNodes;
    [SerializeField] private Button trackerButton;
    [SerializeField] private Image trackerRadius;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMotionDetectionSystem.OnTrackerUpdate += OnTrackerUpdate;
        playerMotionDetectionSystem.OnTrackerPulse += OnTrackerPulse;
        trackerButton.onClick.AddListener(SetTracker);
    }

    private void OnTrackerPulse()
    {
        if (playerMotionDetectionSystem.currentTrackerButton != this) return;
    }

    private void OnTrackerUpdate(TrackerButton button)
    {
        trackerRadius.enabled = button == this;
    }

    private void SetTracker()
    {
        playerMotionDetectionSystem.SetTracker(this);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Hide()
    {
        trackerRadius.enabled = false;
    }

    private void Show()
    {
        trackerRadius.enabled = true;
    }
}
