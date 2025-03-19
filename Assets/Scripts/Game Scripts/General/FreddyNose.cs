using UnityEngine;

public class FreddyNose : MonoBehaviour
{
    [SerializeField] private FreddyPoster freddyPoster;
    void OnMouseDown()
    {
        GameAudioManager.Instance.PlaySfxOneShot("freddy nose honk");

        if (Random.Range(1, 100 + 1) <= 5) StartCoroutine(freddyPoster.KillPlayer());
    }
}
