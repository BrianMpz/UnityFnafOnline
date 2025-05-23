using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxManager : Singleton<VivoxManager>
{
    public static bool CanUseVoiceChat;
    private bool isSwitchingChannels;
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
        if (!CanUseVoiceChat) return;

        await VivoxService.Instance.InitializeAsync();
        VivoxService.Instance.ChannelJoined += OnChannelJoined;
        VivoxService.Instance.ChannelLeft += OnChannelLeft;

        LoginOptions loginOptions = new() { DisplayName = MultiplayerManager.Instance.playerName, EnableTTS = true };
        await VivoxService.Instance.LoginAsync(loginOptions);

        privateChatName = "PrivateChat_" + Guid.NewGuid().ToString("N")[..8];

        currentChannelName = privateChatName;
        await VivoxService.Instance.JoinGroupChannelAsync(privateChatName, ChatCapability.TextAndAudio);
    }

    public async Task LogOutAsync()
    {
        if (!CanUseVoiceChat) return;

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
    public void SwitchToLobbyChat() => StartCoroutine(SwitchChannels(lobbyChatName));
    public void SwitchToGameChat() => StartCoroutine(SwitchChannels(gameChatName));
    public void SwitchToPrivateChat() => StartCoroutine(SwitchChannels(privateChatName));

    private IEnumerator SwitchChannels(string channelName)
    {
        if (isSwitchingChannels) yield return new WaitUntil(() => { return !isSwitchingChannels; });
        SetActiveAudioChannel(channelName);
    }

    private async void SetActiveAudioChannel(string channelName)
    {
        if (!CanUseVoiceChat) return;

        if (VivoxService.Instance == null) return;
        if (currentChannelName == channelName) return;

        isSwitchingChannels = true;

        if (!VivoxService.Instance.IsLoggedIn) await LogInAsync();

        await LeaveCurrentChannel(currentChannelName);

        if (GetChannel(channelName) == null) await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

        currentChannelName = channelName;

        isSwitchingChannels = false;

        Debug.Log($"joined {channelName}");
    }

    private async Task LeaveCurrentChannel(string channelName)
    {
        if (!CanUseVoiceChat) return;

        if (!VivoxService.Instance.ActiveChannels.TryGetValue(channelName, out _)) return; // we are not in any channels

        await VivoxService.Instance.LeaveChannelAsync(channelName);

        Debug.Log($"left {channelName}");
    }

    void Update()
    {
        if (VivoxService.Instance == null) return;
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
        return null;
    }

    public void ToggleMute()
    {
        if (VivoxService.Instance.IsInputDeviceMuted) VivoxService.Instance.UnmuteInputDevice(); else VivoxService.Instance.MuteInputDevice();
    }
}
