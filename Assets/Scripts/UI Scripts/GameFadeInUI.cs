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

    public void FadeOut(float fadeOutTime = 5, bool oneSecondBlackout = true)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutCoroutine(fadeOutTime, oneSecondBlackout));
    }

    public IEnumerator FadeOutCoroutine(float fadeOutTime, bool oneSecondBlackout)
    {
        Show();
        blackScreen.color = new Color(0, 0, 0, 1);

        if (oneSecondBlackout) yield return new WaitForSeconds(1.5f);

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
