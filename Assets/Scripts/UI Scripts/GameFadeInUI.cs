using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameFadeInUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image blackScreen;
    [SerializeField] private float fadeOutTime;

    private void Start()
    {
        GameManager.Instance.OnGameStarted += FadeOut;
        Hide();
    }

    public void FadeOut()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    public IEnumerator FadeOutCoroutine()
    {
        Show();
        blackScreen.color = new Color(0, 0, 0, 1);
        float elapedTime = 0;

        yield return new WaitForSeconds(1f);
        while (elapedTime < fadeOutTime)
        {
            blackScreen.color = new Color(0, 0, 0, 1 - (elapedTime / fadeOutTime));
            elapedTime += Time.deltaTime;
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
