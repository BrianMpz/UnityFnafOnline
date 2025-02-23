using UnityEngine;

public class PlayerManual : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    public void Initialise(Camera playerCamera)
    {
        canvas.worldCamera = playerCamera;
        Disable();
    }

    public void Enable()
    {
        canvas.enabled = true;
    }

    public void Disable()
    {
        canvas.enabled = false;
    }
}

