using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PointerContext : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;

    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private InputActionReference primaryPressAction;
    [SerializeField] private InputActionReference secondaryPressAction;

    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private LayerMask walkableLayers = ~0;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField, Min(0f)] private float rayDistance = 500f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool ignoreWorldWhenOverUi = true;
    [SerializeField, Min(0f)] private float dragThresholdPixels = 8f;

    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private PointerCursorEntry[] cursorEntries = Array.Empty<PointerCursorEntry>();
    [SerializeField] private bool resetCursorOnDisable = true;
    [SerializeField] private bool fallbackToSoftwareCursor = true;

    private bool primaryPressedThisFrame;
    private bool primaryReleasedThisFrame;
    private bool primaryClickedThisFrame;
    private bool secondaryClickedThisFrame;
    private bool dragStartedThisFrame;
    private bool dragEndedThisFrame;
    private bool isPrimaryPressed;
    private bool isDragging;
    private bool isPointerOverUi;
    private bool hasWorldPoint;
    private bool hasWalkPoint;
    private bool isWorldBlocked;
    private bool contextMenuOpen;
    private Vector2 screenPosition;
    private Vector2 pressScreenPosition;
    private Vector3 worldPoint;
    private Vector3 walkPoint;
    private PointerCursorKind appliedCursorKind = (PointerCursorKind)(-1);
    private InteractionTarget hoveredTarget;
    private InteractionTarget pressedTarget;
    private InteractionTarget dragTarget;
    private InteractionTarget clickedTarget;
    private InteractionTarget secondaryClickedTarget;

    public event Action<InteractionTarget, InteractionTarget> HoverChanged;
    public event Action<PointerContext> PrimaryPressed;
    public event Action<PointerContext> PrimaryClicked;
    public event Action<PointerContext> SecondaryPressed;
    public event Action<PointerContext> DragStarted;
    public event Action<PointerContext> DragUpdated;
    public event Action<PointerContext> DragEnded;

    public bool PrimaryPressedThisFrame => primaryPressedThisFrame;
    public bool PrimaryReleasedThisFrame => primaryReleasedThisFrame;
    public bool PrimaryClickedThisFrame => primaryClickedThisFrame;
    public bool SecondaryClickedThisFrame => secondaryClickedThisFrame;
    public bool DragStartedThisFrame => dragStartedThisFrame;
    public bool DragEndedThisFrame => dragEndedThisFrame;
    public bool IsPrimaryPressed => isPrimaryPressed;
    public bool IsPointerOverUi => isPointerOverUi;
    public bool HasWalkPoint => hasWalkPoint;
    public bool HasWorldPoint => hasWorldPoint;
    public bool IsWorldBlocked => isWorldBlocked;
    public Vector2 ScreenPosition => screenPosition;
    public Vector3 WorldPoint => worldPoint;
    public InteractionTarget HoveredTarget => hoveredTarget;
    public InteractionTarget ClickedTarget => clickedTarget;
    public InteractionTarget SecondaryClickedTarget => secondaryClickedTarget;
    public InteractionTarget DragTarget => dragTarget;
    public Camera WorldCamera => worldCamera ? worldCamera : worldCamera = Camera.main;
    public PointerState State => ResolveState();

    private Ray PointerRay => WorldCamera ? WorldCamera.ScreenPointToRay(screenPosition) : default;

    private void Reset()
    {
        worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        pointerPositionAction.SetEnabled(true);
        primaryPressAction.SetEnabled(true);
        secondaryPressAction.SetEnabled(true);
        ApplyCursor(force: true);
    }

    private void OnDisable()
    {
        secondaryPressAction.SetEnabled(false);
        primaryPressAction.SetEnabled(false);
        pointerPositionAction.SetEnabled(false);
        SetHoveredTarget(null);
        if (!resetCursorOnDisable)
        {
            return;
        }

        Cursor.SetCursor(null, Vector2.zero, cursorMode);
        appliedCursorKind = (PointerCursorKind)(-1);
    }

    private void OnValidate()
    {
        rayDistance = Mathf.Max(0f, rayDistance);
        dragThresholdPixels = Mathf.Max(0f, dragThresholdPixels);
        if (!worldCamera)
        {
            worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        primaryPressedThisFrame = false;
        primaryReleasedThisFrame = false;
        primaryClickedThisFrame = false;
        secondaryClickedThisFrame = false;
        dragStartedThisFrame = false;
        dragEndedThisFrame = false;
        clickedTarget = null;
        secondaryClickedTarget = null;

        screenPosition = pointerPositionAction.ReadValueOrDefault<Vector2>();
        ResolveWorldState();
        UpdatePrimaryState();
        UpdateSecondaryState();

        if (isDragging)
        {
            DragUpdated?.Invoke(this);
        }

        ApplyCursor(force: false);
    }

    public void SetContextMenuOpen(bool isOpen)
    {
        contextMenuOpen = isOpen;
        ApplyCursor(force: true);
    }

    public bool TryGetWalkPoint(out Vector3 point)
    {
        point = walkPoint;
        return hasWalkPoint;
    }

    public bool TryGetWorldPoint(out Vector3 point)
    {
        point = worldPoint;
        return hasWorldPoint;
    }

    public bool TryGetDragPoint(float baseHeight, float maxLiftHeight, out Vector3 point)
    {
        point = default;
        if (!WorldCamera)
        {
            return false;
        }

        Plane plane = new(Vector3.up, new Vector3(0f, baseHeight, 0f));
        if (!plane.Raycast(PointerRay, out float distance) || distance < 0f)
        {
            return false;
        }

        point = PointerRay.GetPoint(distance);
        point.y = Mathf.Clamp(point.y, baseHeight, baseHeight + maxLiftHeight);
        return true;
    }

    private void UpdatePrimaryState()
    {
        if (primaryPressAction.WasPressedThisFrame())
        {
            primaryPressedThisFrame = true;
            pressScreenPosition = screenPosition;
            pressedTarget = hoveredTarget;
            dragTarget = null;
            PrimaryPressed?.Invoke(this);
        }

        isPrimaryPressed = primaryPressAction.IsPressed();
        if (isPrimaryPressed && !isDragging && (screenPosition - pressScreenPosition).sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
        {
            isDragging = true;
            dragStartedThisFrame = true;
            dragTarget = pressedTarget != null && pressedTarget.SupportsDrag ? pressedTarget : null;
            DragStarted?.Invoke(this);
        }

        if (!primaryPressAction.WasReleasedThisFrame())
        {
            return;
        }

        primaryReleasedThisFrame = true;
        if (isDragging)
        {
            dragEndedThisFrame = true;
            DragEnded?.Invoke(this);
        }
        else
        {
            primaryClickedThisFrame = true;
            clickedTarget = pressedTarget;
            PrimaryClicked?.Invoke(this);
        }

        isDragging = false;
        isPrimaryPressed = false;
        pressedTarget = null;
        dragTarget = null;
    }

    private void UpdateSecondaryState()
    {
        if (!secondaryPressAction.WasPressedThisFrame())
        {
            return;
        }

        secondaryClickedThisFrame = true;
        secondaryClickedTarget = hoveredTarget;
        SecondaryPressed?.Invoke(this);
    }

    private void ResolveWorldState()
    {
        hasWorldPoint = false;
        hasWalkPoint = false;
        isWorldBlocked = false;
        isPointerOverUi = EventSystem.current.IsPointerOverCurrentPointer();

        if (!WorldCamera || ignoreWorldWhenOverUi && isPointerOverUi)
        {
            SetHoveredTarget(null);
            return;
        }

        int mask = interactionLayers.value | walkableLayers.value | blockingLayers.value;
        if (mask == 0)
        {
            SetHoveredTarget(null);
            return;
        }

        InteractionTarget resolvedTarget = null;
        RaycastHit[] hits = Physics.RaycastAll(PointerRay, rayDistance, mask, triggerInteraction);
        Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!hit.collider.IsUsable())
            {
                continue;
            }

            hasWorldPoint = true;
            worldPoint = hit.point;
            int layer = 1 << hit.collider.gameObject.layer;
            if ((blockingLayers.value & layer) != 0)
            {
                isWorldBlocked = true;
                break;
            }

            resolvedTarget = hit.collider.ResolveInteractionTarget();
            if (resolvedTarget)
            {
                break;
            }

            if ((walkableLayers.value & layer) != 0)
            {
                walkPoint = hit.point;
                hasWalkPoint = true;
                break;
            }
        }

        SetHoveredTarget(resolvedTarget);
    }

    private void SetHoveredTarget(InteractionTarget target)
    {
        if (hoveredTarget == target)
        {
            return;
        }

        InteractionTarget previous = hoveredTarget;
        previous?.SetHovered(false);
        hoveredTarget = target;
        hoveredTarget?.SetHovered(true);
        HoverChanged?.Invoke(previous, hoveredTarget);
    }

    private PointerState ResolveState()
    {
        if (contextMenuOpen)
        {
            return PointerState.ContextMenu;
        }

        if (isDragging)
        {
            return dragTarget ? PointerState.DraggingWorldProp : PointerState.DraggingInventoryItem;
        }

        if (isPointerOverUi)
        {
            return PointerState.HoveringUi;
        }

        if (hoveredTarget)
        {
            return PointerState.HoveringWorld;
        }

        return PointerState.None;
    }

    private PointerCursorKind ResolveCursorKind()
    {
        if (contextMenuOpen)
        {
            return PointerCursorKind.Default;
        }

        if (isDragging)
        {
            return dragTarget ? dragTarget.DragCursorKind : PointerCursorKind.Dragging;
        }

        if (isPrimaryPressed)
        {
            return PointerCursorKind.Pressed;
        }

        if (hoveredTarget)
        {
            return hoveredTarget.HoverCursorKind;
        }

        if (hasWalkPoint)
        {
            return PointerCursorKind.Move;
        }

        return isWorldBlocked ? PointerCursorKind.Blocked : PointerCursorKind.Default;
    }

    private void ApplyCursor(bool force)
    {
        PointerCursorKind kind = ResolveCursorKind();
        if (!force && kind == appliedCursorKind)
        {
            return;
        }

        appliedCursorKind = kind;
        PointerCursorEntry entry = GetCursorEntry(kind);
        Texture2D texture = IsCursorTextureUsable(entry.Texture) ? entry.Texture : null;
        Vector2 hotspot = texture ? entry.Hotspot : Vector2.zero;
        Cursor.SetCursor(texture, hotspot, ResolveCursorMode(texture));
    }

    private CursorMode ResolveCursorMode(Texture2D texture)
    {
        if (!texture || !fallbackToSoftwareCursor)
        {
            return cursorMode;
        }

        if (IsHardwareCursorCompatible(texture))
        {
            return cursorMode;
        }

        return CursorMode.ForceSoftware;
    }

    private static bool IsHardwareCursorCompatible(Texture2D texture)
    {
        if (!texture || !texture.isReadable)
        {
            return false;
        }

        if (texture.mipmapCount > 1)
        {
            return false;
        }

        return texture.format is TextureFormat.RGBA32 or TextureFormat.ARGB32 or TextureFormat.BGRA32;
    }

    private static bool IsCursorTextureUsable(Texture2D texture)
    {
        return texture
            && texture.isReadable
            && texture.mipmapCount <= 1
            && texture.format is TextureFormat.RGBA32 or TextureFormat.ARGB32 or TextureFormat.BGRA32;
    }

    private PointerCursorEntry GetCursorEntry(PointerCursorKind kind)
    {
        for (int i = 0; i < cursorEntries.Length; i++)
        {
            if (cursorEntries[i].CursorKind == kind)
            {
                return cursorEntries[i];
            }
        }

        for (int i = 0; i < cursorEntries.Length; i++)
        {
            if (cursorEntries[i].CursorKind == PointerCursorKind.Default)
            {
                return cursorEntries[i];
            }
        }

        return default;
    }
}

[Serializable]
public struct PointerCursorEntry
{
    [SerializeField] private PointerCursorKind cursorKind;
    [SerializeField] private Texture2D texture;
    [SerializeField] private Vector2 hotspot;

    public PointerCursorKind CursorKind => cursorKind;
    public Texture2D Texture => texture;
    public Vector2 Hotspot => hotspot;
}
