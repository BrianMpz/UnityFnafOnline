using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameFadeInUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image blackScreen;

    private void Start()
    {
        GameManager.Instance.OnGameStarted += () => { FadeOut(); };
        Hide();
    }

    public void FadeOut(float fadeOutTime = 6)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutCoroutine(fadeOutTime));
    }

    public IEnumerator FadeOutCoroutine(float fadeOutTime)
    {
        Show();
        blackScreen.color = new Color(0, 0, 0, 1);
        float elapsedTime = 0;

        while (elapsedTime < fadeOutTime)
        {
            blackScreen.color = new Color(0, 0, 0, 1 - (elapsedTime / fadeOutTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Hide();
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
