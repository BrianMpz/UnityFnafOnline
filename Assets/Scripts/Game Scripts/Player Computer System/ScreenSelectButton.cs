using UnityEngine;
using UnityEngine.UI;

public class ScreenSelectButton : MonoBehaviour
{
    [SerializeField] private ComputerScreen computerScreen;
    [SerializeField] private PlayerComputer playerComputer;
    private Button Button { get => GetComponent<Button>(); }
    private Image Image { get => GetComponent<Image>(); }

    private void Start()
    {
        Button.onClick.AddListener(Select);
        playerComputer.OnComputerScreenChanged += HandleComputerScreenChanged;
        Deselected();
    }

    private void HandleComputerScreenChanged(ComputerScreen computerScreen)
    {
        if (this.computerScreen != computerScreen)
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
        GameAudioManager.Instance.PlaySfxOneShot("camera blip");
        playerComputer.SetComputerScreen(computerScreen);
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
