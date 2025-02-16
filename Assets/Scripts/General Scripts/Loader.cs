using Unity.Netcode;
using UnityEngine.SceneManagement;


public static class Loader
{
    public enum Scene // must be changed to fit game
    {
        MainMenu,
        Matchmaking,
        Lobby,
        Game
    }

    public static void LoadScene(Scene targetScene)
    {
        SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoadNetworkScene(Scene targetScene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

}
