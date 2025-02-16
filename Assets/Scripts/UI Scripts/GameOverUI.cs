using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private TMP_Text timeLeftText;

    private void Start()
    {
        leaveButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);
        playAgainButton.onClick.AddListener(() =>
        {
            MiscellaneousGameUI.Instance.waitToPlayAgainUI.Show();
            Hide();
        });
        GameManager.Instance.OnGameOver += Show;

        Hide();
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    private void Show()
    {
        GameAudioManager.Instance.StopAllSfx();
        MiscellaneousGameUI.Instance.debugCanvasUI.Hide();
        canvas.enabled = true;

        float timeLeft = Mathf.Round((GameManager.MaxGameLength - GameManager.Instance.currentGameTime.Value) * 10f) / 10f;
        timeLeftText.text = $"You had {timeLeft} seconds left!";
    }
}
