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

    [Header("Animation")]
    [SerializeField] private string movingParameter = "isMoving";
    [SerializeField] private string horizontalSpeedParameter = "moveX";
    [SerializeField] private string verticalSpeedParameter = "moveY";
    [SerializeField, Min(0f)] private float movingThreshold = 0.05f;
    [SerializeField, Min(0f)] private float animationSmoothing = 10f;
    [SerializeField, Min(0f)] private float flipThreshold = 0.05f;

    private PointerContext pointer;
    private NavMeshAgent navMeshAgent;
    private SpriteFlipper spriteFlipper;
    private Animator animator;
    private InteractionTarget pendingTarget;
    private IWorldInteractable pendingInteractable;
    private IWorldDraggable activeDrag;
    private ObstacleAvoidanceType defaultAvoidanceType;
    private float smoothedSpeed;
    private Vector2 smoothedDirection;
    private Vector3 lastPosition;

    private PointerContext Pointer => this.ResolveSceneComponent(ref pointer);
    private NavMeshAgent Agent => this.ResolveComponent(ref navMeshAgent);
    private SpriteFlipper Flipper => this.ResolveComponent(ref spriteFlipper, true);
    private Animator Anim => this.ResolveComponent(ref animator, true);
    private Camera ViewCamera => Pointer && Pointer.WorldCamera ? Pointer.WorldCamera : Camera.main;
    private Vector3 Position => Agent ? Agent.transform.position : transform.position;
    private bool IsNavigating => Agent && Agent.isOnNavMesh && (Agent.pathPending || Agent.hasPath && Agent.pathStatus != NavMeshPathStatus.PathInvalid);
    private bool HasPendingInteraction => pendingTarget && pendingInteractable != null;
    private bool HasReachedDestination => Agent
        && Agent.isOnNavMesh
        && !Agent.pathPending
        && (!Agent.hasPath || Agent.remainingDistance <= Mathf.Max(Agent.stoppingDistance, destinationReachedThreshold));
    private bool IsPointerBlocked => ignorePointerOverUi && Pointer && Pointer.IsPointerOverUi;

    private void Awake()
    {
        SyncAgent();
        ResetPresentationState();
    }

    private void OnValidate()
    {
        ClampSettings();
        SyncAgent();
    }

    private void OnEnable()
    {
        SyncAgent();
        ResetPresentationState();
        if (snapToNavMeshOnEnable)
        {
            TrySnapToNavMesh();
        }
    }

    private void ClampSettings()
    {
        navMeshSampleDistance = Mathf.Max(0f, navMeshSampleDistance);
        navMeshSnapDistance = Mathf.Max(0f, navMeshSnapDistance);
        destinationReachedThreshold = Mathf.Max(0f, destinationReachedThreshold);
        movingThreshold = Mathf.Max(0f, movingThreshold);
        animationSmoothing = Mathf.Max(0f, animationSmoothing);
        flipThreshold = Mathf.Max(0f, flipThreshold);
    }

    private void SyncAgent()
    {
        if (Agent)
        {
            if (activeDrag == null)
            {
                defaultAvoidanceType = Agent.obstacleAvoidanceType;
            }

            Agent.updateRotation = updateRotation;
            Agent.obstacleAvoidanceType = activeDrag == null ? defaultAvoidanceType : ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }

    private void OnDisable()
    {
        Cancel();
        ResetPresentationState();
    }

    private void ResetPresentationState()
    {
        smoothedSpeed = 0f;
        smoothedDirection = Vector2.zero;
        lastPosition = transform.position;
        ApplyPresentation(Vector2.zero, false);
    }

    private void Update()
    {
        RefreshPresentation();
        if (!Pointer || !Agent)
        {
            return;
        }

        if (HasReachedDestination)
        {
            Stop();
        }

        if (Pointer.SecondaryPressedThisFrame)
        {
            Cancel();
            return;
        }

        if (activeDrag != null)
        {
            UpdateActiveDrag();
            return;
        }

        if (HasPendingInteraction)
        {
            UpdatePendingInteraction();
            return;
        }

        if (!IsPointerBlocked && Pointer.DragStartedThisFrame)
        {
            StartDrag();
            return;
        }

        if (!IsPointerBlocked && Pointer.PrimaryClickedThisFrame)
        {
            HandlePrimaryClick();
        }
    }

    private void Stop()
    {
        Agent.StopPath();
    }

    private bool TrySnapToNavMesh()
    {
        return Agent.TrySnapToNavMesh(transform, navMeshSnapDistance);
    }

    private bool TryApproach(InteractionTarget target)
    {
        return target && TrySetDestination(target.GetApproachPoint(Position));
    }

    private bool IsInRange(InteractionTarget target, float extraDistance = 0f)
    {
        return target && target.IsInRange(Position, extraDistance);
    }

    private bool TrySetDestination(Vector3 worldPosition)
    {
        if (!Agent.TrySetDestination(transform, worldPosition, navMeshSampleDistance, navMeshSnapDistance, out _))
        {
            return false;
        }

        Agent.isStopped = false;
        return true;
    }

    public bool TryDisplace(Vector3 worldOffset)
    {
        if (!Agent || worldOffset.sqrMagnitude <= DirectionDeadzone)
        {
            return false;
        }

        Vector3 targetPosition = Position + worldOffset;
        targetPosition.y = Position.y;
        if (!Agent.TryWarpTo(transform, targetPosition, navMeshSampleDistance, navMeshSnapDistance, out Vector3 sampledPosition))
        {
            return false;
        }

        Agent.StopPath();
        lastPosition = sampledPosition;
        return true;
    }

    private void UpdateActiveDrag()
    {
        if (Pointer.PrimaryReleasedThisFrame)
        {
            ClearActiveDrag();
            return;
        }

        if (!Pointer.IsPrimaryPressed)
        {
            ClearActiveDrag();
        }
    }

    private void UpdatePendingInteraction()
    {
        if (pendingInteractable == null || pendingTarget == null || !pendingInteractable.CanInteract(this))
        {
            Stop();
            ClearPendingInteraction();
            return;
        }

        if (!IsInRange(pendingTarget))
        {
            if (!IsNavigating && !TryApproach(pendingTarget))
            {
                ClearPendingInteraction();
            }

            return;
        }

        Stop();
        pendingInteractable.Interact(this);
        ClearPendingInteraction();
    }

    private void HandlePrimaryClick()
    {
        if (Pointer.ClickedTarget)
        {
            QueueInteraction(Pointer.ClickedTarget);
            return;
        }

        if (Pointer.TryGetWalkPoint(out Vector3 point))
        {
            TrySetDestination(point);
        }
    }

    private void QueueInteraction(InteractionTarget target)
    {
        if (!target || !target.TryGetInteractable(out IWorldInteractable interactable) || !interactable.CanInteract(this))
        {
            return;
        }

        if (IsInRange(target))
        {
            Stop();
            interactable.Interact(this);
            return;
        }

        if (!TryApproach(target))
        {
            return;
        }

        pendingTarget = target;
        pendingInteractable = interactable;
    }

    private void StartDrag()
    {
        InteractionTarget target = Pointer.DragTarget;
        if (!target || !target.TryGetDraggable(out IWorldDraggable draggable) || !draggable.CanStartDrag(Pointer))
        {
            return;
        }

        Stop();
        ClearPendingInteraction();
        draggable.BeginDrag(Pointer);
        activeDrag = draggable.IsDragging ? draggable : null;
        SyncAgent();
    }

    private void Cancel()
    {
        Stop();
        ClearActiveDrag();
        ClearPendingInteraction();
    }

    private void ClearActiveDrag()
    {
        if (activeDrag == null)
        {
            return;
        }

        activeDrag.EndDrag();
        activeDrag = null;
        SyncAgent();
    }

    private void ClearPendingInteraction()
    {
        pendingTarget = null;
        pendingInteractable = null;
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

        Animator animationController = Anim;
        if (!animationController)
        {
            return;
        }

        SetBool(animationController, movingParameter, moving);
        SetFloat(animationController, horizontalSpeedParameter, direction.x);
        SetFloat(animationController, verticalSpeedParameter, direction.y);
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

    private static void SetBool(Animator animator, string parameterName, bool value)
    {
        if (!animator || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        animator.SetBool(Animator.StringToHash(parameterName), value);
    }

    private static void SetFloat(Animator animator, string parameterName, float value)
    {
        if (!animator || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        animator.SetFloat(Animator.StringToHash(parameterName), value);
    }
}
