using UnityEngine;
using UnityEngine.InputSystem;

public class GhostWaveInstructionDismiss : MonoBehaviour
{
    [SerializeField] private GameObject leftClickInstruction;
    [SerializeField] private GameObject rightClickInstruction;

    private void Update()
    {
        if (Mouse.current == null) return;

        if (leftClickInstruction != null && leftClickInstruction.activeSelf
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            leftClickInstruction.SetActive(false);
        }

        if (rightClickInstruction != null && rightClickInstruction.activeSelf
            && Mouse.current.rightButton.wasPressedThisFrame)
        {
            rightClickInstruction.SetActive(false);
        }
    }
}
