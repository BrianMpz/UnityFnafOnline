using System.Collections;
using UnityEngine;

public class GFJumpscareImage : Singleton<GFJumpscareImage>
{
    [SerializeField] private Canvas canvas;

    private void Start()
    {
        Hide();
    }

    public IEnumerator PlayJumpscare()
    {
        GameAudioManager.Instance.StopAllSfx();

        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable("gf jumpscare scream", false);
        Show();

        yield return new WaitForSeconds(2f);

        GameAudioManager.Instance.StopSfx(audioSource);
        Hide();
    }

    public void Hide()
    {
        canvas.enabled = false;
    }

    public void Show()
    {
        canvas.enabled = true;
    }
}
