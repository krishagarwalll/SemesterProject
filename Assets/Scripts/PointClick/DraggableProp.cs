using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InteractionTarget))]
public class DraggableProp : MonoBehaviour, IInteractionActionProvider, IWorldDraggable
{
    private const float SupportSurfaceTolerance = 0.05f;

    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform dragHandle;
    [SerializeField] private NavMeshObstacle navMeshObstacle;
    [SerializeField] private RoomBoundsConstraint boundsConstraint;
    [SerializeField] private string dragLabel = "Drag";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string dragGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField, TextArea] private string inspectText;
    [SerializeField] private bool blocksNavigation = true;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private bool lockZPosition = true;
    [SerializeField] private bool lockRotationX = true;
    [SerializeField] private bool lockRotationY = true;
    [SerializeField, Min(0f)] private float dragSmoothTime = 0.05f;
    [SerializeField, Min(0f)] private float maxLiftHeight = 0.35f;
    [SerializeField, Min(0f)] private float maxDropDistance = 2f;
    [SerializeField, Min(0f)] private float playerClearance = 0.75f;
    [SerializeField] private bool keepGrabOffset = true;

    private PointerContext activePointer;
    private Vector3 dragVelocity;
    private Vector3 dragOffset;
    private float dragBaseHeight;
    private Vector3 lastValidPosition;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool originalUseGravity;
    private bool originalKinematic;
    private bool originalDetectCollisions;
    private RigidbodyConstraints originalConstraints;
    private Collider[] colliders;
    private Renderer[] renderers;
    private PointClickController player;

    public bool IsDragging => activePointer != null;
    public bool SupportsDrag => enabled && gameObject.activeInHierarchy;

    private Rigidbody Body => this.ResolveComponent(ref body);
    private Transform DragHandle => dragHandle ? dragHandle : transform;
    private NavMeshObstacle Obstacle => navMeshObstacle ? navMeshObstacle : navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
    private RoomBoundsConstraint BoundsConstraint => boundsConstraint ? boundsConstraint : boundsConstraint = GetComponent<RoomBoundsConstraint>();
    private Collider[] Colliders => colliders ??= GetComponentsInChildren<Collider>(true);
    private Renderer[] Renderers => renderers ??= GetComponentsInChildren<Renderer>(true);
    private PointClickController Player => player ? player : player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);

    private void Reset()
    {
        this.ResolveComponent(ref body);
        dragHandle = transform;
        navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
        boundsConstraint = GetComponent<RoomBoundsConstraint>();
    }

    private void Awake()
    {
        if (Body)
        {
            lastValidPosition = Body.position;
            startRotation = Body.rotation;
        }

        CachePhysicsState();
        ApplyRestingState();
    }

    private void OnValidate()
    {
        dragSmoothTime = Mathf.Max(0f, dragSmoothTime);
        maxLiftHeight = Mathf.Max(0f, maxLiftHeight);
        maxDropDistance = Mathf.Max(0f, maxDropDistance);
        playerClearance = Mathf.Max(0f, playerClearance);
        if (!dragHandle)
        {
            dragHandle = transform;
        }
    }

    private void Update()
    {
        if (!IsDragging || activePointer == null)
        {
            return;
        }

        if (!activePointer.TryGetDragPoint(dragBaseHeight, maxLiftHeight, out Vector3 candidate))
        {
            return;
        }

        candidate += dragOffset;
        candidate.y = dragBaseHeight + maxLiftHeight;
        if (lockZPosition)
        {
            candidate.z = startPosition.z;
        }

        if (BoundsConstraint)
        {
            candidate = BoundsConstraint.Clamp(candidate);
        }

        if (!TryResolvePlayerOverlap(candidate) || !IsPlacementValid(candidate))
        {
            candidate = lastValidPosition;
        }
        else
        {
            lastValidPosition = candidate;
        }

        Vector3 next = dragSmoothTime <= 0f
            ? candidate
            : Vector3.SmoothDamp(Body.position, candidate, ref dragVelocity, dragSmoothTime, Mathf.Infinity, Time.deltaTime);
        Body.position = next;
    }

    public void GetActions(in InteractionContext context, System.Collections.Generic.List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(this, InteractionMode.Drag, dragLabel, dragGlyphId, requiresApproach: false));
        if (!string.IsNullOrWhiteSpace(inspectText))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (action.Mode == InteractionMode.Inspect && !string.IsNullOrWhiteSpace(inspectText))
        {
            InteractionFeedback.Show(inspectText, this);
            return true;
        }

        if (action.Mode != InteractionMode.Drag || context.Pointer == null)
        {
            return false;
        }

        BeginDrag(context.Pointer);
        return true;
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return pointer && Body && enabled && gameObject.activeInHierarchy;
    }

    public void BeginDrag(PointerContext pointer)
    {
        if (!CanStartDrag(pointer))
        {
            return;
        }

        activePointer = pointer;
        startPosition = Body.position;
        startRotation = Body.rotation;
        dragBaseHeight = Body.position.y;
        lastValidPosition = Body.position;
        dragVelocity = Vector3.zero;
        CachePhysicsState();
        if (!Body.isKinematic)
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }

        Body.isKinematic = true;
        Body.useGravity = false;
        Body.detectCollisions = false;
        Body.constraints = GetConstraints();
        dragOffset = keepGrabOffset && activePointer.TryGetDragPoint(dragBaseHeight, maxLiftHeight, out Vector3 point)
            ? DragHandle.position - point
            : Vector3.zero;
        dragOffset.y = 0f;

        if (Obstacle)
        {
            Obstacle.enabled = false;
        }
    }

    public void EndDrag()
    {
        if (!IsDragging)
        {
            return;
        }

        activePointer = null;
        dragVelocity = Vector3.zero;
        lastValidPosition = GetResolvedReleasePosition(lastValidPosition);
        ApplyRestingState();
    }

    private bool TryResolvePlayerOverlap(Vector3 candidate)
    {
        if (!Player)
        {
            return true;
        }

        Bounds bounds = GetCandidateBounds(candidate);
        if (!bounds.Intersects(Player.GetWorldBounds(playerClearance)))
        {
            return true;
        }

        if (!Player.RequestSmoothClearance(bounds, playerClearance))
        {
            return false;
        }

        return !bounds.Intersects(Player.GetWorldBounds(playerClearance));
    }

    private bool IsPlacementValid(Vector3 candidate)
    {
        Bounds bounds = GetCandidateBounds(candidate);
        return !WorldDragUtility.IsBlocked(bounds, startRotation, blockingLayers, Colliders, SupportSurfaceTolerance);
    }

    private Bounds GetCandidateBounds(Vector3 candidate)
    {
        return WorldDragUtility.GetWorldBounds(Renderers, Colliders, Body.position, candidate);
    }

    private void CachePhysicsState()
    {
        if (!Body)
        {
            return;
        }

        originalKinematic = Body.isKinematic;
        originalUseGravity = Body.useGravity;
        originalDetectCollisions = Body.detectCollisions;
        originalConstraints = Body.constraints;
    }

    private void ApplyRestingState()
    {
        if (!Body)
        {
            return;
        }

        Body.isKinematic = originalKinematic;
        Body.useGravity = originalUseGravity;
        Body.detectCollisions = originalDetectCollisions;
        Body.constraints = originalConstraints | GetConstraints();
        Body.position = lastValidPosition;
        Body.rotation = startRotation;
        if (!Body.isKinematic)
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }
        if (Obstacle)
        {
            Obstacle.enabled = blocksNavigation;
            Obstacle.carving = blocksNavigation;
        }

        Physics.SyncTransforms();
    }

    private RigidbodyConstraints GetConstraints()
    {
        RigidbodyConstraints constraints = RigidbodyConstraints.None;
        if (lockZPosition)
        {
            constraints |= RigidbodyConstraints.FreezePositionZ;
        }

        if (lockRotationX)
        {
            constraints |= RigidbodyConstraints.FreezeRotationX;
        }

        if (lockRotationY)
        {
            constraints |= RigidbodyConstraints.FreezeRotationY;
        }

        return constraints;
    }

    private Vector3 GetResolvedReleasePosition(Vector3 liftedPosition)
    {
        Bounds bounds = GetCandidateBounds(liftedPosition);
        return WorldDragUtility.ResolveRestingPosition(
            bounds,
            liftedPosition,
            startRotation,
            blockingLayers,
            Colliders,
            SupportSurfaceTolerance,
            dragBaseHeight,
            maxDropDistance);
    }
}
