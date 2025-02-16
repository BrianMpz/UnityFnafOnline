using System.Collections;
using UnityEngine;

public class MaintainAspectRatio : Singleton<MaintainAspectRatio>
{
    [SerializeField] private float targetAspectRatio = 16.0f / 9.0f; // Set your desired aspect ratio here (e.g., 16:9)

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
            Instance = this;
        }

        // Continuously check and adjust resolution
        StartCoroutine(AdjustResolution());
    }

    private IEnumerator AdjustResolution()
    {
        while (true)
        {
            // Current screen aspect ratio
            float currentAspectRatio = Screen.width / Screen.height;

            if (Mathf.Approximately(currentAspectRatio, targetAspectRatio))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            int width = Screen.width;
            int height = Mathf.RoundToInt(width / targetAspectRatio);

            if (height > Screen.height)
            {
                height = Screen.height;
                width = Mathf.RoundToInt(height * targetAspectRatio);
            }

            Screen.SetResolution(width, height, Screen.fullScreen);

            yield return new WaitForSeconds(0.5f);
        }
    }
}
