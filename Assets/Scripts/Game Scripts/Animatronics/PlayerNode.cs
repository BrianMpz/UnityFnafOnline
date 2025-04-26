using UnityEngine;
using UnityEngine.UI;

public class PlayerNode : Node
{
    public PlayerBehaviour playerBehaviour;
    public bool IsAlive
    {
        get => playerBehaviour.isPlayerAlive.Value;
    }

    void Awake()
    {
        GetComponent<Image>().color = Color.white;
        if (playerBehaviour != null) playerBehaviour.isPlayerAlive.OnValueChanged += IsAliveChanged;
    }

    private void IsAliveChanged(bool _, bool newValue)
    {
        GetComponent<Image>().color = newValue ? Color.green : Color.red;
    }
}
