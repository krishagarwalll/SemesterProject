using UnityEngine.InputSystem;

public static class InputActionReferenceExtensions
{
    public static bool IsAssigned(this InputActionReference actionReference)
    {
        return actionReference != null && actionReference.action != null;
    }

    public static void SetEnabled(this InputActionReference actionReference, bool enabled)
    {
        if (!actionReference.IsAssigned())
        {
            return;
        }

        if (enabled)
        {
            actionReference.action.Enable();
        }
        else
        {
            actionReference.action.Disable();
        }
    }

    public static bool WasPressedThisFrame(this InputActionReference actionReference)
    {
        return actionReference.IsAssigned() && actionReference.action.WasPressedThisFrame();
    }

    public static bool WasReleasedThisFrame(this InputActionReference actionReference)
    {
        return actionReference.IsAssigned() && actionReference.action.WasReleasedThisFrame();
    }

    public static bool IsPressed(this InputActionReference actionReference)
    {
        return actionReference.IsAssigned() && actionReference.action.IsPressed();
    }

    public static T ReadValueOrDefault<T>(this InputActionReference actionReference) where T : struct
    {
        return actionReference.IsAssigned() ? actionReference.action.ReadValue<T>() : default;
    }
}
