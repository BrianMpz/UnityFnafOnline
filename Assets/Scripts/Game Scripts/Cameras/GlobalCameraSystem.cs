using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public float timeSinceLastFoxyCheck;


    private void Start()
    {
        GameManager.Instance.OnGameStarted += () => { StartCoroutine(SetCameraVisibilities()); };
        playerComputers = PlayerRoleManager.Instance.GetComponentsInChildren<PlayerComputer>().ToList();
        DisableAllCameraComponents();
    }

    private void Update()
    {
        if (!IsServer) return;

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
        int playersWatchingfoxy = 0;

        foreach (PlayerComputer playerComputer in playerComputers)
        {
            if (playerComputer.playerCameraSystem.IsWatchingCamera(CameraName.Three))
            {
                playersWatchingfoxy++;
                timeSinceLastFoxyCheck = 0;
            }
        }

        timeSinceLastFoxyCheck += Time.deltaTime;

        OnPlayersWatchingFoxyUpdate?.Invoke(playersWatchingfoxy);
    }

    public bool IsSomeoneWatchingNode(Node node)
    {
        foreach (PlayerComputer playerComputer in playerComputers)
        {
            if (playerComputer.playerCameraSystem.IsNodeVisibleOnCamera(node, true))
            {
                return true;
            }
        }

        return false;
    }

    public void DisableAllCameraComponents()
    {
        CameraDatas.ForEach(cam => cam.cameraFlashlight.enabled = false);
        CameraDatas.ForEach(cam => cam.GetCamera().enabled = false);
    }

    public void EnableCameraComponent(CameraData cameraData)
    {
        DisableAllCameraComponents();

        cameraData.cameraFlashlight.enabled = true;
        cameraData.GetCamera().enabled = true;
    }

    public bool CheckIfAnyoneWatchingHallwayNode(Node hallwayNode)
    {
        return playerComputers.Any(pc => pc.playerCameraSystem.CheckIfAnyoneWatchingHallwayNode(hallwayNode));
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

