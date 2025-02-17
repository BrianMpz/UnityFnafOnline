using UnityEngine;

public class Zap : Animatronic
{
    public override void Initialise() // dont call base class
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 1;
                currentMovementWaitTime.Value = 4;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 2;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 4;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 7;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 11;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 4f;
                break;
        }

    }
}
