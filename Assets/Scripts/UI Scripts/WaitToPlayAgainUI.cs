using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WaitToPlayAgainUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text waitingText;

    void Start()
    {
        leaveButton.onClick.AddListener(() => { MultiplayerManager.Instance.LeaveGame(); });

        Hide();
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    public void Show()
    {
        canvas.enabled = true;

        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.BackToLobby();

            waitingText.text = "Loading Lobby...";
        }
        else
        {
            waitingText.text = "Waiting for Host...";
        }
    }
}
