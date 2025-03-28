using UnityEngine;
using UnityEngine.UI;

public class KeypadButton : MonoBehaviour
{
    [SerializeField] private KeypadSystem keypadSystem;
    [SerializeField] private string number;

    void OnMouseDown() => keypadSystem.OnButtonPress(number);
}
