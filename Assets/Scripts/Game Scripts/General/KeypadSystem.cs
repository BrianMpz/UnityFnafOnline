using System;
using System.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class KeypadSystem : NetworkBehaviour
{
    [SerializeField] private string requriedCombination;
    [SerializeField] private string currentCombination;
    [SerializeField] private NetworkVariable<float> currentDifficulty = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private AudioSource alarm;
    [SerializeField] private Light alarmLight;

    private void Start()
    {
        GameManager.Instance.OnGameStarted += () => { StartCoroutine(GameplayLoop()); };
        DebugCanvasUI.Instance.OnBuff += () =>
        {
            if (IsOwner) currentDifficulty.Value += 2f;
        };
        alarmLight.enabled = false;
    }

    public void OnButtonPress(string number)
    {
        GameAudioManager.Instance.PlaySfxOneShot("keypad button press");
        currentCombination += number;
    }

    private void Update()
    {
        if (!PlayerRoleManager.Instance.IsControllingPlayer(PlayerRoles.SecurityOffice) || !GameManager.Instance.isPlaying) return;

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
            {
                OnButtonPress(i.ToString());
            }
        }
    }

    public IEnumerator GameplayLoop()
    {
        if (!IsOwner) yield break;
        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 2f;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 5f;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 8f;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 11f;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 14f;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 17f;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20f;
                break;
        }

        while (GameManager.Instance.isPlaying)
        {
            yield return new WaitForSeconds(10 + Mathf.Lerp(30f, 10f, currentDifficulty.Value / 20f));
            if (UnityEngine.Random.Range(1, 20) > currentDifficulty.Value || !PlayerRoleManager.Instance.securityOfficeBehaviour.isPlayerAlive.Value || !GameManager.Instance.isPlaying) continue;

            yield return PlayCallAudio();

            currentCombination = "";
            requriedCombination = "";

            for (int i = 0; i < 5; i++)
            {
                string newRequiredNumber = UnityEngine.Random.Range(1, 10).ToString();
                requriedCombination += newRequiredNumber;

                AudioSource number = GameAudioManager.Instance.PlaySfxInterruptable($"number 0{newRequiredNumber}");
                number.pitch = 0.9f;
                yield return new WaitForSeconds(Mathf.Lerp(2, 1, currentDifficulty.Value / 20f));
            }
            yield return new WaitForSeconds(1);

            if (currentCombination == requriedCombination)
            {
                GameAudioManager.Instance.PlaySfxOneShot("select 1");
            }
            else
            {
                if (PlayerRoleManager.Instance.securityOfficeBehaviour.isPlayerAlive.Value && GameManager.Instance.isPlaying) AlertAllAnimatronicsServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AlertAllAnimatronicsServerRpc()
    {
        PlayerNode playerNode = AnimatronicManager.Instance.GetPlayerNodeFromPlayerRole(PlayerRoles.SecurityOffice);
        AnimatronicManager.Instance.AttentionDivert?.Invoke(playerNode);
        AlertAllAnimatronicsClientRpc();
        DebugCanvasUI.Instance.OnBuff?.Invoke();
    }

    [ClientRpc]
    private void AlertAllAnimatronicsClientRpc()
    {
        StartCoroutine(AlarmLightEffect());
    }

    private IEnumerator AlarmLightEffect()
    {
        float duration = 9f;
        float fadeOutDuration = 2f; // Duration for light to fade out
        float elapsedTime = 0f;

        alarm.Play();
        yield return new WaitForSeconds(0.3f);
        alarmLight.enabled = true;

        // Main flashing effect
        while (elapsedTime < duration)
        {
            float intensity = Mathf.Lerp(5f, 10f, Mathf.PingPong(Time.time * 2f, 1f));
            alarmLight.intensity = intensity;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Smooth fade out of the light after the alarm stops
        float startIntensity = alarmLight.intensity;
        elapsedTime = 0f;
        alarm.Stop();

        while (elapsedTime < fadeOutDuration)
        {
            alarmLight.intensity = Mathf.Lerp(startIntensity, 0f, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        alarmLight.intensity = 0f;
        alarmLight.enabled = false; // Ensure the light is completely off
    }

    private IEnumerator PlayCallAudio()
    {
        AudioSource callAudio = GameAudioManager.Instance.PlaySfxInterruptable("calling");
        callAudio.pitch = 1.2f;
        yield return new WaitForSeconds(1);
        GameAudioManager.Instance.StopSfx(callAudio);
        GameAudioManager.Instance.PlaySfxOneShot("call pick up");
        yield return new WaitForSeconds(1f);
    }
}
