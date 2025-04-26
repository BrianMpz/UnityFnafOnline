using Unity.Netcode;
using UnityEngine;

public class MainMenuCleanUpUI : MonoBehaviour
{
    private async void Awake()
    {
        if (NetworkManager.Singleton != null) Destroy(NetworkManager.Singleton.gameObject);

        if (MultiplayerManager.Instance != null) Destroy(MultiplayerManager.Instance.gameObject);

        if (VivoxManager.Instance != null)
        {
            await VivoxManager.Instance.LogOutAsync();
            Destroy(VivoxManager.Instance.gameObject);
        }

        Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
