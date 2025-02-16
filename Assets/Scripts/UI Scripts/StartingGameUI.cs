using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartingGameUI : MonoBehaviour
{
    private string[] loadingMessages =
    {
        "Starting game",
        "Checking cameras",
        "Stocking supplies",
        "Testing door locks",
        "Preparing for opening hour",
        "Calibrating animatronics",
        "Recharging door batteries",
        "Cleaning party tables",
        "Inspecting kitchen equipment",
        "Powering up systems",
        "Restocking prize counter",
        "Sweeping the floors"
    };
    [SerializeField] private Image helpy;
    [SerializeField] private Sprite helpyImg1;
    [SerializeField] private Sprite helpyImg2;
    [SerializeField] private TMP_Text startingGameText;
    [SerializeField] private Canvas canvas;

    private void Start()
    {
        Show();

        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
        StartCoroutine(WaitingForGameToStart());
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameStarted -= GameManager_OnGameStarted;
    }

    private void GameManager_OnGameStarted()
    {
        StopAllCoroutines();
        Hide();
    }

    private IEnumerator WaitingForGameToStart()
    {
        string message = loadingMessages[Random.Range(0, loadingMessages.Length)];

        int dotCount = 0;

        while (true)
        {
            // Update the loading message with 1, 2, or 3 dots
            startingGameText.text = message + new string('.', dotCount + 1);
            dotCount = (dotCount + 1) % 3; // Cycle dotCount between 0, 1, and 2

            // Alternate the sprite for helpy every 0.3 seconds
            helpy.sprite = helpyImg1;
            helpy.SetAllDirty(); // Ensure UI updates
            yield return new WaitForSeconds(0.3f);

            helpy.sprite = helpyImg2;
            helpy.SetAllDirty(); // Ensure UI updates
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    private void Show()
    {
        canvas.enabled = true;
    }
}
