using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image blackOut;
    [SerializeField] private float fadeOutTime;

    void Start()
    {
        Hide();
        GameManager.Instance.OnGameWin += StopAllCoroutines;
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    public IEnumerator Show()
    {
        canvas.enabled = true;
        yield return FadeOut();
    }

    private IEnumerator FadeOut()
    {
        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable("death static");
        float elapedTime = 0;

        while (elapedTime < fadeOutTime)
        {
            if (audioSource != null) audioSource.volume = 1 / (elapedTime / fadeOutTime);
            blackOut.color = new Color(0, 0, 0, elapedTime / fadeOutTime);
            elapedTime += Time.deltaTime;
            yield return null;
        }

        GameAudioManager.Instance.StopSfx(audioSource);
        Hide();
    }
}
