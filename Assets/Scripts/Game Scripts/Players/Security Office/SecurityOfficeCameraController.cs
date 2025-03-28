using UnityEngine;

public class SecurityOfficeCameraController : CameraController
{
    private float minRotationY = 120f;
    private float maxRotationY = 240f;
    private float minRotationX = 6f;
    private float maxRotationX = -6f;

    public override void SetCameraView()
    {
        if (!canPlayerControlCamera) return;

        float normalizedMouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
        float normalizedMouseY = Mathf.Clamp01(Input.mousePosition.y / Screen.height);

        float targetRotationY = Mathf.Clamp(Mathf.Lerp(70, 290, normalizedMouseX), minRotationY, maxRotationY);
        float targetRotationX = Mathf.Lerp(minRotationX, maxRotationX, normalizedMouseY);

        // Get the current rotation
        Vector3 currentRotation = playerView.eulerAngles;

        // Smoothly interpolate towards the target rotation
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, targetRotationY, Time.deltaTime * 5f);
        currentRotation.x = Mathf.LerpAngle(currentRotation.x, targetRotationX, Time.deltaTime * 5f);

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
