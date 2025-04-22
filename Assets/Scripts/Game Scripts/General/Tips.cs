using System.Collections.Generic;
using UnityEngine;

public class Tips : MonoBehaviour
{
    [SerializeField] private PlayerBehaviour playerBehaviour;
    [SerializeField] private GameObject tipsObject;

    // Tracks which roles have already seen the tips this session
    public static HashSet<PlayerRoles> rolesThatHaveSeenTips = new();

    private void Start()
    {
        playerBehaviour.OnInitialise += Initialise;
    }

    private void Initialise()
    {
        bool alreadySeen = rolesThatHaveSeenTips.Contains(playerBehaviour.playerRole);

        tipsObject.SetActive(!alreadySeen);
    }

    private void Update()
    {
        if (GameManager.localPlayerBehaviour != playerBehaviour) return;

        bool alreadySeen = rolesThatHaveSeenTips.Contains(playerBehaviour.playerRole);
        tipsObject.SetActive((!playerBehaviour.playerComputer.isMonitorUp.Value || playerBehaviour.playerComputer.isMonitorAlwaysUp) && !alreadySeen);

        // Dismiss with X
        if (Input.GetKeyDown(KeyCode.X) && !alreadySeen)
        {
            rolesThatHaveSeenTips.Add(playerBehaviour.playerRole);
            tipsObject.SetActive(false);
        }
    }
}
