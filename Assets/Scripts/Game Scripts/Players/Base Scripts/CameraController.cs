using UnityEngine;
using UnityEngine.SocialPlatforms;

public abstract class CameraController : MonoBehaviour
{
    [SerializeField] private protected PlayerBehaviour playerBehaviour;
    [SerializeField] private protected AudioListener audioListener;
    [SerializeField] private protected bool canPlayerControlCamera;
    public Camera cam;
    public Transform playerView;
    public abstract void SetCameraView();
    public abstract void LerpTowardsDeathView();

    private void Start()
    {
        playerBehaviour.OnInitialise += Initialise;
        playerBehaviour.OnDisable += Disable;
        playerBehaviour.OnKill += () => { canPlayerControlCamera = false; };
    }

    private protected virtual void Initialise()
    {
        GameManager.Instance.DefaultAudioListener.enabled = false;
        audioListener.enabled = true;

        cam.enabled = true;
        canPlayerControlCamera = true;
    }

    public virtual void Disable()
    {
        if (GameManager.localPlayerBehaviour == playerBehaviour) GameManager.Instance.DefaultAudioListener.enabled = true;
        audioListener.enabled = false;

        cam.enabled = false;
        canPlayerControlCamera = false;
    }
}
