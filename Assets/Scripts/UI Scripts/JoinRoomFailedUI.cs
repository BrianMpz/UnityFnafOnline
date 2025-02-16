using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class JoinRoomFailedUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text responseText;

    private void Start()
    {
        closeButton.onClick.AddListener(Hide);
        MultiplayerManager.Instance.OnDisconnectedFromGame += MultiplayerManager_OnFailedToJoinGame;
        Hide();
    }

    private void MultiplayerManager_OnFailedToJoinGame(bool IsServer)
    {
        Show();

        responseText.text = NetworkManager.Singleton.DisconnectReason;

        if (!IsServer && !MultiplayerManager.Instance.IsCodeValid())
        {
            responseText.text = "Join Code is Invalid";
        }
        else if (responseText.text == "" || responseText.text == null)
        {
            responseText.text = "Failed to connnect to this room!";
        }
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

        MultiplayerManager.Instance.OnDisconnectedFromGame -= MultiplayerManager_OnFailedToJoinGame;
    }
}
