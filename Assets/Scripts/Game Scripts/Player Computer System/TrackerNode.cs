using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TrackerNode : MonoBehaviour
{
    public NodeName nodeName;
    [SerializeField] private Color blinkColor;
    [SerializeField] private float fadeOutTime;

    public IEnumerator Blink()
    {
        Image image = GetComponent<Image>();

        image.color = blinkColor;

        float elapsedTime = 0;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;

            image.color = Color.Lerp(blinkColor, new(0, 0, 0, 0), elapsedTime / fadeOutTime);
            yield return null;
        }
    }

}
