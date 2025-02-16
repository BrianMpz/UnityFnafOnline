using UnityEngine;
using Unity.Netcode;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance;
    [SerializeField] private bool dontDestroyOnLoad;

    private void OnEnable()
    {
        Instance = this as T;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }
}

public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public static T Instance;
    [SerializeField] private bool dontDestroyOnLoad;

    private void OnEnable()
    {
        Instance = this as T;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }
}

