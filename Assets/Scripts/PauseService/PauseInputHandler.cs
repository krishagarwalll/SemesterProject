using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PauseInputHandler : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PauseService.Toggle();
        }
    }
}
