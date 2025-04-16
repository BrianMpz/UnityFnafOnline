using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LureButton : MonoBehaviour
{
    [SerializeField] private PlayerAudioLureSystem playerAudioLureSystem;
    [SerializeField] private Image radius;
    [SerializeField] private Button button;
    [SerializeField] private NodeName nodeName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        radius.color = new(0, 0, 0, 0);
        button.onClick.AddListener(TryPlayLure);
        playerAudioLureSystem.OnLurePlayed += OnLurePlayed;
    }

    private void OnLurePlayed(NodeName name, float lureDuration)
    {
        if (name == nodeName) StartCoroutine(DecayRadius(lureDuration));
    }

    private void TryPlayLure()
    {
        GameAudioManager.Instance.PlaySfxOneShot("camera blip", false);
        playerAudioLureSystem.PlayLureServerRpc(nodeName);
    }

    public IEnumerator DecayRadius(float duration)
    {
        radius.color = new Color(1, 1, 1, .5f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(.2f, 0f, elapsedTime / duration);

            radius.color = new Color(radius.color.r, radius.color.g, radius.color.b, newAlpha);
            yield return null;
        }

        radius.color = new Color(1, 1, 1, 0);
    }
}
