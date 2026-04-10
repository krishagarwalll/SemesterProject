using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class PointClickController : MonoBehaviour
{
    private const float DirectionDeadzone = 0.0001f;

    [Header("NavMesh Agent")]
    [SerializeField] private bool updateRotation;

    [Header("Navigation")]
    [SerializeField, Min(0f)] private float navMeshSampleDistance = 1.5f;
    [SerializeField, Min(0f)] private float navMeshSnapDistance = 2f;
    [SerializeField, Min(0f)] private float destinationReachedThreshold = 0.05f;
    [SerializeField] private bool snapToNavMeshOnEnable = true;
    [SerializeField] private bool ignorePointerOverUi = true;
    [SerializeField, Min(0.1f)] private float sidestepDistance = 1f;
    [SerializeField, Min(0.1f)] private float sidestepSampleRadius = 1.5f;
    [SerializeField, Min(0.01f)] private float sidestepRepathInterval = 0.15f;

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
    private NavMeshAgent navMeshAgent;
    private SpriteFlipper spriteFlipper;
    private Animator animator;
    private PendingAction pendingAction;
    private IWorldDraggable activeDrag;
    private float smoothedSpeed;
    private Vector2 smoothedDirection;
    private Vector3 lastPosition;
    private float nextSidestepRequestTime;
    private bool sidestepActive;

    private PointerContext Pointer => this.ResolveSceneComponent(ref pointer);
    private Inventory SceneInventory => this.ResolveSceneComponent(ref inventory);
    private RoomTransitionService Rooms => this.ResolveSceneComponent(ref roomTransitionService);
    private NavMeshAgent Agent => this.ResolveComponent(ref navMeshAgent);
    private SpriteFlipper Flipper => this.ResolveComponent(ref spriteFlipper, true);
    private Animator Anim => this.ResolveComponent(ref animator, true);
    private Camera ViewCamera => Pointer && Pointer.WorldCamera ? Pointer.WorldCamera : Camera.main;
    private Vector3 Position => Agent ? Agent.transform.position : transform.position;
    private bool HasPendingAction => pendingAction.IsValid;
    private bool IsPointerBlocked => ignorePointerOverUi && Pointer && Pointer.IsPointerOverUi;
    private bool HasReachedDestination => Agent
        && Agent.isOnNavMesh
        && !Agent.pathPending
        && (!Agent.hasPath || Agent.remainingDistance <= Mathf.Max(Agent.stoppingDistance, destinationReachedThreshold));

    private void Awake()
    {
        SyncAgent();
        ResetPresentationState();
    }

    private void OnEnable()
    {
        SyncAgent();
        ResetPresentationState();
        SubscribePointerEvents();
        if (snapToNavMeshOnEnable)
        {
            Agent.TrySnapToNavMesh(transform, navMeshSnapDistance);
        }
    }

    private void OnDisable()
    {
        UnsubscribePointerEvents();
        Cancel();
        ResetPresentationState();
    }

    private void OnValidate()
    {
        navMeshSampleDistance = Mathf.Max(0f, navMeshSampleDistance);
        navMeshSnapDistance = Mathf.Max(0f, navMeshSnapDistance);
        destinationReachedThreshold = Mathf.Max(0f, destinationReachedThreshold);
        sidestepDistance = Mathf.Max(0.1f, sidestepDistance);
        sidestepSampleRadius = Mathf.Max(0.1f, sidestepSampleRadius);
        sidestepRepathInterval = Mathf.Max(0.01f, sidestepRepathInterval);
        movingThreshold = Mathf.Max(0f, movingThreshold);
        animationSmoothing = Mathf.Max(0f, animationSmoothing);
        flipThreshold = Mathf.Max(0f, flipThreshold);
        SyncAgent();
    }

    private void Update()
    {
        RefreshPresentation();
        if (!Agent)
        {
            return;
        }

        if (HasReachedDestination)
        {
            Agent.StopPath();
            sidestepActive = false;
        }

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

    public bool TryWarp(Vector3 worldPosition)
    {
        if (!Agent || !Agent.TryWarpTo(transform, worldPosition, navMeshSampleDistance, navMeshSnapDistance, out Vector3 sampledPosition))
        {
            return false;
        }

        lastPosition = sampledPosition;
        Agent.StopPath();
        return true;
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
        if (!target || !target.TryGetAction(CreateContext(target), mode, out InteractionAction action))
        {
            return false;
        }

        return ExecuteAction(target, action);
    }

    public bool ExecuteAction(InteractionTarget target, InteractionAction action)
    {
        if (!target || !action.IsValid || !action.Enabled)
        {
            return false;
        }

        if (action.Mode == InteractionMode.Drag && target.TryGetDraggable(out IWorldDraggable draggable))
        {
            BeginDrag(draggable);
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
        if (!Agent || !Agent.isOnNavMesh)
        {
            return false;
        }

        Bounds currentBounds = GetWorldBounds(clearance);
        if (!currentBounds.Intersects(blockerBounds))
        {
            sidestepActive = false;
            return true;
        }

        if (sidestepActive && Time.time < nextSidestepRequestTime)
        {
            return true;
        }

        Vector3 playerCenter = currentBounds.center;
        Vector3 away = playerCenter - blockerBounds.ClosestPoint(playerCenter);
        away.y = 0f;
        if (away.sqrMagnitude <= DirectionDeadzone)
        {
            away = Vector3.right;
        }

        away.Normalize();
        Vector3[] directions =
        {
            away,
            Quaternion.Euler(0f, 20f, 0f) * away,
            Quaternion.Euler(0f, -20f, 0f) * away,
            Quaternion.Euler(0f, 40f, 0f) * away,
            Quaternion.Euler(0f, -40f, 0f) * away
        };
        float baseDistance = Mathf.Max(clearance * 0.75f, sidestepDistance * 0.75f);
        float[] distances = { baseDistance, baseDistance * 1.1f, baseDistance * 1.25f };

        for (int distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 candidate = transform.position + directions[i] * distances[distanceIndex];
                candidate = ClampToActiveRoom(candidate);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sidestepSampleRadius, Agent.areaMask))
                {
                    continue;
                }

                Bounds shiftedBounds = currentBounds;
                shiftedBounds.center += hit.position - transform.position;
                if (shiftedBounds.Intersects(blockerBounds))
                {
                    continue;
                }

                if (!Agent.SetDestination(hit.position))
                {
                    continue;
                }

                Agent.isStopped = false;
                sidestepActive = true;
                nextSidestepRequestTime = Time.time + sidestepRepathInterval;
                return true;
            }
        }

        return false;
    }

    public Bounds GetWorldBounds(float padding)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        Bounds bounds = new(transform.position, Vector3.zero);
        bool initialized = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i] || !colliders[i].enabled)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = colliders[i].bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds.min);
                bounds.Encapsulate(colliders[i].bounds.max);
            }
        }

        bounds.Expand(padding * 2f);
        return bounds;
    }

    private void SubscribePointerEvents()
    {
        if (!Pointer)
        {
            return;
        }

        Pointer.PrimaryClicked += HandlePrimaryClick;
        Pointer.DragStarted += HandleDragStarted;
    }

    private void UnsubscribePointerEvents()
    {
        if (!pointer)
        {
            return;
        }

        pointer.PrimaryClicked -= HandlePrimaryClick;
        pointer.DragStarted -= HandleDragStarted;
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
                TrySetDestination(ClampToActiveRoom(point));
            }

            return;
        }

        InteractionTarget target = context.ClickedTarget;
        if (!target.TryGetPreferredAction(CreateContext(target), out InteractionAction action))
        {
            return;
        }

        ExecuteAction(target, action);
    }

    private void HandleDragStarted(PointerContext context)
    {
        if (IsPointerBlocked)
        {
            return;
        }

        InteractionTarget target = context.DragTarget;
        if (!target || !target.TryGetDraggable(out IWorldDraggable draggable))
        {
            return;
        }

        BeginDrag(draggable);
    }

    private void BeginDrag(IWorldDraggable draggable)
    {
        if (draggable == null || !draggable.CanStartDrag(Pointer))
        {
            return;
        }

        StopAndClear();
        draggable.BeginDrag(Pointer);
        activeDrag = draggable.IsDragging ? draggable : null;
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
            if (!Agent.pathPending && (!Agent.hasPath || Agent.pathStatus == NavMeshPathStatus.PathInvalid) && !TryApproach(pendingAction.Target))
            {
                pendingAction = default;
            }

            return;
        }

        InteractionTarget target = pendingAction.Target;
        InteractionAction action = pendingAction.Action;
        StopAndClear();
        target.Execute(CreateContext(target), action);
        pendingAction = default;
    }

    private bool TryApproach(InteractionTarget target)
    {
        return target && TrySetDestination(target.GetApproachPoint(Position));
    }

    private bool TrySetDestination(Vector3 worldPosition)
    {
        worldPosition = ClampToActiveRoom(worldPosition);
        if (!Agent.TrySetDestination(transform, worldPosition, navMeshSampleDistance, navMeshSnapDistance, out _))
        {
            return false;
        }

        sidestepActive = false;
        Agent.isStopped = false;
        return true;
    }

    private void SyncAgent()
    {
        if (!Agent)
        {
            return;
        }

        Agent.updateRotation = updateRotation;
    }

    private void StopAndClear()
    {
        if (Agent)
        {
            Agent.StopPath();
        }

        sidestepActive = false;
        pendingAction = default;
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
        Vector3 velocity = Agent ? Agent.velocity : Vector3.zero;
        velocity.y = 0f;
        if (velocity.sqrMagnitude <= DirectionDeadzone && Time.deltaTime > 0f)
        {
            velocity = (transform.position - lastPosition) / Time.deltaTime;
            velocity.y = 0f;
        }

        Vector2 direction = GetLocalMoveDirection(velocity);
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

    private Vector2 GetLocalMoveDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude <= DirectionDeadzone)
        {
            return Vector2.zero;
        }

        Vector3 forward = ViewCamera ? ViewCamera.transform.forward : Vector3.forward;
        Vector3 right = ViewCamera ? ViewCamera.transform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude <= DirectionDeadzone || right.sqrMagnitude <= DirectionDeadzone)
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }

        forward.Normalize();
        right.Normalize();
        float speed = Agent && Agent.speed > DirectionDeadzone ? Agent.speed : worldDirection.magnitude;
        if (speed <= DirectionDeadzone)
        {
            return Vector2.zero;
        }

        return new Vector2(
            Mathf.Clamp(Vector3.Dot(worldDirection, right) / speed, -1f, 1f),
            Mathf.Clamp(Vector3.Dot(worldDirection, forward) / speed, -1f, 1f));
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

    private Vector3 ClampToActiveRoom(Vector3 point)
    {
        Room activeRoom = Rooms ? Rooms.ActiveRoom : null;
        return activeRoom ? activeRoom.ClampPosition(point) : point;
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
