using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GlobalCameraSystem : NetworkSingleton<GlobalCameraSystem>
{
    private List<CameraData> CameraDatas
    {
        get => GetComponentsInChildren<CameraData>().ToList();
    }

    public CameraData GetCameraDataFromCameraName(CameraName cameraName)
    {
        return CameraDatas.FirstOrDefault(cameradata => cameradata.GetCameraName() == cameraName);
    }

    private List<PlayerComputer> playerComputers;
    public Action<int> OnPlayersWatchingFoxyUpdate;
    public Action<CameraName> OnCameraVisibilityChanged;


    private void Start()
    {
        playerComputers = PlayerRoleManager.Instance.GetComponentsInChildren<PlayerComputer>().ToList();
        GameManager.Instance.OnGameStarted += () => { StartCoroutine(SetCameraVisibilities()); };
    }

    private void Update()
    {
        CountPlayersWatchingFoxy();
    }

    private IEnumerator SetCameraVisibilities()
    {
        List<CameraData> changableCameraDatas = CameraDatas.Where(cameraData => cameraData.GetCameraName() != CameraName.Eight).ToList();

        while (GameManager.Instance.isPlaying)
        {
            foreach (CameraData cameraData in changableCameraDatas)
            {
                bool previousIsCurrentlyHiddenValue = cameraData.isCurrentlyHidden; // used to check for a change in camera state

                if (Maintenance.Instance.camerasState.Value != State.ONLINE)
                {
                    cameraData.isCurrentlyHidden = true;
                }
                else
                {
                    cameraData.isCurrentlyHidden = cameraData.isAudioOnly;
                }

                if (cameraData.isCurrentlyHidden != previousIsCurrentlyHiddenValue) OnCameraVisibilityChanged?.Invoke(cameraData.GetCameraName());
            }

            yield return new WaitForSeconds(2);
        }
    }

    public void CountPlayersWatchingFoxy()
    {
        if (!IsServer) return;

        int playersWatchingfoxy = 0;
        foreach (PlayerComputer playerComputer in playerComputers)
        {
            if (playerComputer.playerCameraSystem.isWatchingFoxy.Value)
            {
                playersWatchingfoxy++;
            }
        }

        OnPlayersWatchingFoxyUpdate?.Invoke(playersWatchingfoxy);
    }

    public void DisableLights()
    {
        CameraDatas.ForEach(cam => cam.cameraFlashlight.enabled = false);
    }

    public bool CheckIfAnyoneWatchingHallwayNode(Node hallwayNode)
    {
        foreach (PlayerComputer playerComputer in playerComputers)
        {
            if (playerComputer.playerCameraSystem.CheckIfAnyoneWatchingHallwayNode(hallwayNode))
            {
                return true;
            }
        }

        return false;
    }
}

public enum CameraName
{
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
}

