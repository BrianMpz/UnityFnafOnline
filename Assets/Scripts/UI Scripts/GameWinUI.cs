using UnityEngine;
using UnityEngine.UI;

public class GameWinUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button playAgainButton;

    void Start()
    {
        leaveButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);
        playAgainButton.onClick.AddListener(() =>
        {
            MiscellaneousGameUI.Instance.waitToPlayAgainUI.Show();
            Hide();
        });
        GameManager.Instance.OnGameWin += Show;

        Hide();
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameWin -= Show;
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
    }
}
