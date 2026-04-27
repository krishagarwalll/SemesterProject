using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PointerContext : MonoBehaviour
{
    [FieldHeader("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PoptropicaController actor;
    [SerializeField] private Inventory inventory;
    [SerializeField] private InventoryHotbar hotbar;
    [SerializeField] private RoomTransitionService roomTransitionService;

    [FieldHeader("Input")]
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private InputActionReference primaryPressAction;
    [SerializeField] private InputActionReference secondaryPressAction;

    [FieldHeader("World")]
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private LayerMask walkableLayers = ~0;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private bool ignoreWorldWhenOverUi = true;
    [SerializeField, Min(0f)] private float interactionProbeRadius = 0.85f;
    [SerializeField, Min(0f)] private float dragThresholdPixels = 8f;

    private bool primaryPressedThisFrame;
    private bool primaryReleasedThisFrame;
    private bool primaryClickedThisFrame;
    private bool secondaryClickedThisFrame;
    private bool dragStartedThisFrame;
    private bool dragEndedThisFrame;
    private bool isPrimaryPressed;
    private bool isDragging;
    private bool isPointerOverUi;
    private bool isPointerOverClickableUi;
    private bool hasWorldPoint;
    private bool isWorldBlocked;
    private bool contextMenuOpen;
    private Vector2 screenPosition;
    private Vector2 pressScreenPosition;
    private Vector3 worldPoint;
    private PointerCursorKind currentCursorKind;
    private readonly List<InteractionCandidate> hoverCandidates = new();
    private readonly List<InteractionCandidate> dragCandidates = new();
    private readonly List<RaycastResult> uiRaycastResults = new();
    private InteractionTarget hoveredTarget;
    private InteractionTarget pressedTarget;
    private InteractionTarget pressedWorldDragTarget;
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
    public event Action<PointerCursorKind, PointerCursorKind> CursorChanged;

    public bool PrimaryPressedThisFrame => primaryPressedThisFrame;
    public bool PrimaryReleasedThisFrame => primaryReleasedThisFrame;
    public bool PrimaryClickedThisFrame => primaryClickedThisFrame;
    public bool SecondaryClickedThisFrame => secondaryClickedThisFrame;
    public bool DragStartedThisFrame => dragStartedThisFrame;
    public bool DragEndedThisFrame => dragEndedThisFrame;
    public bool IsPrimaryPressed => isPrimaryPressed;
    public bool IsPointerOverUi => isPointerOverUi;
    public bool IsPointerOverClickableUi => isPointerOverClickableUi;
    public bool HasWorldPoint => hasWorldPoint;
    public bool IsWorldBlocked => isWorldBlocked;
    public bool IsDragging => isDragging;
    public Vector2 ScreenPosition => screenPosition;
    public Vector3 WorldPoint => worldPoint;
    public InteractionTarget HoveredTarget => hoveredTarget;
    public InteractionTarget ClickedTarget => clickedTarget;
    public InteractionTarget SecondaryClickedTarget => secondaryClickedTarget;
    public InteractionTarget DragTarget => dragTarget;
    public Camera WorldCamera => worldCamera ? worldCamera : worldCamera = Camera.main;
    public PoptropicaController Actor => actor ? actor : actor = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
    public Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    public InventoryHotbar Hotbar => hotbar ? hotbar : hotbar = FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
    public RoomTransitionService Rooms => roomTransitionService ? roomTransitionService : roomTransitionService = FindFirstObjectByType<RoomTransitionService>(FindObjectsInactive.Include);
    public PointerState State => ResolveState();
    public PointerCursorKind CurrentCursorKind => currentCursorKind;

    private void Reset()
    {
        worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        pointerPositionAction.SetEnabled(true);
        primaryPressAction.SetEnabled(true);
        secondaryPressAction.SetEnabled(true);
        currentCursorKind = ResolveCursorKind();
    }

    private void OnDisable()
    {
        secondaryPressAction.SetEnabled(false);
        primaryPressAction.SetEnabled(false);
        pointerPositionAction.SetEnabled(false);
        SetHoveredTarget(null);
    }

    private void OnValidate()
    {
        interactionProbeRadius = Mathf.Max(0f, interactionProbeRadius);
        dragThresholdPixels = Mathf.Max(0f, dragThresholdPixels);
        if (!worldCamera)
        {
            worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        ResetFrameState();
        screenPosition = ReadScreenPosition();

        if (PauseService.IsGameplayInputPaused(this))
        {
            CancelPointerStateForPause();
            UpdateCursorKind();
            return;
        }

        ResolveWorldState();
        UpdatePrimaryState();
        UpdateSecondaryState();

        if (isDragging)
        {
            DragUpdated?.Invoke(this);
        }

        UpdateCursorKind();
    }

    private void CancelPointerStateForPause()
    {
        hoverCandidates.Clear();
        dragCandidates.Clear();
        SetHoveredTarget(null);
        clickedTarget = null;
        secondaryClickedTarget = null;
        pressedTarget = null;
        pressedWorldDragTarget = null;

        if (isDragging)
        {
            dragEndedThisFrame = true;
            DragEnded?.Invoke(this);
        }

        isPrimaryPressed = false;
        isDragging = false;
        dragTarget = null;
        hasWorldPoint = false;
        isWorldBlocked = false;
        isPointerOverUi = false;
        isPointerOverClickableUi = false;
    }

    private void ResetFrameState()
    {
        primaryPressedThisFrame = false;
        primaryReleasedThisFrame = false;
        primaryClickedThisFrame = false;
        secondaryClickedThisFrame = false;
        dragStartedThisFrame = false;
        dragEndedThisFrame = false;
        clickedTarget = null;
        secondaryClickedTarget = null;
    }

    public void SetContextMenuOpen(bool isOpen)
    {
        contextMenuOpen = isOpen;
        UpdateCursorKind();
    }

    public bool TryGetWorldPoint(out Vector3 point)
    {
        point = worldPoint;
        return hasWorldPoint;
    }

    public bool TryGetWorldDragTarget(out InteractionTarget target)
    {
        return TryGetWorldDragTarget(screenPosition, out target);
    }

    public bool TryGetWorldDragTarget(Vector2 pointerScreenPosition, out InteractionTarget target)
    {
        if (!isDragging && IsBlockingUi(pointerScreenPosition))
        {
            target = null;
            return false;
        }

        if (!TryGetWorldPointAtDepth(pointerScreenPosition, 0f, out Vector3 dragWorldPoint))
        {
            target = null;
            return false;
        }

        return TryResolveBestTarget((Vector2)dragWorldPoint, dragOnly: true, dragCandidates, out target, out _);
    }

    public bool TryGetWorldPointAtDepth(float depth, out Vector3 point)
    {
        return TryGetWorldPointAtDepth(screenPosition, depth, out point);
    }

    public bool TryGetDragPoint(float baseHeight, float maxLiftHeight, out Vector3 point)
    {
        if (!TryGetPointOnPlane(Vector3.up, new Vector3(0f, baseHeight, 0f), out point))
        {
            return false;
        }

        point.y = Mathf.Clamp(point.y, baseHeight, baseHeight + maxLiftHeight);
        return true;
    }

    public bool TryGetWorldPointAtDepth(Vector2 pointerScreenPosition, float depth, out Vector3 point)
    {
        point = default;
        if (!WorldCamera)
        {
            return false;
        }

        if (!WorldCamera.orthographic)
        {
            return TryGetPointOnPlane(pointerScreenPosition, Vector3.forward, new Vector3(0f, 0f, depth), out point);
        }

        float distance = Mathf.Abs(depth - WorldCamera.transform.position.z);
        Vector3 screenPoint = new(pointerScreenPosition.x, pointerScreenPosition.y, distance);
        point = WorldCamera.ScreenToWorldPoint(screenPoint);
        point.z = depth;
        return IsFinite(point);
    }

    public bool TryGetPointOnPlane(Vector3 planeNormal, Vector3 planePoint, out Vector3 point)
    {
        return TryGetPointOnPlane(screenPosition, planeNormal, planePoint, out point);
    }

    public bool TryGetPointOnPlane(Vector2 pointerScreenPosition, Vector3 planeNormal, Vector3 planePoint, out Vector3 point)
    {
        point = default;
        if (!WorldCamera)
        {
            return false;
        }

        Plane plane = new(planeNormal, planePoint);
        Ray ray = WorldCamera.ScreenPointToRay(pointerScreenPosition);
        if (!plane.Raycast(ray, out float distance) || distance < 0f || !float.IsFinite(distance))
        {
            return false;
        }

        point = ray.GetPoint(distance);
        return IsFinite(point);
    }

    public int GetHoveredTargets(List<InteractionTarget> results)
    {
        if (results == null)
        {
            return 0;
        }

        results.Clear();
        for (int i = 0; i < hoverCandidates.Count; i++)
        {
            InteractionTarget target = hoverCandidates[i].Target;
            if (target)
            {
                results.Add(target);
            }
        }

        return results.Count;
    }

    private void UpdatePrimaryState()
    {
        bool primaryPressed = WasPrimaryPressedThisFrame();
        bool primaryReleased = WasPrimaryReleasedThisFrame();

        if (primaryPressed)
        {
            primaryPressedThisFrame = true;
            pressScreenPosition = screenPosition;
            pressedTarget = hoveredTarget;
            TryGetWorldDragTarget(pressScreenPosition, out pressedWorldDragTarget);
            if (!pressedTarget && hasWorldPoint)
            {
                TryGetBestInteraction((Vector2)worldPoint, out pressedTarget, out _);
            }

            if (!pressedTarget)
            {
                TryGetWorldDragTarget(pressScreenPosition, out pressedTarget);
            }

            dragTarget = null;
            PrimaryPressed?.Invoke(this);
        }

        isPrimaryPressed = IsPrimaryCurrentlyPressed();
        if (isPrimaryPressed
            && !isDragging
            && (screenPosition - pressScreenPosition).sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
        {
            isDragging = true;
            dragStartedThisFrame = true;
            dragTarget = pressedTarget && pressedTarget.SupportsDrag
                ? pressedTarget
                : pressedWorldDragTarget
                    ? pressedWorldDragTarget
                    : TryGetWorldDragTarget(pressScreenPosition, out InteractionTarget resolvedDragTarget)
                        ? resolvedDragTarget
                        : null;
            if (!dragTarget)
            {
                TryGetWorldDragTarget(screenPosition, out dragTarget);
            }
            DragStarted?.Invoke(this);
        }

        if (!primaryReleased)
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
        pressedWorldDragTarget = null;
        dragTarget = null;
    }

    private void UpdateSecondaryState()
    {
        if (!WasSecondaryPressedThisFrame())
        {
            return;
        }

        secondaryClickedThisFrame = true;
        secondaryClickedTarget = hoveredTarget;
        SecondaryPressed?.Invoke(this);
    }

    private Vector2 ReadScreenPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return pointerPositionAction.ReadValueOrDefault<Vector2>();
    }

    private bool WasPrimaryPressedThisFrame()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        return primaryPressAction.WasPressedThisFrame();
    }

    private bool WasPrimaryReleasedThisFrame()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasReleasedThisFrame;
        }

        return primaryPressAction.WasReleasedThisFrame();
    }

    private bool IsPrimaryCurrentlyPressed()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.isPressed;
        }

        return primaryPressAction.IsPressed();
    }

    private bool WasSecondaryPressedThisFrame()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.rightButton.wasPressedThisFrame;
        }

        return secondaryPressAction.WasPressedThisFrame();
    }

    private void ResolveWorldState()
    {
        hasWorldPoint = TryGetWorldPointAtDepth(0f, out worldPoint);
        isWorldBlocked = false;
        isPointerOverUi = ProbeBlockingUi(screenPosition, out isPointerOverClickableUi);

        if (!hasWorldPoint)
        {
            hoverCandidates.Clear();
            SetHoveredTarget(null);
            return;
        }

        if (!WorldCamera || ignoreWorldWhenOverUi && isPointerOverUi && !isDragging)
        {
            hoverCandidates.Clear();
            SetHoveredTarget(null);
            return;
        }

        Vector2 point2D = worldPoint;
        bool hasInteraction = TryResolveBestTarget(point2D, dragOnly: false, hoverCandidates, out InteractionTarget target, out _);
        bool hasWalkable = TryGetBestOverlap(point2D, walkableLayers, out _, out _);
        bool hasBlocking = TryGetBestOverlap(point2D, blockingLayers, out _, out _);

        isWorldBlocked = hasBlocking && !hasWalkable && !hasInteraction;
        SetHoveredTarget(hasInteraction ? target : null);
    }

    private bool TryGetBestInteraction(Vector2 point, out InteractionTarget target, out int sortScore)
    {
        return TryResolveBestTarget(point, dragOnly: false, hoverCandidates, out target, out sortScore);
    }

    private bool TryResolveBestTarget(Vector2 point, bool dragOnly, List<InteractionCandidate> candidates, out InteractionTarget target, out int sortScore)
    {
        target = null;
        sortScore = int.MinValue;
        candidates.Clear();
        if (interactionLayers.value == 0)
        {
            return false;
        }

        IReadOnlyList<InteractionTarget> targets = InteractionTarget.ActiveTargets;
        for (int i = 0; i < targets.Count; i++)
        {
            AddInteractionCandidate(candidates, targets[i], point, dragOnly);
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        candidates.Sort(CompareCandidatesDescending);
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].Target)
            {
                continue;
            }

            target = candidates[i].Target;
            sortScore = candidates[i].SortingScore;
            return true;
        }

        return false;
    }

    private void AddInteractionCandidate(List<InteractionCandidate> candidates, InteractionTarget resolvedTarget, Vector2 point, bool dragOnly)
    {
        if (!resolvedTarget || !resolvedTarget.isActiveAndEnabled || !IsTargetOnInteractionLayer(resolvedTarget))
        {
            return;
        }

        if (!IsTargetReachableInActorRoom(resolvedTarget))
        {
            return;
        }

        if (dragOnly && !resolvedTarget.SupportsDrag)
        {
            return;
        }

        InteractionContext context = CreateInteractionContext(resolvedTarget);
        float distanceSqr = GetTargetDistanceSqr(point, resolvedTarget);
        float allowedRadius = GetPointerPickRadius(resolvedTarget, dragOnly);
        bool isDirectHit = resolvedTarget.ContainsPoint(point);
        if (!isDirectHit && distanceSqr > allowedRadius * allowedRadius)
        {
            return;
        }

        int existingIndex = FindCandidateIndex(candidates, resolvedTarget);
        float actorDistanceSqr = GetActorDistanceSqr(resolvedTarget);
        bool inInteractionRange = resolvedTarget.IsInRange(Actor ? Actor.transform.position : resolvedTarget.transform.position);
        bool hasPrimaryAction = dragOnly
            ? resolvedTarget.TryGetDraggable(out IWorldDraggable draggable) && draggable.SupportsDrag && draggable.CanStartDrag(this)
            : resolvedTarget.TryGetPrimaryAction(context, out InteractionAction primaryAction) && primaryAction.Enabled;
        bool hasAnyAction = dragOnly ? hasPrimaryAction : resolvedTarget.HasAnyEnabledAction(context);
        InteractionCandidate candidate = new(
            resolvedTarget,
            isDirectHit,
            inInteractionRange,
            resolvedTarget.SelectionPriority,
            hasPrimaryAction,
            hasAnyAction,
            GetSortingScore(resolvedTarget),
            distanceSqr,
            GetNormalizedTargetScore(distanceSqr, actorDistanceSqr, allowedRadius, resolvedTarget.InteractionRadius, inInteractionRange));

        if (existingIndex >= 0)
        {
            if (CompareCandidates(candidate, candidates[existingIndex]) > 0)
            {
                candidates[existingIndex] = candidate;
            }

            return;
        }

        candidates.Add(candidate);
    }

    private bool IsTargetReachableInActorRoom(InteractionTarget target)
    {
        Room activeRoom = GetActiveRoom();
        if (!target || !target.OwnerRoom || !activeRoom)
        {
            return true;
        }

        return target.OwnerRoom == activeRoom;
    }

    private Room GetActiveRoom()
    {
        if (Rooms && Rooms.ActiveRoom)
        {
            return Rooms.ActiveRoom;
        }

        if (!Actor)
        {
            return null;
        }

        IReadOnlyList<Room> rooms = Room.ActiveRooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i] && rooms[i].ContainsPoint(Actor.transform.position))
            {
                return rooms[i];
            }
        }

        return null;
    }

    private bool IsTargetOnInteractionLayer(InteractionTarget target)
    {
        return target && ((1 << target.gameObject.layer) & interactionLayers.value) != 0;
    }


    private static bool TryGetBestOverlap(Vector2 point, LayerMask layerMask, out Collider2D collider, out int sortScore)
    {
        collider = null;
        sortScore = int.MinValue;
        if (layerMask.value == 0)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapPointAll(point, layerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].IsUsable())
            {
                continue;
            }

            int candidateScore = GetSortingScore(hits[i]);
            if (collider && candidateScore < sortScore)
            {
                continue;
            }

            collider = hits[i];
            sortScore = candidateScore;
        }

        return collider;
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
            return dragTarget ? PointerState.DraggingWorldObject : PointerState.DraggingInventoryItem;
        }

        if (isPointerOverUi)
        {
            return isPointerOverClickableUi ? PointerState.HoveringClickableUi : PointerState.HoveringUi;
        }

        return hoveredTarget ? PointerState.HoveringWorld : PointerState.None;
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

        if (hoveredTarget)
        {
            return ResolveHoveredCursorKind(hoveredTarget);
        }

        if (isPointerOverUi)
        {
            return isPointerOverClickableUi ? PointerCursorKind.Interact : PointerCursorKind.Default;
        }

        if (TryGetWorldDragTarget(out InteractionTarget dragTargetCandidate))
        {
            return ResolveHoveredCursorKind(dragTargetCandidate);
        }

        return isWorldBlocked ? PointerCursorKind.Blocked : PointerCursorKind.Default;
    }

    private void UpdateCursorKind()
    {
        PointerCursorKind next = ResolveCursorKind();
        if (next == currentCursorKind)
        {
            return;
        }

        PointerCursorKind previous = currentCursorKind;
        currentCursorKind = next;
        CursorChanged?.Invoke(previous, currentCursorKind);
    }

    private PointerCursorKind ResolveHoveredCursorKind(InteractionTarget target)
    {
        if (!target)
        {
            return PointerCursorKind.Default;
        }

        if (target.SupportsDrag)
        {
            return PointerCursorKind.DragReady;
        }

        InteractionContext context = CreateInteractionContext(target);
        if (target.TryGetAction(context, InteractionMode.Inspect, out InteractionAction inspectAction) && inspectAction.Enabled)
        {
            return PointerCursorKind.Inspect;
        }

        return target.HoverCursorKind;
    }

    private static int GetSortingScore(Component component)
    {
        if (!component)
        {
            return int.MinValue;
        }

        SortingGroup sortingGroup = component.GetComponentInParent<SortingGroup>();
        if (sortingGroup)
        {
            return SortingLayer.GetLayerValueFromID(sortingGroup.sortingLayerID) * 1000 + sortingGroup.sortingOrder;
        }

        Renderer renderer = component.GetComponent<Renderer>() ?? component.GetComponentInParent<Renderer>();
        if (renderer)
        {
            return SortingLayer.GetLayerValueFromID(renderer.sortingLayerID) * 1000 + renderer.sortingOrder;
        }

        return -1000;
    }

    private InteractionContext CreateInteractionContext(InteractionTarget target)
    {
        return new InteractionContext(Actor, this, target, SceneInventory);
    }

    private bool IsBlockingUi(Vector2 pointerScreenPosition)
    {
        return ProbeBlockingUi(pointerScreenPosition, out _);
    }

    private bool ProbeBlockingUi(Vector2 pointerScreenPosition, out bool clickable)
    {
        clickable = false;
        if (Hotbar && Hotbar.BlocksWorldInteractionAt(pointerScreenPosition))
        {
            clickable = true;
            return true;
        }

        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new(EventSystem.current)
        {
            position = pointerScreenPosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            if (!hitObject)
            {
                continue;
            }

            if (hitObject.GetComponentInParent<InteractionPromptPresenter>())
            {
                continue;
            }

            if (hitObject.GetComponentInParent<InteractionContextMenuPresenter>())
            {
                clickable = true;
                return true;
            }

            if (hitObject.GetComponentInParent<InventoryHotbarSlot>())
            {
                clickable = true;
                return true;
            }

            Selectable selectable = hitObject.GetComponentInParent<Selectable>();
            if (selectable)
            {
                clickable = selectable.IsActive() && selectable.IsInteractable();
                return true;
            }
        }

        return false;
    }

    private static int FindCandidateIndex(List<InteractionCandidate> candidates, InteractionTarget target)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Target == target)
            {
                return i;
            }
        }

        return -1;
    }

    private float GetPointerPickRadius(InteractionTarget target, bool dragOnly)
    {
        if (!target)
        {
            return interactionProbeRadius;
        }

        float minimumRadius = dragOnly ? Mathf.Max(interactionProbeRadius, 0.6f) : interactionProbeRadius;
        float radius = target.GetPointerSelectionRadius(minimumRadius);
        if (target.TryGetComponent(out RoomPortal _))
        {
            return Mathf.Max(0.6f, Mathf.Min(radius, 1f));
        }

        return radius;
    }

    private static float GetTargetDistanceSqr(Vector2 point, InteractionTarget target)
    {
        if (!target)
        {
            return float.PositiveInfinity;
        }

        Vector2 interactionPoint = target.InteractionPoint.position;
        return (interactionPoint - point).sqrMagnitude;
    }

    private static int CompareCandidates(InteractionCandidate left, InteractionCandidate right)
    {
        int directHitComparison = left.IsDirectHit.CompareTo(right.IsDirectHit);
        if (directHitComparison != 0)
        {
            return directHitComparison;
        }

        int inRangeComparison = left.IsInInteractionRange.CompareTo(right.IsInInteractionRange);
        if (inRangeComparison != 0)
        {
            return inRangeComparison;
        }

        int anyActionComparison = left.HasAnyEnabledAction.CompareTo(right.HasAnyEnabledAction);
        if (anyActionComparison != 0)
        {
            return anyActionComparison;
        }

        int normalizedDistanceComparison = right.NormalizedDistanceSqr.CompareTo(left.NormalizedDistanceSqr);
        if (normalizedDistanceComparison != 0)
        {
            return normalizedDistanceComparison;
        }

        int distanceComparison = right.DistanceSqr.CompareTo(left.DistanceSqr);
        if (distanceComparison != 0)
        {
            return distanceComparison;
        }

        int primaryActionComparison = left.HasEnabledPrimaryAction.CompareTo(right.HasEnabledPrimaryAction);
        if (primaryActionComparison != 0)
        {
            return primaryActionComparison;
        }

        int sortingComparison = left.SortingScore.CompareTo(right.SortingScore);
        if (sortingComparison != 0)
        {
            return sortingComparison;
        }

        int priorityComparison = left.SelectionPriority.CompareTo(right.SelectionPriority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return 0;
    }

    private float GetActorDistanceSqr(InteractionTarget target)
    {
        if (!target || !Actor)
        {
            return float.PositiveInfinity;
        }

        Vector2 actorPosition = Actor.transform.position;
        Vector2 approachPoint = target.GetApproachPoint(Actor.transform.position);
        return (approachPoint - actorPosition).sqrMagnitude;
    }

    private static float GetNormalizedTargetScore(float pointerDistanceSqr, float actorDistanceSqr, float pointerRadius, float interactionRadius, bool inInteractionRange)
    {
        float pointerScore = pointerRadius > 0.0001f ? pointerDistanceSqr / (pointerRadius * pointerRadius) : pointerDistanceSqr;
        float interactionScore = interactionRadius > 0.0001f && float.IsFinite(actorDistanceSqr)
            ? actorDistanceSqr / (interactionRadius * interactionRadius)
            : 10f;
        return (inInteractionRange ? 0f : 1f) + interactionScore + pointerScore * 0.35f;
    }

    private static int CompareCandidatesDescending(InteractionCandidate left, InteractionCandidate right)
    {
        return CompareCandidates(right, left);
    }

    private static bool IsFinite(Vector3 point)
    {
        return float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
    }

    private readonly struct InteractionCandidate
    {
        public InteractionCandidate(InteractionTarget target, int selectionPriority, bool hasEnabledPrimaryAction, bool hasAnyEnabledAction, int sortingScore, float distanceSqr)
            : this(target, false, false, selectionPriority, hasEnabledPrimaryAction, hasAnyEnabledAction, sortingScore, distanceSqr, distanceSqr)
        {
        }

        public InteractionCandidate(InteractionTarget target, bool isDirectHit, bool isInInteractionRange, int selectionPriority, bool hasEnabledPrimaryAction, bool hasAnyEnabledAction, int sortingScore, float distanceSqr, float normalizedDistanceSqr)
        {
            Target = target;
            IsDirectHit = isDirectHit;
            IsInInteractionRange = isInInteractionRange;
            SelectionPriority = selectionPriority;
            HasEnabledPrimaryAction = hasEnabledPrimaryAction;
            HasAnyEnabledAction = hasAnyEnabledAction;
            SortingScore = sortingScore;
            DistanceSqr = distanceSqr;
            NormalizedDistanceSqr = normalizedDistanceSqr;
        }

        public InteractionTarget Target { get; }
        public bool IsDirectHit { get; }
        public bool IsInInteractionRange { get; }
        public int SelectionPriority { get; }
        public bool HasEnabledPrimaryAction { get; }
        public bool HasAnyEnabledAction { get; }
        public int SortingScore { get; }
        public float DistanceSqr { get; }
        public float NormalizedDistanceSqr { get; }
    }
}
