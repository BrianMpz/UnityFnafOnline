using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class ServicesInitialiser : Singleton<ServicesInitialiser>
{
    public Action OnConnectionToServicesCompleted;
    public Action OnConnectionToServicesFailed;
    public bool areServicesInitialised;

    private async void Start()
    {
        if (!MultiplayerManager.isPlayingOnline) return;

        try
        {
            if (UnityServices.Instance.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

            await VivoxManager.Instance.LogInAsync();

            OnConnectionToServicesCompleted?.Invoke();
            areServicesInitialised = true;
        }
        catch (Exception)
        {
            OnConnectionToServicesFailed?.Invoke();
        }
    }
}
