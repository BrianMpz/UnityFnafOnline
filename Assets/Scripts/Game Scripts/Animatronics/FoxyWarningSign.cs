using TMPro;
using UnityEngine;

public class FoxyWarningSign : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text text;

    private void Start()
    {
        Hide();
    }

    public void UpdateWarningSign(PlayerRoles playerRole)
    {
        text.text = MultiplayerManager.Instance.GetPlayerDataFromPlayerRole(playerRole).playerName.ToString();
        Show();
    }

    public void UpdateWarningSign(string signText)
    {
        text.text = signText;
        Show();
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    private void Show()
    {
        canvas.enabled = true;
    }
}
