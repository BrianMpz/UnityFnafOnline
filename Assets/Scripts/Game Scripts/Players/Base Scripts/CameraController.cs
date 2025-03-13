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
        playerBehaviour.OnPlayerJumpscare += () => { canPlayerControlCamera = false; };
    }

    private protected virtual void Initialise()
    {
        GameManager.Instance.DefaultAudioListener.enabled = false;
        audioListener.enabled = true;

        GameManager.Instance.DefaultCamera.enabled = false;
        cam.enabled = true;

        canPlayerControlCamera = true;
    }

    public virtual void Disable()
    {
        if (GameManager.localPlayerBehaviour != playerBehaviour) return;

        GameManager.Instance.DefaultAudioListener.enabled = true;
        audioListener.enabled = false;

        GameManager.Instance.DefaultCamera.enabled = true;
        cam.enabled = false;

        canPlayerControlCamera = false;
    }
}
