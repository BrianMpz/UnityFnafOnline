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

    private List<PlayerComputer> PlayerComputers
    {
        get => PlayerRoleManager.Instance.GetComponentsInChildren<PlayerComputer>().ToList();
    }

    public Action<int> OnPlayersWatchingFoxyUpdate;
    public Action<CameraName> OnCameraVisibilityChanged;


    private void Start()
    {
        GameManager.Instance.OnGameStarted += () => { StartCoroutine(SetCameraVisibilities()); };
    }

    private IEnumerator SetCameraVisibilities()
    {
        List<CameraData> changableCameraDatas = CameraDatas.Where(cameraData => cameraData.GetCameraName() != CameraName.Eight).ToList();

        while (GameManager.Instance.isPlaying)
        {
            foreach (CameraData cameraData in changableCameraDatas)
            {
                if (Maintenance.Instance.camerasState.Value != State.ONLINE)
                {
                    cameraData.isCurrentlyHidden = true;
                }
                else
                {
                    cameraData.isCurrentlyHidden = cameraData.isAudioOnly;
                }
                OnCameraVisibilityChanged?.Invoke(cameraData.GetCameraName());
            }

            yield return new WaitForSeconds(2);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CountPlayersWatchingFoxyServerRpc()
    {
        int count = 0;

        foreach (PlayerComputer playerComputer in PlayerComputers)
        {
            if (playerComputer.playerCameraSystem.isWatchingFoxy.Value)
            {
                count++;
            }
        }
        OnPlayersWatchingFoxyUpdate?.Invoke(count);
    }

    public void DisableLights()
    {
        CameraDatas.ForEach(cam => cam.cameraFlashlight.enabled = false);
    }

    public bool CheckIfAnyoneWatchingHallwayNode(Node hallwayNode)
    {
        foreach (PlayerComputer playerComputer in PlayerComputers)
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

