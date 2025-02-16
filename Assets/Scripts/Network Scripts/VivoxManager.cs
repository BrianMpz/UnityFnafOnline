using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxManager : Singleton<VivoxManager>
{
    public Action<string> ChannelJoined;
    public Action<string> ChannelLeft;

    public string lobbyChatName;
    public string gameChatName;
    public string privateChatName;

    public string currentChannelName;


    private void Awake()
    {
        if (MultiplayerManager.isPlayingOnline) DontDestroyOnLoad(gameObject); else Destroy(gameObject);
    }

    private void OnChannelJoined(string name)
    {
        ChannelJoined?.Invoke(name);
    }

    private void OnChannelLeft(string name)
    {
        ChannelLeft?.Invoke(name);
    }

    public async Task LogInAsync()
    {
        await VivoxService.Instance.InitializeAsync();
        VivoxService.Instance.ChannelJoined += OnChannelJoined;
        VivoxService.Instance.ChannelLeft += OnChannelLeft;

        LoginOptions loginOptions = new() { DisplayName = MultiplayerManager.Instance.playerName, EnableTTS = true };
        await VivoxService.Instance.LoginAsync(loginOptions);

        privateChatName = "PrivateChat_" + UnityEngine.Random.Range(100000000, 1000000000).ToString();

        currentChannelName = privateChatName;
        await VivoxService.Instance.JoinGroupChannelAsync(privateChatName, ChatCapability.TextAndAudio);
    }

    public async Task LogOutAsync()
    {
        if (VivoxService.Instance == null) return;

        if (VivoxService.Instance.IsLoggedIn)
        {
            await VivoxService.Instance.LogoutAsync();
        }
    }

    public void JoinedRoom(string roomCode)
    {
        lobbyChatName = roomCode + "_LobbyChat";
        gameChatName = roomCode + "_GameChat";
    }

    // Switch chat based on game state
    public void SwitchToLobbyChat() => SetActiveAudioChannel(lobbyChatName);
    public void SwitchToGameChat() => SetActiveAudioChannel(gameChatName);
    public void SwitchToPrivateChat() => SetActiveAudioChannel(privateChatName);

    private async Task LeaveCurrentChannel(string channelName)
    {
        bool notInThisChannel = !VivoxService.Instance.ActiveChannels.Keys.ToList().Contains(channelName);
        if (notInThisChannel) return;

        await VivoxService.Instance.LeaveChannelAsync(channelName);

        Debug.Log($"left {channelName}");
    }

    private async void SetActiveAudioChannel(string channelName)
    {
        if (VivoxService.Instance == null) return;
        if (currentChannelName == channelName) return;

        await LeaveCurrentChannel(currentChannelName);

        currentChannelName = channelName;

        if (!VivoxService.Instance.IsLoggedIn) await LogInAsync();

        if (GetChannel(currentChannelName) == null) await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio);

        VivoxService.Instance.DisableAcousticEchoCancellation();

        Debug.Log($"joined {channelName}");
        VivoxService.Instance.UnmuteOutputDevice();
    }

    public bool IsInChannel(string channel)
    {
        return currentChannelName == channel;
    }

    public List<VivoxParticipant> GetChannel(string desiredChannel)
    {
        foreach (KeyValuePair<string, ReadOnlyCollection<VivoxParticipant>> channel in VivoxService.Instance.ActiveChannels)
        {
            if (channel.Key == desiredChannel) return channel.Value.ToList();
        }

        return default;
    }

    public void ToggleMute()
    {
        if (VivoxService.Instance.IsInputDeviceMuted) VivoxService.Instance.UnmuteInputDevice(); else VivoxService.Instance.MuteInputDevice();
    }
}
