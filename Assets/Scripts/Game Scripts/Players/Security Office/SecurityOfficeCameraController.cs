using UnityEngine;

public class SecurityOfficeCameraController : CameraController
{
    private float minRotationY = 140f;
    private float maxRotationY = 220f;

    public override void SetCameraView()
    {
        if (!canPlayerControlCamera) return;

        float normalizedMouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
        float targetRotationY = Mathf.Clamp(Mathf.Lerp(100, 260, normalizedMouseX), minRotationY, maxRotationY);

        // Get the current rotation
        Vector3 currentRotation = playerView.eulerAngles;

        // Smoothly interpolate towards the target rotation
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, targetRotationY, Time.deltaTime * 5f);

        // Apply the new rotation to the camera
        playerView.eulerAngles = currentRotation;
    }

    public override void LerpTowardsDeathView()
    {
        float targetRotationY = 180;
        float targetRotationX = -6;
        float targetFovX = 30;

        Vector3 currentRotation = playerView.transform.eulerAngles;
        currentRotation.x = Mathf.LerpAngle(currentRotation.x, targetRotationX, Time.deltaTime * 10f);
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, targetRotationY, Time.deltaTime * 10f);
        playerView.eulerAngles = currentRotation;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFovX, Time.deltaTime * 10f);
    }

    private protected override void Initialise()
    {
        base.Initialise();
        playerView.eulerAngles = new Vector3(0, 180, 0);
        cam.fieldOfView = 60;
    }
}
