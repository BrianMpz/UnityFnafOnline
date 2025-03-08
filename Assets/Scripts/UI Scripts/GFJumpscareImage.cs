using System.Collections;
using UnityEngine;

public class GFJumpscareImage : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    private void Start()
    {
        Hide();
    }

    public IEnumerator PlayJumpscare()
    {
        GameAudioManager.Instance.StopAllSfx();

        AudioSource audioSource = GameAudioManager.Instance.PlaySfxInterruptable("gf jumpscare scream");
        Show();

        yield return new WaitForSeconds(2f);

        GameAudioManager.Instance.StopSfx(audioSource);
        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            StartCoroutine(PlayJumpscare());
        }
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
