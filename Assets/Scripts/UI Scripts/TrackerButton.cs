using UnityEngine;
using UnityEngine.UI;

public class TrackerButton : MonoBehaviour
{
    [SerializeField] private PlayerMotionDetectionSystem playerMotionDetectionSystem;
    public TrackerNode[] encompassingNodes;
    [SerializeField] private Button trackerButton;
    [SerializeField] private Image trackerRadius;
    public string roomName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMotionDetectionSystem.OnTrackerUpdate += OnTrackerUpdate;
        trackerButton.onClick.AddListener(SetTracker);
    }

    private void OnTrackerUpdate(TrackerButton button)
    {
        trackerRadius.enabled = button == this;
    }

    private void Update()
    {
        trackerRadius.transform.Rotate(0, 0, 10 * Time.deltaTime);
    }

    private void SetTracker()
    {
        playerMotionDetectionSystem.SetTracker(playerMotionDetectionSystem.currentTrackerButton == this ? null : this);
    }
}
