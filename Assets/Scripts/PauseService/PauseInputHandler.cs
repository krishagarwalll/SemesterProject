using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PauseInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction;

    private void OnEnable()
    {
        PauseService.SetPauseBypass(this, PauseType.Input, true);
        pauseAction.SetEnabled(true);
    }

    private void OnDisable()
    {
        pauseAction.SetEnabled(false);
        PauseService.SetPauseBypass(this, PauseType.Input, false);
    }

    private void Update()
    {
        if (IsCutscenePlaying()) return;
        if (WasPausePressed())
        {
            PauseService.Toggle();
        }
    }

    public void OnResumePressed()
    {
        PauseService.Resume();
    }

    public void OnQuitPressed()
    {
        RuntimeUiUtility.QuitApplication();
    }

    private static bool IsCutscenePlaying()
    {
        CutsceneController[] cutscenes = FindObjectsByType<CutsceneController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cutscenes.Length; i++)
        {
            if (cutscenes[i] && !cutscenes[i].HasEnded)
            {
                return true;
            }
        }

        return false;
    }

    private bool WasPausePressed()
    {
        if (pauseAction.WasPressedThisFrame())
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null
            && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null
            && (gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame))
        {
            return true;
        }

        try
        {
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }
}
