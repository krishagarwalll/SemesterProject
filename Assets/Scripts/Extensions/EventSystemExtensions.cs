using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class EventSystemExtensions
{
    public static bool IsPointerOverCurrentPointer(this EventSystem eventSystem)
    {
        if (eventSystem == null)
        {
            return false;
        }

        return Pointer.current != null
            ? eventSystem.IsPointerOverGameObject(Pointer.current.deviceId)
            : eventSystem.IsPointerOverGameObject();
    }
}
