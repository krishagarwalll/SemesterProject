using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class CutsceneInputBindings
{
    [SerializeField] private InputActionReference skipAction;
    [SerializeField] private InputActionReference fastForwardAction;
    [SerializeField] private bool anyKeyboardKeySkips;
    [SerializeField] private Key fallbackSkipKey = Key.Escape;
    [SerializeField] private Key fallbackFastForwardKey = Key.Space;

    public void ApplyLegacyFields(
        InputActionReference legacySkipAction,
        InputActionReference legacyFastForwardAction,
        bool legacyAnyKeyboardKeySkips,
        Key legacyFallbackSkipKey,
        Key legacyFallbackFastForwardKey)
    {
        if (legacySkipAction) skipAction = legacySkipAction;
        if (legacyFastForwardAction) fastForwardAction = legacyFastForwardAction;
        if (legacyAnyKeyboardKeySkips) anyKeyboardKeySkips = true;
        if (legacyFallbackSkipKey != Key.None) fallbackSkipKey = legacyFallbackSkipKey;
        if (legacyFallbackFastForwardKey != Key.None) fallbackFastForwardKey = legacyFallbackFastForwardKey;
    }

    public void Enable()
    {
        EnableAction(skipAction);
        EnableAction(fastForwardAction);
    }

    public bool WasSkipPressed()
    {
        if (skipAction && skipAction.action != null && skipAction.action.WasPressedThisFrame())
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (anyKeyboardKeySkips && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        return fallbackSkipKey != Key.None && keyboard[fallbackSkipKey].wasPressedThisFrame;
    }

    public bool IsFastForwardHeld()
    {
        if (fastForwardAction && fastForwardAction.action != null)
        {
            return fastForwardAction.action.IsPressed();
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null
            && fallbackFastForwardKey != Key.None
            && keyboard[fallbackFastForwardKey].isPressed;
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference && actionReference.action != null && !actionReference.action.enabled)
        {
            actionReference.action.Enable();
        }
    }
}
