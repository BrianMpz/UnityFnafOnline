using System.Collections;
using UnityEngine;

public class Hallucinations : Singleton<Hallucinations>
{
    [SerializeField] private GameObject[] images;
    [SerializeField] private float timeStep;

    private Coroutine hallucinationRoutine;

    private void Start()
    {
        GameManager.Instance.OnGameWin += StopHallucinations;
        GameManager.Instance.OnGameOver += StopHallucinations;
        DisableAllImages();
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameWin -= StopHallucinations;
        GameManager.Instance.OnGameOver -= StopHallucinations;
    }

    private void StopHallucinations()
    {
        if (hallucinationRoutine != null)
        {
            StopCoroutine(hallucinationRoutine);
            hallucinationRoutine = null;
        }

        DisableAllImages();
    }

    public void StartHallucination(float duration = 10f)
    {
        if (hallucinationRoutine != null)
            StopCoroutine(hallucinationRoutine);

        hallucinationRoutine = StartCoroutine(PlayHallucination(GameManager.localPlayerBehaviour, duration));
    }

    private IEnumerator PlayHallucination(PlayerBehaviour playerBehaviour, float duration)
    {
        AudioSource sound = GameAudioManager.Instance.PlaySfxInterruptable("robot voice", false);
        float elapsedTime = 0f;

        bool startedFade = false;

        while (elapsedTime < duration)
        {
            if (!playerBehaviour.isPlayerAlive.Value) break;

            DisableAllImages();

            if (Random.Range(0, 4) == 0) // 25% chance
                images[Random.Range(0, images.Length)].SetActive(true);

            // Start fading audio if within the last second and not already fading
            if (!startedFade && duration - elapsedTime <= 1f && sound != null)
            {
                StartCoroutine(FadeOutAudio(sound, 1f));
                startedFade = true;
            }

            yield return new WaitForSeconds(timeStep);
            elapsedTime += timeStep;
        }

        DisableAllImages();
        hallucinationRoutine = null;
    }

    private IEnumerator FadeOutAudio(AudioSource audioSource, float fadeDuration)
    {
        float startVolume = audioSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // reset volume if needed for future reuse
    }

    private void DisableAllImages()
    {
        foreach (GameObject image in images)
            image.SetActive(false);
    }
}
