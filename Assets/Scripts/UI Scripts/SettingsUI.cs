using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : Singleton<SettingsUI>
{
    [SerializeField] private Button closeButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        closeButton.onClick.AddListener(Hide);
        Hide();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
