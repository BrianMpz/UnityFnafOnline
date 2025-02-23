using UnityEngine;

public class ComputerFlipAnimationFinished : MonoBehaviour
{
    public void OnComputerFlipAnimationFinished()
    {
        GetComponentInChildren<PlayerComputer>().MonitorFlipFinished();
    }
}
