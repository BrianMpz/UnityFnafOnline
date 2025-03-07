using UnityEngine;

public class JanitorCameraController : CameraController
{
    [SerializeField] private Transform DeathView;
    [SerializeField] private float cameraLerpSpeed;

    public override void LerpTowardsDeathView()
    {
        Vector3 currentRotation = playerView.transform.eulerAngles;
        currentRotation.z = Mathf.LerpAngle(currentRotation.z, DeathView.eulerAngles.z, Time.deltaTime * cameraLerpSpeed);
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, DeathView.eulerAngles.y, Time.deltaTime * cameraLerpSpeed);
        currentRotation.x = Mathf.LerpAngle(currentRotation.x, DeathView.eulerAngles.x, Time.deltaTime * cameraLerpSpeed);
        playerView.eulerAngles = currentRotation;

        Vector3 currentPosition = playerView.transform.position;
        currentPosition = Vector3.Lerp(currentPosition, DeathView.position, Time.deltaTime * cameraLerpSpeed);
        playerView.position = currentPosition;
    }

    public override void SetCameraView()
    {
        // camera view is static so dont implement
    }

    private protected override void Initialise()
    {
        base.Initialise();
        cam.fieldOfView = 60;
    }

}
