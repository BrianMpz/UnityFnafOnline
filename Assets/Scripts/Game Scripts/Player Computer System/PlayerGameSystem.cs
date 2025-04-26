using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGameSystem : NetworkBehaviour
{
    public PlayerComputer playerComputer;
    private AudioSource gameMusic;
    public Canvas canvas;
    [SerializeField] private NetworkVariable<GameState> currentGameState = new(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] private GameObject titleScreenUI;
    [SerializeField] private Button playButton;

    [SerializeField] private FreddyInSpace gameSceneUI;
    [SerializeField] private GameObject UIForSpectators;
    public NetworkVariable<bool> isPlaying = new(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> nasirVersion = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private GameObject nasirPoster;
    [SerializeField] private GameObject nasirShip;
    [SerializeField] private GameObject helpyShip;

    private void Start()
    {
        playButton.onClick.AddListener(StartGame);
    }

    private void StartGame()
    {
        GameAudioManager.Instance.PlaySfxOneShot("select 1", false);

        SetGameState(GameState.Gameplay);
        StartCoroutine(gameSceneUI.WaitToStartGame());
    }

    public void Initialise(Camera playerCamera)
    {
        canvas.worldCamera = playerCamera;
        SetGameState(GameState.TitleScreen);
        Disable();
    }

    public void Enable()
    {
        canvas.enabled = true;
        EnableServerRpc();

        nasirPoster.SetActive(nasirVersion.Value);
        nasirShip.SetActive(nasirVersion.Value);
        helpyShip.SetActive(!nasirVersion.Value);

        gameMusic = GameAudioManager.Instance.PlaySfxInterruptable(!nasirVersion.Value ? "just add water" : "king nasir theme", false, volume: 0.5f, loop: true); // this music can be interrupted
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableServerRpc(ServerRpcParams serverRpcParams = default)
    => EnableClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void EnableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        canvas.enabled = true;

        DisableAllUI();
        UIForSpectators.SetActive(true);
    }

    public void Disable()
    {
        canvas.enabled = false;
        DisableServerRpc();

        GameAudioManager.Instance.StopSfx(gameMusic);

        if (currentGameState.Value == GameState.Gameplay)
        {
            if (isPlaying.Value) gameSceneUI.PauseGame();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableServerRpc(ServerRpcParams serverRpcParams = default)
    => DisableClientRpc(serverRpcParams.Receive.SenderClientId);

    [ClientRpc]
    private void DisableClientRpc(ulong ignoreId)
    {
        if (NetworkManager.Singleton.LocalClientId == ignoreId) return;

        canvas.enabled = false;
    }

    private void DisableAllUI()
    {
        titleScreenUI.SetActive(false);
        gameSceneUI.gameObject.SetActive(false);
        UIForSpectators.SetActive(false);
    }

    private void SetGameState(GameState gameState)
    {
        DisableAllUI();

        currentGameState.Value = gameState;

        // Show appropriate UI depending on game state
        if (currentGameState.Value == GameState.TitleScreen)
            titleScreenUI.SetActive(true);
        else if (currentGameState.Value == GameState.Gameplay)
            gameSceneUI.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!IsOwner || currentGameState.Value != GameState.Gameplay) return;
    }

    private enum GameState
    {
        TitleScreen,
        Gameplay
    }
}
