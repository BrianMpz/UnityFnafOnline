using UnityEngine;

public class ZapButton : MonoBehaviour
{
    [SerializeField] private BackstagePlayerBehaviour backstagePlayerBehaviour;

    void OnMouseDown()
    {
        backstagePlayerBehaviour.Zap();
    }
}
