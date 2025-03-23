using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameDisconnectedUI : MonoBehaviour
{
    [SerializeField] private Button playAgainButton;
    [SerializeField] private TMP_Text disconnectReason;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        NetworkManager.Singleton.OnTransportFailure += NetworkManager_OnTransportFailure;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnDisconnectCallback;
        MultiplayerManager.Instance.OnKick += MultiplayerManager_OnKick;

        if (MultiplayerManager.isPlayingOnline)
        {
            VivoxService.Instance.LoggedOut += VivoxService_LoggedOut;
            VivoxService.Instance.ConnectionFailedToRecover += VivoxService_LoggedOut;
        }

        Hide();

        playAgainButton.onClick.AddListener(() => { Loader.LoadScene(Loader.Scene.MainMenu); });
    }

    private void OnDestroy()
    {
        if (MultiplayerManager.isPlayingOnline)
        {
            VivoxService.Instance.LoggedOut -= VivoxService_LoggedOut;
            VivoxService.Instance.ConnectionFailedToRecover -= VivoxService_LoggedOut;
        }
    }

    private void VivoxService_LoggedOut()
    {
        DestroyGame();
        disconnectReason.text = "Lost connection to Online Service!";
    }

    private void NetworkManager_OnDisconnectCallback(ulong clientId)
    {
        if (SceneManager.GetActiveScene().name == Loader.Scene.Matchmaking.ToString()) return;

        if (GameAudioManager.Instance != null && clientId == NetworkManager.Singleton.LocalClientId) GameAudioManager.Instance.StopAllSfx();

        if (clientId == 0 && clientId != NetworkManager.Singleton.LocalClientId)
        {
            DestroyGame();
            disconnectReason.text = "The Host has disconnected!";
        }

        else if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            DestroyGame();
            disconnectReason.text = "Lost connection to Online Service!";
        }
    }

    private void NetworkManager_OnTransportFailure()
    {
        DestroyGame();
        disconnectReason.text = "Lost connection to Online Service!";
    }

    private void MultiplayerManager_OnKick()
    {
        DestroyGame();
        disconnectReason.text = "You have been Kicked from the Server!";
    }

    private async void DestroyGame()
    {
        if (MultiplayerManager.isPlayingOnline) await VivoxManager.Instance.LogOutAsync();
        NetworkManager.Singleton.Shutdown();
        Show();
    }

    private void Show()
    {
        GetComponent<Canvas>().enabled = true;
    }

    private void Hide()
    {
        GetComponent<Canvas>().enabled = false;
    }

}
