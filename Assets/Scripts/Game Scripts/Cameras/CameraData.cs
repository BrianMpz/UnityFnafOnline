using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraData : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private CameraName cameraName;
    [SerializeField] private RenderTexture cameraTexture;
    public Node[] nodesVisibleOnCamera;
    public bool isCurrentlyHidden;
    public bool isAudioOnly;
    public bool isSecurityOfficeOnly;
    public Light cameraFlashlight;
    [HideInInspector] public float startingRange;
    [HideInInspector] public float startingIntensity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.targetTexture = cameraTexture;
        startingRange = cameraFlashlight.range;
        startingIntensity = cameraFlashlight.intensity;
    }

    public CameraName GetCameraName() => cameraName;
    public RenderTexture GetRenderTexture() => cameraTexture;
    public Camera GetCamera() => cam;
}
