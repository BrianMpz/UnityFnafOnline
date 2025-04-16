using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DoorLight : NetworkBehaviour
{
    [SerializeField] private Door door;
    [SerializeField] private Light doorLight;
    public DoorLightButton doorLightButton;
    private float lightsFlashLength = 1;
    private AudioSource lightAudioSource;
    public NetworkVariable<bool> isFlashingLight = new(writePerm: NetworkVariableWritePermission.Owner);
    private bool hasSeenDanger;

    public void ToggleLights()
    {
        if (isFlashingLight.Value)
        {
            return;
        }

        if (door.isLocked || doorLightButton.isBroken)
        {
            GameAudioManager.Instance.PlaySfxOneShot("button error", true);
            return;
        }

        StartCoroutine(HandleLightsVisual());
    }

    private IEnumerator HandleLightsVisual()
    {
        float endTime = Time.time + lightsFlashLength;
        bool hasEnemyAppeared = false;
        EnableLights();

        while (Time.time < endTime)
        {
            if (door.linkedNode.isOccupied.Value && !hasEnemyAppeared)
            {
                hasEnemyAppeared = true;

                if (door.linkedNode.occupier == AnimatronicManager.Instance.foxy)
                {
                    endTime += 2;
                    continue;
                }

                doorLight.enabled = false;
                if (lightAudioSource != null) lightAudioSource.mute = true;

                yield return new WaitForSeconds(0.2f);

                doorLight.enabled = true;
                if (lightAudioSource != null) lightAudioSource.mute = false;

                hasSeenDanger = false;
                if (!hasSeenDanger && !CantSeeDanger())
                {
                    GameAudioManager.Instance.PlaySfxOneShot("window scare", false);
                    hasSeenDanger = true;
                    StartCoroutine(WaitToDisableHasSeenDanger());
                }
            }

            doorLight.enabled = Random.Range(1, 10 + 1) != 1;

            if (lightAudioSource != null)
            {
                lightAudioSource.mute = !doorLight.enabled;
                if (!door.isDoorClosed.Value) lightAudioSource.mute = false;
            }

            yield return null;
        }

        DisableLights();
    }

    private bool CantSeeDanger()
    {
        PartsAndServiceBehaviour partsAndServiceBehaviour = PlayerRoleManager.Instance.partsAndServiceBehaviour;
        return door.isDoorClosed.Value ||
            (door.playerBehaviour == partsAndServiceBehaviour &&
                (
                    partsAndServiceBehaviour.partsAndServiceCameraController.currentView.Value != PartsAndServiceCameraController_View.DoorView ||
                    door.linkedNode.occupier == AnimatronicManager.Instance.foxy
                ));
    }

    private IEnumerator WaitToDisableHasSeenDanger()
    {
        yield return new WaitUntil(() => !door.linkedNode.isOccupied.Value);
        hasSeenDanger = false;
    }

    public void EnableLights()
    {
        lightAudioSource = GameAudioManager.Instance.PlaySfxInterruptable("light hum", false);
        doorLightButton.TurnOn();
        TurnOn();
        isFlashingLight.Value = true;
    }

    public void DisableLights()
    {
        GameAudioManager.Instance.StopSfx(lightAudioSource);
        doorLightButton.TurnOff();
        TurnOff();
        isFlashingLight.Value = false;
        StopAllCoroutines();
    }

    public void TurnOn()
    {
        doorLight.enabled = true;
        TurnOnServerRpc();
    }
    [ServerRpc(RequireOwnership = false)] private void TurnOnServerRpc(ServerRpcParams serverRpcParams = default) => TurnOnClientRpc(serverRpcParams.Receive.SenderClientId);
    [ClientRpc]
    private void TurnOnClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        doorLight.enabled = true;
    }

    public void TurnOff()
    {
        doorLight.enabled = false;
        TurnOffServerRpc();
    }
    [ServerRpc(RequireOwnership = false)] private void TurnOffServerRpc(ServerRpcParams serverRpcParams = default) => TurnOffClientRpc(serverRpcParams.Receive.SenderClientId);
    [ClientRpc]
    private void TurnOffClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;
        doorLight.enabled = false;
    }
}