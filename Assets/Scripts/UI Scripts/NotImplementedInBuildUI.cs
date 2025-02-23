using UnityEngine;
using UnityEngine.UI;

public class NotImplementedInBuildUI : Singleton<NotImplementedInBuildUI>
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button closeButton;

    void Start()
    {
        closeButton.onClick.AddListener(() =>
        {
            Hide();
        });

        Hide();
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    public void Show()
    {
        canvas.enabled = true;
    }
}
