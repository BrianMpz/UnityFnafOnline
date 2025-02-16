using TMPro;
using UnityEngine;

public class JoiningRoomUI : MonoBehaviour
{
    [SerializeField] private TMP_Text connectingToRoomText;

    private void Start()
    {
        MultiplayerManager.Instance.OnTryingToJoinGame += MultiplayerManager_OnTryingToJoinGame;
        MultiplayerManager.Instance.OnDisconnectedFromGame += MultiplayerManager_OnFailedToJoinGame;

        Hide();
    }

    private void MultiplayerManager_OnTryingToJoinGame(bool IsServer)
    {
        Show();

        if (IsServer)
        {
            connectingToRoomText.text = "Creating Room...";
        }
        else
        {
            connectingToRoomText.text = "Joining Room...";
        }
    }

    private void MultiplayerManager_OnFailedToJoinGame(bool IsServer)
    {
        Hide();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (MultiplayerManager.Instance == null) return;

        MultiplayerManager.Instance.OnTryingToJoinGame -= MultiplayerManager_OnTryingToJoinGame;
        MultiplayerManager.Instance.OnDisconnectedFromGame -= MultiplayerManager_OnFailedToJoinGame;
    }
}
