using System;
using System.Collections;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
