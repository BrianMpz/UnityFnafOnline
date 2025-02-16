using UnityEngine;
using UnityEngine.UI;

public class ConnectToUnityServicesFailedUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;

    private void Start()
    {
        Hide();
        retryButton.onClick.AddListener(() => { Loader.LoadScene(Loader.Scene.MainMenu); });
        ServicesInitialiser.Instance.OnConnectionToServicesFailed += RelayManager_OnConnectionToServicesFailed;
    }

    private void RelayManager_OnConnectionToServicesFailed()
    {
        Show();
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
