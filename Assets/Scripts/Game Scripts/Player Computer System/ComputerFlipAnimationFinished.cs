using UnityEngine;

public class ComputerFlipAnimationFinished : MonoBehaviour
{
    [SerializeField] private PlayerComputer playerComputer;
    public void OnComputerFlipAnimationFinished()
    {
        playerComputer.MonitorFlipFinished();
    }
}
