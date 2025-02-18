using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GlobalCameraSystem : Singleton<GlobalCameraSystem>
{
    private List<CameraData> CameraDatas
    {
        get => GetComponentsInChildren<CameraData>().ToList();
    }

    public CameraData GetCameraDataFromCameraName(CameraName cameraName)
    {
        return CameraDatas.FirstOrDefault(cameradata => cameradata.GetCameraName() == cameraName);
    }

    [SerializeField] private List<PlayerComputer> playerComputers;

    private int previousPlayersWatchingFoxy = -1;
    public Action<int> OnPlayersWatchingFoxyChanged;
    public Action<CameraName> OnCameraVisibilityChanged;


    private void Start()
    {
        GameManager.Instance.OnGameStarted += () => { StartCoroutine(SetCameraVisibilities()); };
    }

    private IEnumerator SetCameraVisibilities()
    {
        List<CameraData> changableCameraDatas = CameraDatas.Where(cameraData =>
            cameraData.GetCameraName() != CameraName.Eight &&
            cameraData.GetCameraName() != CameraName.Four &&
            cameraData.GetCameraName() != CameraName.Nine
            ).ToList();


        while (GameManager.Instance.isPlaying)
        {
            foreach (CameraData cameraData in changableCameraDatas)
            {
                if (UnityEngine.Random.Range(0, 10) <= 7 && Maintenance.Instance.camerasState.Value != State.ONLINE)
                {
                    cameraData.isHidden = true;
                }
                else
                {
                    cameraData.isHidden = false;
                }
                OnCameraVisibilityChanged?.Invoke(cameraData.GetCameraName());
            }

            yield return new WaitForSeconds(5);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CountPlayersWatchingFoxyServerRpc()
    {
        int count = 0;
        foreach (PlayerComputer playerComputer in playerComputers)
        {
            if (playerComputer.isMonitorUp.Value && playerComputer.playerCameraSystem.isWatchingFoxy.Value)
            {
                count++;
            }
        }

        if (count != previousPlayersWatchingFoxy)
        {
            previousPlayersWatchingFoxy = count;
            OnPlayersWatchingFoxyChanged?.Invoke(count);
        }
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

