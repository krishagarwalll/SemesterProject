using UnityEngine;

[DisallowMultipleComponent]
public class PointClickController : MonoBehaviour
{
    private const float DirectionDeadzone = 0.0001f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField] private LayerMask walkableLayers = 1 << 6;
    [SerializeField] private bool ignorePointerOverUi = true;
    [SerializeField, Min(0f)] private float destinationReachedThreshold = 0.05f;
    [SerializeField, Min(0f)] private float groundedPositionOffset = 0.5f;

    [Header("Animation")]
    [SerializeField] private string movingParameter = "isMoving";
    [SerializeField] private string horizontalSpeedParameter = "moveX";
    [SerializeField] private string verticalSpeedParameter = "moveY";
    [SerializeField, Min(0f)] private float movingThreshold = 0.05f;
    [SerializeField, Min(0f)] private float animationSmoothing = 10f;
    [SerializeField, Min(0f)] private float flipThreshold = 0.05f;

    private PointerContext pointer;
    private Inventory inventory;
    private RoomTransitionService roomTransitionService;
    private SpriteFlipper spriteFlipper;
    private Animator animator;
    private Rigidbody2D body2D;
    private Collider2D[] colliders2D;
    private SpriteRenderer[] spriteRenderers;
    private PendingAction pendingAction;
    private IWorldDraggable activeDrag;
    private Vector3 movementDestination;
    private float smoothedSpeed;
    private Vector2 smoothedDirection;
    private Vector3 lastPosition;
    private float fixedDepth;
    private float cachedGroundedOffset;
    private bool hasDestination;
    private bool groundedOffsetResolved;

    private PointerContext Pointer => this.ResolveSceneComponent(ref pointer);
    private Inventory SceneInventory => this.ResolveSceneComponent(ref inventory);
    private RoomTransitionService Rooms => this.ResolveSceneComponent(ref roomTransitionService);
    private SpriteFlipper Flipper => this.ResolveComponent(ref spriteFlipper, true);
    private Animator Anim => this.ResolveComponent(ref animator, true);
    private Rigidbody2D Body2D => this.ResolveComponent(ref body2D, true);
    private Collider2D[] Colliders2D => colliders2D ??= GetComponentsInChildren<Collider2D>(true);
    private SpriteRenderer[] SpriteRenderers => spriteRenderers ??= GetComponentsInChildren<SpriteRenderer>(true);
    private Vector3 Position => transform.position;
    private bool HasPendingAction => pendingAction.IsValid;
    private bool IsPointerBlocked => ignorePointerOverUi && Pointer && Pointer.IsPointerOverUi && activeDrag == null;
    public bool HasActiveInteraction => activeDrag != null || HasPendingAction;

    private void Awake()
    {
        fixedDepth = transform.position.z;
        ApplyRuntimeSetup();
        ResetPresentationState();
        ResolveGroundedOffset();
        SnapToGroundAtCurrentPosition();
    }

    private void OnEnable()
    {
        fixedDepth = transform.position.z;
        ApplyRuntimeSetup();
        SubscribePointerEvents();
        ResetPresentationState();
        ResolveGroundedOffset();
        SnapToGroundAtCurrentPosition();
    }

    private void OnDisable()
    {
        UnsubscribePointerEvents();
        Cancel();
        ResetPresentationState();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        destinationReachedThreshold = Mathf.Max(0f, destinationReachedThreshold);
        groundedPositionOffset = Mathf.Max(0f, groundedPositionOffset);
        movingThreshold = Mathf.Max(0f, movingThreshold);
        animationSmoothing = Mathf.Max(0f, animationSmoothing);
        flipThreshold = Mathf.Max(0f, flipThreshold);
        groundedOffsetResolved = false;
    }

    private void Update()
    {
        RefreshPresentation();

        if (activeDrag != null)
        {
            if (Pointer && Pointer.DragEndedThisFrame)
            {
                activeDrag.EndDrag();
                activeDrag = null;
            }

            return;
        }

        if (HasPendingAction)
        {
            UpdatePendingAction();
        }
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    public bool TryWarp(Vector3 worldPosition)
    {
        if (TryResolveDestination(worldPosition, out Vector3 resolvedPosition))
        {
            SetWorldPosition(resolvedPosition);
            StopPath();
            return true;
        }

        SetWorldPosition(ClampToActiveRoom(worldPosition));
        StopPath();
        return false;
    }

    public void GetAvailableActions(InteractionTarget target, System.Collections.Generic.List<InteractionAction> actions)
    {
        if (!target)
        {
            actions.Clear();
            return;
        }

        target.GetActions(CreateContext(target), actions);
    }

    public bool ExecuteAction(InteractionTarget target, InteractionMode mode)
    {
        return target && target.TryGetAction(CreateContext(target), mode, out InteractionAction action) && ExecuteAction(target, action);
    }

    public bool ExecuteAction(InteractionTarget target, InteractionAction action)
    {
        if (!target || !action.IsValid || !action.Enabled)
        {
            return false;
        }

        if (action.Mode == InteractionMode.Drag && target.TryGetDraggable(out IWorldDraggable draggable))
        {
            if (!Pointer)
            {
                return false;
            }

            StopAndClear();
            draggable.BeginDrag(Pointer);
            if (!draggable.IsDragging)
            {
                return false;
            }

            activeDrag = draggable;
            return true;
        }

        if (!action.RequiresApproach || target.IsInRange(Position))
        {
            StopAndClear();
            return target.Execute(CreateContext(target), action);
        }

        if (!TryApproach(target))
        {
            return false;
        }

        pendingAction = new PendingAction(target, action);
        return true;
    }

    public bool RequestSmoothClearance(Bounds blockerBounds, float clearance)
    {
        return true;
    }

    public Bounds GetWorldBounds(float padding)
    {
        if (Colliders2D.Length > 0)
        {
            Bounds bounds = new(transform.position, Vector3.zero);
            bool initialized = false;
            for (int i = 0; i < Colliders2D.Length; i++)
            {
                if (!Colliders2D[i].IsUsable())
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = Colliders2D[i].bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(Colliders2D[i].bounds.min);
                    bounds.Encapsulate(Colliders2D[i].bounds.max);
                }
            }

            bounds.Expand(padding * 2f);
            return bounds;
        }

        return new Bounds(transform.position, Vector3.one + Vector3.one * padding * 2f);
    }

    private void SubscribePointerEvents()
    {
        if (!Pointer)
        {
            return;
        }

        Pointer.PrimaryClicked += HandlePrimaryClick;
        Pointer.DragStarted += HandleDragStarted;
        Pointer.DragUpdated += HandleDragUpdated;
    }

    private void UnsubscribePointerEvents()
    {
        if (!pointer)
        {
            return;
        }

        pointer.PrimaryClicked -= HandlePrimaryClick;
        pointer.DragStarted -= HandleDragStarted;
        pointer.DragUpdated -= HandleDragUpdated;
    }

    private void HandlePrimaryClick(PointerContext context)
    {
        if (IsPointerBlocked)
        {
            return;
        }

        if (!context.ClickedTarget)
        {
            if (context.TryGetWalkPoint(out Vector3 point))
            {
                TrySetDestination(point);
            }

            return;
        }

        InteractionTarget target = context.ClickedTarget;
        if (!target.TryGetPrimaryAction(CreateContext(target), out InteractionAction action))
        {
            if (target.SupportsDrag)
            {
                TryApproach(target);
            }

            return;
        }

        ExecuteAction(target, action);
    }

    private void HandleDragStarted(PointerContext context)
    {
        InteractionTarget target = context.DragTarget;
        if (!target && context)
        {
            context.TryGetWorldDragTarget(out target);
        }

        if (!target || !target.TryGetDraggable(out IWorldDraggable draggable) || !Pointer)
        {
            return;
        }

        StopAndClear();
        draggable.BeginDrag(Pointer);
        if (draggable.IsDragging)
        {
            activeDrag = draggable;
        }
    }

    private void HandleDragUpdated(PointerContext context)
    {
        activeDrag?.UpdateDrag(context);
    }

    private InteractionContext CreateContext(InteractionTarget target)
    {
        return new InteractionContext(this, Pointer, target, SceneInventory);
    }

    private void UpdatePendingAction()
    {
        if (!pendingAction.IsValid)
        {
            return;
        }

        if (!pendingAction.Target || !pendingAction.Action.IsValid)
        {
            pendingAction = default;
            return;
        }

        if (!pendingAction.Target.IsInRange(Position))
        {
            if (!hasDestination && !TryApproach(pendingAction.Target))
            {
                pendingAction = default;
            }

            return;
        }

        InteractionTarget target = pendingAction.Target;
        InteractionAction action = pendingAction.Action;
        pendingAction = default;
        ExecuteAction(target, action);
    }

    private bool TryApproach(InteractionTarget target)
    {
        return target && TrySetDestination(target.GetApproachPoint(Position));
    }

    private bool TrySetDestination(Vector3 worldPosition)
    {
        if (!TryResolveDestination(worldPosition, out movementDestination))
        {
            return false;
        }

        hasDestination = true;
        return true;
    }

    private bool TryResolveDestination(Vector3 worldPosition, out Vector3 resolvedDestination)
    {
        resolvedDestination = ClampToActiveRoom(worldPosition);
        return TryResolveGroundDestination(resolvedDestination.x, out resolvedDestination);
    }

    private bool TryResolveGroundDestination(float targetX, out Vector3 resolvedDestination)
    {
        resolvedDestination = transform.position;
        Room currentRoom = GetCurrentRoom();
        if (!currentRoom)
        {
            return false;
        }

        Vector3 requested = new(targetX, currentRoom.GroundY, fixedDepth);
        resolvedDestination = currentRoom.ClampPoint(requested, fixedDepth);
        return true;
    }

    private void UpdateMovement()
    {
        if (!hasDestination || activeDrag != null)
        {
            return;
        }

        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, movementDestination, moveSpeed * Time.deltaTime);
        SetWorldPosition(next);
        if ((movementDestination - transform.position).sqrMagnitude <= destinationReachedThreshold * destinationReachedThreshold)
        {
            SetWorldPosition(movementDestination);
            StopPath();
        }
    }

    private void StopAndClear()
    {
        StopPath();
        pendingAction = default;
    }

    private void StopPath()
    {
        hasDestination = false;
        movementDestination = transform.position;
    }

    private void Cancel()
    {
        StopAndClear();
        if (activeDrag != null)
        {
            activeDrag.EndDrag();
            activeDrag = null;
        }
    }

    private void ResetPresentationState()
    {
        smoothedSpeed = 0f;
        smoothedDirection = Vector2.zero;
        lastPosition = transform.position;
        ApplyPresentation(Vector2.zero, false);
    }

    private void RefreshPresentation()
    {
        Vector3 velocity = Time.deltaTime > 0f ? (transform.position - lastPosition) / Time.deltaTime : Vector3.zero;
        Vector2 direction = velocity.sqrMagnitude <= DirectionDeadzone ? Vector2.zero : ((Vector2)velocity).normalized;
        float speed = velocity.magnitude;
        smoothedSpeed = animationSmoothing <= 0f ? speed : Mathf.MoveTowards(smoothedSpeed, speed, animationSmoothing * Time.deltaTime);
        smoothedDirection = animationSmoothing <= 0f ? direction : Vector2.MoveTowards(smoothedDirection, direction, animationSmoothing * Time.deltaTime);
        ApplyPresentation(smoothedDirection, smoothedSpeed > movingThreshold);
        lastPosition = transform.position;
    }

    private void ApplyPresentation(Vector2 direction, bool moving)
    {
        if (Flipper && direction.sqrMagnitude > flipThreshold * flipThreshold)
        {
            Flipper.SetFacing(direction);
        }

        if (!Anim)
        {
            return;
        }

        SetBool(movingParameter, moving);
        SetFloat(horizontalSpeedParameter, direction.x);
        SetFloat(verticalSpeedParameter, direction.y);
    }

    private void SetBool(string parameterName, bool value)
    {
        if (!Anim || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        Anim.SetBool(Animator.StringToHash(parameterName), value);
    }

    private void SetFloat(string parameterName, float value)
    {
        if (!Anim || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        Anim.SetFloat(Animator.StringToHash(parameterName), value);
    }

    private void SetWorldPosition(Vector3 worldPosition)
    {
        worldPosition.z = fixedDepth;
        if (Body2D)
        {
            Body2D.position = new Vector2(worldPosition.x, worldPosition.y);
            Body2D.linearVelocity = Vector2.zero;
            transform.position = worldPosition;
            return;
        }

        transform.position = worldPosition;
    }

    private void ApplyRuntimeSetup()
    {
        int layer = LayerMask.NameToLayer("Character");
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = "Character";
        }
    }

    private Vector3 ClampToActiveRoom(Vector3 point)
    {
        Room activeRoom = GetCurrentRoom();
        point.z = fixedDepth;
        return activeRoom ? activeRoom.ClampPosition(point) : point;
    }

    private float GetGroundedOffset()
    {
        if (!groundedOffsetResolved)
        {
            ResolveGroundedOffset();
        }

        return cachedGroundedOffset;
    }

    private void ResolveGroundedOffset()
    {
        groundedOffsetResolved = true;
        cachedGroundedOffset = groundedPositionOffset;
        if (groundedPositionOffset > 0f)
        {
            return;
        }

        bool hasVisualBounds = false;
        float visualOffset = 0f;
        for (int i = 0; i < SpriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = SpriteRenderers[i];
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            float offset = transform.position.y - renderer.bounds.min.y;
            if (!hasVisualBounds || offset < visualOffset)
            {
                visualOffset = offset;
                hasVisualBounds = true;
            }
        }

        if (hasVisualBounds)
        {
            cachedGroundedOffset = Mathf.Max(0f, visualOffset);
            return;
        }

        bool initialized = false;
        float bestOffset = 0f;
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            Collider2D collider = Colliders2D[i];
            if (!collider || !collider.enabled)
            {
                continue;
            }

            float offset = transform.position.y - collider.bounds.min.y;
            if (!initialized || offset > bestOffset)
            {
                bestOffset = offset;
                initialized = true;
            }
        }

        cachedGroundedOffset = initialized ? bestOffset : 0.5f;
    }

    private void SnapToGroundAtCurrentPosition()
    {
        Room currentRoom = GetCurrentRoom();
        if (!currentRoom)
        {
            return;
        }

        Vector3 groundedPosition = currentRoom.ClampPoint(new Vector3(transform.position.x, currentRoom.GroundY, fixedDepth), fixedDepth);
        Vector3 current = transform.position;
        groundedPosition.z = fixedDepth;
        if ((current - groundedPosition).sqrMagnitude <= DirectionDeadzone)
        {
            return;
        }

        SetWorldPosition(groundedPosition);
    }

    private Room GetCurrentRoom()
    {
        if (Rooms && Rooms.ActiveRoom)
        {
            return Rooms.ActiveRoom;
        }

        Room[] rooms = FindObjectsByType<Room>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] && rooms[i].ContainsPoint(transform.position))
            {
                return rooms[i];
            }
        }

        return GetComponentInParent<Room>(true);
    }

    private readonly struct PendingAction
    {
        public PendingAction(InteractionTarget target, InteractionAction action)
        {
            Target = target;
            Action = action;
        }

        public InteractionTarget Target { get; }
        public InteractionAction Action { get; }
        public bool IsValid => Target && Action.IsValid;
    }
}
