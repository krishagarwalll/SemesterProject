using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PauseInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction;

    private InputAction runtimePauseAction;
    private InputAction activePauseAction;
    private int lastToggleFrame = -1;

    private void OnEnable()
    {
        PauseService.SetPauseBypass(this, PauseType.Input, true);
        activePauseAction = GetOrCreatePauseAction();
        activePauseAction.performed += HandlePausePerformed;
        activePauseAction.Enable();
    }

    private void OnDisable()
    {
        if (activePauseAction != null)
        {
            activePauseAction.performed -= HandlePausePerformed;
            activePauseAction.Disable();
            activePauseAction = null;
        }

        PauseService.SetPauseBypass(this, PauseType.Input, false);
    }

    private void OnDestroy()
    {
        runtimePauseAction?.Dispose();
        runtimePauseAction = null;
    }

    public void OnResumePressed()
    {
        PauseService.Resume();
    }

    public void OnQuitPressed()
    {
        RuntimeUiUtility.QuitApplication();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            TryTogglePause();
        }
    }

    public static void RequestPause()
    {
        if (!IsCutscenePlaying() && !PauseService.IsPaused(PauseType.Physics))
        {
            PauseService.Pause();
        }
    }

    public static bool IsCutscenePlaying()
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

    private InputAction GetOrCreatePauseAction()
    {
        if (pauseAction.IsAssigned())
        {
            return pauseAction.action;
        }

        runtimePauseAction ??= new InputAction(
            name: "Pause",
            type: InputActionType.Button);
        if (runtimePauseAction.bindings.Count == 0)
        {
            runtimePauseAction.AddBinding("<Keyboard>/escape");
            runtimePauseAction.AddBinding("<Keyboard>/p");
            runtimePauseAction.AddBinding("<Gamepad>/start");
            runtimePauseAction.AddBinding("<Gamepad>/select");
        }

        return runtimePauseAction;
    }

    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        TryTogglePause();
    }

    private void TryTogglePause()
    {
        if (lastToggleFrame == Time.frameCount || IsCutscenePlaying()) return;
        lastToggleFrame = Time.frameCount;
        PauseService.Toggle();
    }
}
