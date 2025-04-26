using UnityEngine;

public class FreddyNose : MonoBehaviour
{
    [SerializeField] private FreddyPoster freddyPoster;
    void OnMouseDown()
    {
        GameAudioManager.Instance.PlaySfxInterruptable("freddy nose honk", true);

        if (Random.Range(1, 100 + 1) == 1) StartCoroutine(freddyPoster.KillPlayer());
    }
}
