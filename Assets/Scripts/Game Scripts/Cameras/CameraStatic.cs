using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraStatic : MonoBehaviour
{
    [SerializeField] private RawImage staticImage;
    public AudioSource staticaudio;
    public AudioSource disturbanceAudio;
    [SerializeField] private float fadeOutTime; // Total duration for the fade-out effect
    [SerializeField] private float minimumAlpha;
    [SerializeField] private float minimumVolume;

    public void RefreshMonitorStatic(bool isHidden)
    {
        StopAllCoroutines(); // Stop any previous fade-out coroutines
        StartCoroutine(PlayStaticCoroutine(isHidden));
    }

    private IEnumerator PlayStaticCoroutine(bool isHidden)
    {
        staticImage.enabled = true;
        staticImage.color = new Color(staticImage.color.r, staticImage.color.g, staticImage.color.b, 1f);
        if (staticaudio != null) staticaudio.volume = 0.5f;

        float elapsedTime = 0f;

        if (isHidden) yield break;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(1f, minimumAlpha, elapsedTime / fadeOutTime);
            float newVolume = Mathf.Lerp(1f, minimumVolume, elapsedTime / fadeOutTime);

            staticImage.color = new Color(staticImage.color.r, staticImage.color.g, staticImage.color.b, newAlpha);
            if (staticaudio != null) staticaudio.volume = newVolume;
            yield return null;
        }

        staticImage.color = new Color(staticImage.color.r, staticImage.color.g, staticImage.color.b, minimumAlpha);
    }
}
