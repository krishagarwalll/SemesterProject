using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PointerCursorPresenter : MonoBehaviour
{
    [FieldHeader("References")]
    [SerializeField] private PointerContext pointer;

    [FieldHeader("Cursor")]
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private PointerCursorVisualEntry[] cursorEntries = Array.Empty<PointerCursorVisualEntry>();

    private Texture2D appliedTexture;
    private Vector2 appliedHotspot;

    private PointerContext Pointer => pointer ? pointer : pointer = FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include);

    private void OnEnable()
    {
        if (Pointer)
        {
            Pointer.CursorChanged += HandleCursorChanged;
            ApplyCursor(Pointer.CurrentCursorKind, force: true);
        }
    }

    private void OnDisable()
    {
        if (pointer)
        {
            pointer.CursorChanged -= HandleCursorChanged;
        }

        Cursor.SetCursor(null, Vector2.zero, cursorMode);
        appliedTexture = null;
        appliedHotspot = Vector2.zero;
    }

    private void HandleCursorChanged(PointerCursorKind previous, PointerCursorKind current)
    {
        ApplyCursor(current);
    }

    private void ApplyCursor(PointerCursorKind kind, bool force = false)
    {
        Texture2D texture = null;
        Vector2 hotspot = Vector2.zero;
        TryGetEntry(kind, out PointerCursorVisualEntry entry);
        texture = entry.Texture;
        hotspot = entry.Hotspot;

        if (!force && texture == appliedTexture && hotspot == appliedHotspot)
        {
            return;
        }

        Cursor.SetCursor(texture, hotspot, cursorMode);
        appliedTexture = texture;
        appliedHotspot = hotspot;
    }

    private bool TryGetEntry(PointerCursorKind kind, out PointerCursorVisualEntry entry)
    {
        if (cursorEntries != null)
        {
            for (int i = 0; i < cursorEntries.Length; i++)
            {
                if (cursorEntries[i].CursorKind == kind)
                {
                    entry = cursorEntries[i];
                    return true;
                }
            }

            for (int i = 0; i < cursorEntries.Length; i++)
            {
                if (cursorEntries[i].CursorKind == PointerCursorKind.Default)
                {
                    entry = cursorEntries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }
}

[Serializable]
public struct PointerCursorVisualEntry
{
    [SerializeField] private PointerCursorKind cursorKind;
    [SerializeField] private Texture2D texture;
    [SerializeField] private Vector2 hotspot;

    public PointerCursorKind CursorKind => cursorKind;
    public Texture2D Texture => texture;
    public Vector2 Hotspot => hotspot;
}
