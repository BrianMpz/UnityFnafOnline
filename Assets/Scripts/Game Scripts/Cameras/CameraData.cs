using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraData : MonoBehaviour
{
    private Camera Cam { get => GetComponent<Camera>(); }
    [SerializeField] private CameraName cameraName;
    [SerializeField] private RenderTexture cameraTexture;
    public Node[] nodesVisibleOnCamera;
    public bool isHidden;
    public Light cameraFlashlight;

    private void Awake()
    {
        Cam.targetTexture = cameraTexture;
    }

    public CameraName GetCameraName() => cameraName;
    public RenderTexture GetRenderTexture() => cameraTexture;
}
