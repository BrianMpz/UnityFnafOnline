using TMPro;
using UnityEngine;

public class ConnectingToUnityServicesUI : MonoBehaviour
{
    [SerializeField] TMP_Text message;

    private void Start()
    {
        ServicesInitialiser.Instance.OnConnectionToServicesCompleted += RelayManagerManager_OnConnectionToServicesCompleted;
        ServicesInitialiser.Instance.OnConnectionToServicesFailed += RelayManager_OnConnectionToServicesFailed;

        Show();

        if (MultiplayerManager.isPlayingOnline)
        {
            message.text = "Connecting to Online Services...";
        }
        else
        {
            message.text = "Loading Offline Lobby...";
        }
    }

    private void RelayManagerManager_OnConnectionToServicesCompleted()
    {
        Hide();
    }

    private void RelayManager_OnConnectionToServicesFailed()
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

}
