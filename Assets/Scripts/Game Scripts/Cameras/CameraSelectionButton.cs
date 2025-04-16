using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CameraSelectionButton : MonoBehaviour
{
    [SerializeField] private CameraName targetCameraName;
    [SerializeField] private PlayerCameraSystem PlayerCameraSystem;
    private Button Button { get => GetComponent<Button>(); }
    private Image Image { get => GetComponentInChildren<Image>(); }

    private void Start()
    {
        Button.onClick.AddListener(Select);
        PlayerCameraSystem.OnCameraViewChanged += HandleCameraViewChanged;
        Deselected();
    }

    private void HandleCameraViewChanged(CameraName switchedCameraName)
    {
        if (switchedCameraName != targetCameraName)
        {
            Deselected();
        }
        else
        {
            Selected();
        }
    }

    private void Select()
    {
        GameAudioManager.Instance.PlaySfxOneShot("camera blip", false);
        PlayerCameraSystem.SetCamera(targetCameraName);
    }

    private void Deselected()
    {
        Button.enabled = true;
        Image.color = Button.colors.normalColor;
    }

    private void Selected()
    {
        Button.enabled = false;
        Image.color = Button.colors.selectedColor;
    }
}
