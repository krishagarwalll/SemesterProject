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
    [SerializeField] private bool logConfigurationWarnings = true;

    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private PointerCursorEntry[] cursorEntries = Array.Empty<PointerCursorEntry>();
    [SerializeField] private bool resetCursorOnDisable = true;

    private bool primaryPressedThisFrame;
    private bool primaryReleasedThisFrame;
    private bool primaryClickedThisFrame;
    private bool secondaryClickedThisFrame;
    private bool dragStartedThisFrame;
    private bool isPrimaryPressed;
    private bool isDragging;
    private bool isPointerOverUi;
    private bool hasWorldPoint;
    private bool hasWalkPoint;
    private bool isWorldBlocked;
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

    public bool PrimaryPressedThisFrame => primaryPressedThisFrame;
    public bool PrimaryReleasedThisFrame => primaryReleasedThisFrame;
    public bool PrimaryClickedThisFrame => primaryClickedThisFrame;
    public bool SecondaryClickedThisFrame => secondaryClickedThisFrame;
    public bool DragStartedThisFrame => dragStartedThisFrame;
    public bool IsPrimaryPressed => isPrimaryPressed;
    public bool IsPointerOverUi => isPointerOverUi;
    public bool HasWalkPoint => hasWalkPoint;
    public Vector2 ScreenPosition => screenPosition;
    public InteractionTarget HoveredTarget => hoveredTarget;
    public InteractionTarget ClickedTarget => clickedTarget;
    public InteractionTarget SecondaryClickedTarget => secondaryClickedTarget;
    public InteractionTarget DragTarget => dragTarget;
    public PointerCursorKind CursorKind => ResolveCursorKind();
    public Camera WorldCamera => worldCamera ? worldCamera : worldCamera = Camera.main;

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
        ValidateConfiguration();
        ApplyCursor(force: true);
    }

    private void OnDisable()
    {
        secondaryPressAction.SetEnabled(false);
        primaryPressAction.SetEnabled(false);
        pointerPositionAction.SetEnabled(false);
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
        screenPosition = pointerPositionAction.ReadValueOrDefault<Vector2>();
        primaryPressedThisFrame = primaryPressAction.WasPressedThisFrame();
        primaryReleasedThisFrame = primaryPressAction.WasReleasedThisFrame();
        primaryClickedThisFrame = false;
        secondaryClickedThisFrame = false;
        dragStartedThisFrame = false;
        clickedTarget = null;
        secondaryClickedTarget = null;

        ResolveWorldState();
        UpdatePrimaryState();
        UpdateSecondaryState();
        ApplyCursor(force: false);
    }

    public bool TryGetWalkPoint(out Vector3 point)
    {
        point = walkPoint;
        return hasWalkPoint;
    }

    public bool TryGetDragPoint(float baseHeight, float maxLiftHeight, out Vector3 point)
    {
        if (hasWorldPoint)
        {
            point = worldPoint;
            point.y = Mathf.Clamp(point.y, baseHeight, baseHeight + maxLiftHeight);
            return true;
        }

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
        if (primaryPressedThisFrame)
        {
            pressScreenPosition = screenPosition;
            pressedTarget = hoveredTarget;
            dragTarget = null;
        }

        isPrimaryPressed = primaryPressAction.IsPressed();
        if (isPrimaryPressed && !isDragging && (screenPosition - pressScreenPosition).sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
        {
            isDragging = true;
            dragStartedThisFrame = true;
            dragTarget = pressedTarget != null && pressedTarget.SupportsDrag ? pressedTarget : null;
        }

        if (!primaryReleasedThisFrame)
        {
            return;
        }

        primaryClickedThisFrame = !isDragging && (pressedTarget == null || pressedTarget.SupportsClick);
        if (primaryClickedThisFrame)
        {
            clickedTarget = pressedTarget;
        }

        isDragging = false;
        isPrimaryPressed = false;
        pressedTarget = null;
        dragTarget = null;
    }

    private void UpdateSecondaryState()
    {
        if (secondaryPressAction.WasPressedThisFrame())
        {
            secondaryClickedThisFrame = true;
            secondaryClickedTarget = hoveredTarget;
        }
    }

    private void ResolveWorldState()
    {
        hoveredTarget = null;
        hasWorldPoint = false;
        hasWalkPoint = false;
        isWorldBlocked = false;
        isPointerOverUi = EventSystem.current.IsPointerOverCurrentPointer();

        if (!WorldCamera || ignoreWorldWhenOverUi && isPointerOverUi)
        {
            return;
        }

        int mask = interactionLayers.value | walkableLayers.value | blockingLayers.value;
        if (mask == 0)
        {
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(PointerRay, rayDistance, mask, triggerInteraction);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

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
                return;
            }

            InteractionTarget target = hit.collider.ResolveInteractionTarget();
            if (target)
            {
                hoveredTarget = target;
                return;
            }

            if ((walkableLayers.value & layer) != 0)
            {
                walkPoint = hit.point;
                hasWalkPoint = true;
                return;
            }
        }
    }

    private PointerCursorKind ResolveCursorKind()
    {
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
        Cursor.SetCursor(entry.Texture, entry.Hotspot, cursorMode);
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

    private void ValidateConfiguration()
    {
        if (!logConfigurationWarnings)
        {
            return;
        }

        if (!pointerPositionAction.IsAssigned())
        {
            Debug.LogWarning("PointerContext is missing a pointer position InputActionReference.", this);
        }

        if (!primaryPressAction.IsAssigned())
        {
            Debug.LogWarning("PointerContext is missing a primary press InputActionReference.", this);
        }

        if (!secondaryPressAction.IsAssigned())
        {
            Debug.LogWarning("PointerContext is missing a secondary press InputActionReference.", this);
        }

        if (walkableLayers.value == 0)
        {
            Debug.LogWarning("PointerContext has no walkable layers configured.", this);
        }

        if (!WorldCamera)
        {
            Debug.LogWarning("PointerContext could not find a world camera.", this);
        }
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
