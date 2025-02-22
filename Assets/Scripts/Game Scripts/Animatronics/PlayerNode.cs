using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNode : Node
{
    public PlayerBehaviour playerBehaviour;
    public bool IsAlive
    {
        get => playerBehaviour.isAlive.Value;
    }

    void Awake()
    {
        GetComponent<Image>().color = Color.white;
        if (playerBehaviour != null) playerBehaviour.isAlive.OnValueChanged += IsAliveChanged;
    }

    private void IsAliveChanged(bool previousValue, bool newValue)
    {
        GetComponent<Image>().color = newValue ? Color.green : Color.red;
    }
}
