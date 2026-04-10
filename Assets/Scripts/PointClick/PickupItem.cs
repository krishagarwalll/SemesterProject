using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class PickupItem : MonoBehaviour, IInteractionActionProvider, IWorldDraggable
{
    private const float SupportSurfaceTolerance = 0.05f;

    [SerializeField] private InventoryItemDefinition itemDefinition;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField] private Transform dragHandle;
    [SerializeField] private RoomBoundsConstraint boundsConstraint;
    [SerializeField] private string dragLabel = "Store";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string dragGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField, TextArea] private string inspectText;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private bool lockZPosition = true;
    [SerializeField] private bool lockRotationX = true;
    [SerializeField] private bool lockRotationY = true;
    [SerializeField, Min(0f)] private float dragSmoothTime = 0.05f;
    [SerializeField, Min(0f)] private float liftHeight = 0.35f;
    [SerializeField, Min(0f)] private float maxDropDistance = 2f;
    [SerializeField, Min(0f)] private float playerClearance = 0.75f;
    [SerializeField] private bool keepGrabOffset = true;

    private enum DragMode
    {
        None = 0,
        World = 1,
        InventoryPreview = 2
    }

    private InventoryTransferController transferController;
    private Transform transferRoot;
    private RoomBoundsConstraint resolvedBoundsConstraint;
    private Collider[] colliders;
    private Rigidbody[] bodies;
    private Renderer[] renderers;
    private NavMeshObstacle[] obstacles;
    private InteractionTarget[] targets;
    private bool[] colliderEnabled;
    private bool[] bodyUseGravity;
    private bool[] bodyKinematic;
    private bool[] bodyDetectCollisions;
    private RigidbodyConstraints[] bodyConstraints;
    private RigidbodyInterpolation[] bodyInterpolation;
    private CollisionDetectionMode[] bodyCollisionDetection;
    private bool[] obstacleEnabled;
    private bool[] targetEnabled;
    private bool cachedWorldState;
    private PointClickController player;
    private PointerContext activePointer;
    private DragMode dragMode;
    private Vector3 dragVelocity;
    private Vector3 dragOffset;
    private float dragBaseHeight;
    private Vector3 startPosition;
    private Vector3 lastValidPosition;
    private Quaternion startRotation;

    public InventoryItemDefinition ItemDefinition => itemDefinition;
    public int Quantity => quantity;
    public Room OwnerRoom => TransferRoot.GetComponentInParent<Room>(true);
    public bool SupportsDrag => enabled && gameObject.activeInHierarchy && itemDefinition;
    public bool IsDragging => dragMode != DragMode.None;
    public Transform TransferRoot => transferRoot ? transferRoot : transferRoot = ResolveTransferRoot();
    public Vector3 RootPosition => TransferRoot.position;
    public Quaternion RootRotation => TransferRoot.rotation;

    private InventoryTransferController TransferController => transferController ? transferController : transferController = FindFirstObjectByType<InventoryTransferController>(FindObjectsInactive.Include);
    private Transform DragHandle => dragHandle ? dragHandle : TransferRoot;
    private RoomBoundsConstraint BoundsConstraint
    {
        get
        {
            if (boundsConstraint)
            {
                return boundsConstraint;
            }

            return resolvedBoundsConstraint ? resolvedBoundsConstraint : resolvedBoundsConstraint = TransferRoot.GetComponent<RoomBoundsConstraint>();
        }
    }
    private Collider[] Colliders => colliders ??= TransferRoot.GetComponentsInChildren<Collider>(true);
    private Rigidbody[] Bodies => bodies ??= TransferRoot.GetComponentsInChildren<Rigidbody>(true);
    private Renderer[] Renderers => renderers ??= TransferRoot.GetComponentsInChildren<Renderer>(true);
    private NavMeshObstacle[] Obstacles => obstacles ??= TransferRoot.GetComponentsInChildren<NavMeshObstacle>(true);
    private InteractionTarget[] Targets => targets ??= TransferRoot.GetComponentsInChildren<InteractionTarget>(true);
    private PointClickController Player => player ? player : player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);

    private void Reset()
    {
        dragHandle = transform;
        boundsConstraint = GetComponent<RoomBoundsConstraint>();
    }

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
        dragSmoothTime = Mathf.Max(0f, dragSmoothTime);
        liftHeight = Mathf.Max(0f, liftHeight);
        maxDropDistance = Mathf.Max(0f, maxDropDistance);
        playerClearance = Mathf.Max(0f, playerClearance);
        if (!dragHandle)
        {
            dragHandle = transform;
        }

        if (!transferRoot)
        {
            transferRoot = ResolveTransferRoot();
        }
    }

    private void Awake()
    {
        CacheWorldState();
        ApplyWorldState();
        lastValidPosition = RootPosition;
        startRotation = RootRotation;
    }

    private void OnEnable()
    {
        if (dragMode == DragMode.None)
        {
            ApplyWorldState();
        }
    }

    private void Update()
    {
        if (dragMode == DragMode.None || activePointer == null)
        {
            return;
        }

        UpdateDraggedPosition(Time.deltaTime);
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        bool canDrag = SupportsDrag && TransferController;
        actions.Add(new InteractionAction(this, InteractionMode.Drag, dragLabel, dragGlyphId, canDrag, requiresApproach: false));
        if (!string.IsNullOrWhiteSpace(GetInspectText()))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        switch (action.Mode)
        {
            case InteractionMode.Drag:
                if (context.Pointer == null)
                {
                    return false;
                }

                BeginDrag(context.Pointer);
                return IsDragging;

            case InteractionMode.Inspect:
                string inspect = GetInspectText();
                if (string.IsNullOrWhiteSpace(inspect))
                {
                    return false;
                }

                InteractionFeedback.Show(inspect, this);
                return true;
        }

        return false;
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return pointer && SupportsDrag && TransferController && dragMode == DragMode.None;
    }

    public void BeginDrag(PointerContext pointer)
    {
        if (!CanStartDrag(pointer) || !TransferController.TryBeginWorldTransfer(this))
        {
            return;
        }

        StartDrag(pointer, DragMode.World, keepGrabOffset);
    }

    public void EndDrag()
    {
        switch (dragMode)
        {
            case DragMode.World:
                TransferController?.EndWorldTransfer();
                break;

            case DragMode.InventoryPreview:
                TransferController?.EndPlacementDrag(Vector2.zero);
                break;
        }
    }

    public void ConfigureFromInventory(InventoryItemDefinition definition, int entryQuantity)
    {
        itemDefinition = definition;
        quantity = Mathf.Max(1, entryQuantity);
    }

    public void BindTransferRoot(Transform root)
    {
        transferRoot = root ? root : transform;
        resolvedBoundsConstraint = transferRoot.GetComponent<RoomBoundsConstraint>();
        ClearCachedComponents();
    }

    public bool BeginInventoryPlacement(PointerContext pointer)
    {
        if (!pointer || dragMode != DragMode.None)
        {
            return false;
        }

        SnapPreviewToPointer(pointer);
        StartDrag(pointer, DragMode.InventoryPreview, preserveGrabOffset: false);
        return true;
    }

    public bool CanPlaceAtCurrentPosition()
    {
        return !IntersectsPlayer(lastValidPosition) && IsPlacementValid(lastValidPosition);
    }

    public bool TryGetCommittedPose(out Vector3 worldPosition, out Quaternion worldRotation)
    {
        worldPosition = GetResolvedReleasePosition(lastValidPosition);
        worldRotation = startRotation;
        return true;
    }

    public void CompleteTransfer(Vector3 worldPosition, Quaternion worldRotation, Transform parent = null)
    {
        StopDragging();
        ExitTransferMode(worldPosition, worldRotation, parent);
    }

    public void CancelTransfer(Vector3 worldPosition, Quaternion worldRotation, Transform parent = null)
    {
        StopDragging();
        ExitTransferMode(worldPosition, worldRotation, parent);
    }

    public void DestroyTransferRoot()
    {
        StopDragging();
        if (TransferRoot)
        {
            Destroy(TransferRoot.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void StartDrag(PointerContext pointer, DragMode mode, bool preserveGrabOffset)
    {
        activePointer = pointer;
        dragMode = mode;
        startPosition = RootPosition;
        lastValidPosition = RootPosition;
        startRotation = RootRotation;
        dragBaseHeight = RootPosition.y;
        dragVelocity = Vector3.zero;
        dragOffset = Vector3.zero;
        EnterTransferMode();
        if (preserveGrabOffset && activePointer.TryGetDragPoint(dragBaseHeight, liftHeight, out Vector3 point))
        {
            dragOffset = DragHandle.position - point;
            dragOffset.y = 0f;
        }

        UpdateDraggedPosition(0f);
    }

    private void StopDragging()
    {
        activePointer = null;
        dragMode = DragMode.None;
        dragVelocity = Vector3.zero;
    }

    private void UpdateDraggedPosition(float deltaTime)
    {
        if (!activePointer.TryGetDragPoint(dragBaseHeight, liftHeight, out Vector3 candidate))
        {
            return;
        }

        candidate += dragOffset;
        candidate.y = dragBaseHeight + liftHeight;
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

        Vector3 next = dragSmoothTime <= 0f || deltaTime <= 0f
            ? candidate
            : Vector3.SmoothDamp(RootPosition, candidate, ref dragVelocity, dragSmoothTime, Mathf.Infinity, deltaTime);
        TransferRoot.position = next;
    }

    private bool TryResolvePlayerOverlap(Vector3 candidate)
    {
        if (!Player)
        {
            return true;
        }

        Bounds bounds = GetWorldBounds(candidate);
        if (!bounds.Intersects(Player.GetWorldBounds(playerClearance)))
        {
            return true;
        }

        if (!Player.RequestSmoothClearance(bounds, playerClearance))
        {
            return false;
        }

        return !IntersectsPlayer(candidate);
    }

    private bool IntersectsPlayer(Vector3 candidate)
    {
        return Player && GetWorldBounds(candidate).Intersects(Player.GetWorldBounds(playerClearance));
    }

    private bool IsPlacementValid(Vector3 candidate)
    {
        Bounds bounds = GetWorldBounds(candidate);
        return !WorldDragUtility.IsBlocked(bounds, RootRotation, blockingLayers, Colliders, SupportSurfaceTolerance);
    }

    public Bounds GetWorldBounds(Vector3 worldPosition)
    {
        return WorldDragUtility.GetWorldBounds(Renderers, Colliders, RootPosition, worldPosition);
    }

    public void EnterTransferMode()
    {
        CacheWorldState();

        for (int i = 0; i < Targets.Length; i++)
        {
            if (Targets[i])
            {
                Targets[i].enabled = false;
            }
        }

        for (int i = 0; i < Colliders.Length; i++)
        {
            if (Colliders[i])
            {
                Colliders[i].enabled = false;
            }
        }

        for (int i = 0; i < Bodies.Length; i++)
        {
            Rigidbody body = Bodies[i];
            if (!body)
            {
                continue;
            }

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
            body.constraints = GetManagedConstraints();
        }

        for (int i = 0; i < Obstacles.Length; i++)
        {
            if (Obstacles[i])
            {
                Obstacles[i].enabled = false;
            }
        }
    }

    public void ExitTransferMode(Vector3 worldPosition, Quaternion worldRotation, Transform parent = null)
    {
        if (parent)
        {
            TransferRoot.SetParent(parent, true);
        }

        TransferRoot.SetPositionAndRotation(worldPosition, worldRotation);
        lastValidPosition = worldPosition;
        startPosition = worldPosition;
        startRotation = worldRotation;
        RestoreWorldState();
    }

    private string GetInspectText()
    {
        if (!string.IsNullOrWhiteSpace(inspectText))
        {
            return inspectText;
        }

        return itemDefinition ? itemDefinition.Description : string.Empty;
    }

    private void CacheWorldState()
    {
        if (cachedWorldState)
        {
            return;
        }

        colliderEnabled = new bool[Colliders.Length];
        for (int i = 0; i < Colliders.Length; i++)
        {
            colliderEnabled[i] = Colliders[i] && Colliders[i].enabled;
        }

        bodyUseGravity = new bool[Bodies.Length];
        bodyKinematic = new bool[Bodies.Length];
        bodyDetectCollisions = new bool[Bodies.Length];
        bodyConstraints = new RigidbodyConstraints[Bodies.Length];
        bodyInterpolation = new RigidbodyInterpolation[Bodies.Length];
        bodyCollisionDetection = new CollisionDetectionMode[Bodies.Length];
        for (int i = 0; i < Bodies.Length; i++)
        {
            Rigidbody body = Bodies[i];
            if (!body)
            {
                continue;
            }

            bodyUseGravity[i] = body.useGravity;
            bodyKinematic[i] = body.isKinematic;
            bodyDetectCollisions[i] = body.detectCollisions;
            bodyConstraints[i] = body.constraints;
            bodyInterpolation[i] = body.interpolation;
            bodyCollisionDetection[i] = body.collisionDetectionMode;
        }

        obstacleEnabled = new bool[Obstacles.Length];
        for (int i = 0; i < Obstacles.Length; i++)
        {
            obstacleEnabled[i] = Obstacles[i] && Obstacles[i].enabled;
        }

        targetEnabled = new bool[Targets.Length];
        for (int i = 0; i < Targets.Length; i++)
        {
            targetEnabled[i] = Targets[i] && Targets[i].enabled;
        }

        cachedWorldState = true;
    }

    private void ApplyWorldState()
    {
        for (int i = 0; i < Bodies.Length; i++)
        {
            Rigidbody body = Bodies[i];
            if (!body)
            {
                continue;
            }

            bool isKinematic = bodyKinematic != null && i < bodyKinematic.Length && bodyKinematic[i];
            body.useGravity = bodyUseGravity != null && i < bodyUseGravity.Length && bodyUseGravity[i];
            body.isKinematic = isKinematic;
            body.detectCollisions = bodyDetectCollisions != null && i < bodyDetectCollisions.Length && bodyDetectCollisions[i];
            body.interpolation = bodyInterpolation != null && i < bodyInterpolation.Length ? bodyInterpolation[i] : RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = bodyCollisionDetection != null && i < bodyCollisionDetection.Length
                ? bodyCollisionDetection[i]
                : CollisionDetectionMode.Discrete;
            body.constraints = (bodyConstraints != null && i < bodyConstraints.Length ? bodyConstraints[i] : RigidbodyConstraints.None) | GetManagedConstraints();
            if (!isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }

    private void RestoreWorldState()
    {
        StopDragging();
        CacheWorldState();

        for (int i = 0; i < Targets.Length; i++)
        {
            if (Targets[i])
            {
                Targets[i].enabled = targetEnabled[i];
            }
        }

        ApplyWorldState();

        for (int i = 0; i < Colliders.Length; i++)
        {
            if (Colliders[i])
            {
                Colliders[i].enabled = colliderEnabled[i];
            }
        }

        for (int i = 0; i < Obstacles.Length; i++)
        {
            if (Obstacles[i])
            {
                Obstacles[i].enabled = obstacleEnabled[i];
            }
        }

        Physics.SyncTransforms();
    }

    private RigidbodyConstraints GetManagedConstraints()
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

    private Transform ResolveTransferRoot()
    {
        Rigidbody parentBody = GetComponentInParent<Rigidbody>(true);
        return parentBody ? parentBody.transform : transform;
    }

    private void ClearCachedComponents()
    {
        colliders = null;
        bodies = null;
        renderers = null;
        obstacles = null;
        targets = null;
        colliderEnabled = null;
        bodyUseGravity = null;
        bodyKinematic = null;
        bodyDetectCollisions = null;
        bodyConstraints = null;
        bodyInterpolation = null;
        bodyCollisionDetection = null;
        obstacleEnabled = null;
        targetEnabled = null;
        cachedWorldState = false;
    }

    private void SnapPreviewToPointer(PointerContext pointer)
    {
        if (!pointer.TryGetDragPoint(TransferRoot.position.y, liftHeight, out Vector3 point))
        {
            return;
        }

        if (lockZPosition)
        {
            point.z = TransferRoot.position.z;
        }

        if (BoundsConstraint)
        {
            point = BoundsConstraint.Clamp(point);
        }

        TransferRoot.position = point;
    }

    private Vector3 GetResolvedReleasePosition(Vector3 liftedPosition)
    {
        Bounds bounds = GetWorldBounds(liftedPosition);
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
