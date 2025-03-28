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
    [SerializeField] private NetworkVariable<float> currentMovementWaitTime = new(writePerm: NetworkVariableWritePermission.Owner);

    private void Start()
    {
        GameManager.Instance.OnGameStarted += () => { StartCoroutine(GameplayLoop()); };
        DebugCanvasUI.Instance.OnBuff += () =>
        {
            currentDifficulty.Value += 2f;
        };
    }

    public void OnButtonPress(string number)
    {
        GameAudioManager.Instance.PlaySfxOneShot("keypad button press");
        currentCombination += number;
    }

    private void Update()
    {
        if (!IsOwner || !GameManager.Instance.isPlaying) return;

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
            yield return new WaitForSeconds(UnityEngine.Random.Range(5, 60));
            if (UnityEngine.Random.Range(1, 20) <= currentDifficulty.Value && PlayerRoleManager.Instance.securityOfficeBehaviour.isPlayerAlive.Value)
            {
                yield return PlayCallAudio();

                currentCombination = "";
                requriedCombination = "";

                for (int i = 0; i < 5; i++)
                {
                    string newRequiredNumber = UnityEngine.Random.Range(1, 10).ToString();
                    requriedCombination += newRequiredNumber;

                    GameAudioManager.Instance.PlaySfxOneShot($"number 0{newRequiredNumber}");
                    yield return new WaitForSeconds(Mathf.Lerp(2, 1, currentDifficulty.Value / 20));
                }
                yield return new WaitForSeconds(1);

                if (currentCombination == requriedCombination)
                {
                    Debug.Log("success");
                }
                else
                {
                    GameAudioManager.Instance.PlaySfxOneShot("failed sfx");

                    int randInt = UnityEngine.Random.Range(1, 2 + 1);

                    switch (randInt)
                    {
                        case 1:
                            DebugCanvasUI.Instance.OnBuff?.Invoke();
                            break;
                        case 2:
                            PlayerRoleManager.Instance.backstageBehaviour.maintenance.SetAllSystemsStateServerRpc(State.OFFLINE);
                            break;
                    }
                }
            }
        }
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
