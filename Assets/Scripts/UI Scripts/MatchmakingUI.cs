using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchmakingUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInputField;
    [SerializeField] private TMP_Text playerPlayerNameText;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button backToMainMenuButtton;

    private void Start()
    {
        string gameCode = PlayerPrefs.GetString("GameCode", "");
        if (gameCode != "") roomCodeInputField.text = gameCode;

        roomCodeInputField.onValueChanged.AddListener((string s) => { roomCodeInputField.text = s.ToUpper(); });
        joinGameButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.JoinOnlineRoomAsync(joinCode: roomCodeInputField.text);
        });

        hostGameButton.onClick.AddListener(MultiplayerManager.Instance.HostOnlineRoomAsync);

        backToMainMenuButtton.onClick.AddListener(() =>
        {
            Loader.LoadScene(Loader.Scene.MainMenu);
        });


        string playerName = PlayerPrefs.GetString(MultiplayerManager.PlayerprefsPlayerNameLocation, "");
        if (playerName == "") playerName = "Player" + Random.Range(1000, 10000).ToString();

        MultiplayerManager.Instance.playerName = playerName;
        playerPlayerNameText.text = $"Player Name: " + MultiplayerManager.Instance.playerName;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && ServicesInitialiser.Instance.areServicesInitialised)
        {
            MultiplayerManager.Instance.JoinOnlineRoomAsync(joinCode: roomCodeInputField.text);
        }
    }
}
