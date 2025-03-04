using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraData : MonoBehaviour
{
    private Camera Cam { get => GetComponent<Camera>(); }
    [SerializeField] private CameraName cameraName;
    [SerializeField] private RenderTexture cameraTexture;
    public Node[] nodesVisibleOnCamera;
    public bool isCurrentlyHidden;
    public bool isAudioOnly;
    public bool isSecurityOfficeOnly;
    public Light cameraFlashlight;
    public float startingRange;

    private void Awake()
    {
        Cam.targetTexture = cameraTexture;
        startingRange = cameraFlashlight.range;
    }

    public CameraName GetCameraName() => cameraName;
    public RenderTexture GetRenderTexture() => cameraTexture;
    public Camera GetCamera() => Cam;
}
