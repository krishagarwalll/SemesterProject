using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Obsolete("Legacy world drag/inventory item implementation. Sprint2 uses DragBody2D with PickupItem/WorldProp.", false)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InteractionTarget))]
public class PhysicsItem : MonoBehaviour, IInteractionHandler, IInteractionActionProvider, IWorldDraggable
{
    [Header("Type")]
    [SerializeField] private PhysicsItemMode mode = PhysicsItemMode.RoomProp;
    [SerializeField] private Room room;

    [Header("References")]
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform dragHandle;
    [SerializeField] private NavMeshObstacle navMeshObstacle;
    [SerializeField] private InventoryTransferController transferController;

    [Header("Labels")]
    [SerializeField] private string dragLabel = "Drag";
    [SerializeField] private string storeLabel = "Store";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string dragGlyphId = "Primary";
    [SerializeField] private string storeGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";

    [Header("Drag")]
    [SerializeField] private bool canDrag = true;
    [SerializeField] private bool keepGrabOffset = true;
    [SerializeField, Min(0f)] private float maxLiftHeight = 0.5f;
    [SerializeField] private bool freezeRotationWhileDragging = true;

    [Header("Movement Blocking")]
    [SerializeField] private bool blocksNavigation = true;

    [Header("Constraints")]
    [SerializeField] private bool lockZPosition = true;
    [SerializeField] private bool lockRotationX = true;
    [SerializeField] private bool lockRotationY = true;

    [Header("Inspect")]
    [SerializeField, TextArea] private string inspectText;

    [Header("Inventory")]
    [SerializeField] private InventoryItemDefinition inventoryItemDefinition;
    [SerializeField, Min(1)] private int inventoryQuantity = 1;
    [SerializeField] private bool destroyOnStore;

    private Collider[] itemColliders;
    private PointerContext activePointer;
    private Vector2 screenOverride;
    private Vector3 dragOffset;
    private float dragBaseHeight;
    private float startZPosition;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation = Quaternion.identity;
    private RigidbodyConstraints cachedConstraints;
    private bool cachedUseGravity;
    private bool cachedIsKinematic;
    private bool hasCachedBodyRuntimeState;
    private bool hasLastValidPose;
    private bool placementDragActive;
    private bool useScreenOverride;

    public PhysicsItemMode Mode => mode;
    public InventoryItemDefinition ItemDefinition => inventoryItemDefinition;
    public int Quantity => inventoryQuantity;
    public Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    public bool CanStore => mode == PhysicsItemMode.WorldItem && inventoryItemDefinition && inventoryQuantity > 0;
    public bool SupportsDrag => canDrag && enabled && gameObject.activeInHierarchy && Body;
    public bool IsDragging => activePointer;
    public bool IsPlacementPreview => placementDragActive;

    private Rigidbody Body => body ? body : body = GetComponent<Rigidbody>();
    private Transform DragHandle => dragHandle ? dragHandle : transform;
    private NavMeshObstacle Obstacle => navMeshObstacle ? navMeshObstacle : navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
    private InventoryTransferController TransferController => transferController ? transferController : transferController = FindFirstObjectByType<InventoryTransferController>(FindObjectsInactive.Include);
    private Collider[] ItemColliders => itemColliders ??= GetComponentsInChildren<Collider>(true);

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
        dragHandle = transform;
        navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
        room = GetComponentInParent<Room>(true);
    }

    private void Awake()
    {
        ApplyRuntimeSetup();
        CaptureState();
        RecordValidPose();
        SyncObstacleState();
    }

    private void OnEnable()
    {
        ApplyRuntimeSetup();
        CaptureState();
        RecordValidPose();
        SyncObstacleState();
    }

    private void OnDisable()
    {
        activePointer = null;
        placementDragActive = false;
        useScreenOverride = false;
        RestorePhysicsState();
        SyncObstacleState();
    }

    private void OnValidate()
    {
        inventoryQuantity = Mathf.Max(1, inventoryQuantity);
        maxLiftHeight = Mathf.Max(0f, maxLiftHeight);
        if (mode == PhysicsItemMode.WorldItem)
        {
            blocksNavigation = false;
        }

        if (!dragHandle)
        {
            dragHandle = transform;
        }

        if (!room)
        {
            room = GetComponentInParent<Room>(true);
        }

        ApplyRuntimeSetup();
    }

    private void FixedUpdate()
    {
        if (!Body)
        {
            return;
        }

        if (IsDragging)
        {
            if (TryGetDragPoint(out Vector3 targetPoint))
            {
                ApplyPose(targetPoint, Body.rotation, resetVelocity: true);
                RecordValidPose();
            }

            return;
        }

        EnforceRoomBounds();
        RecordValidPose();
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        if (SupportsDrag)
        {
            actions.Add(new InteractionAction(this, InteractionMode.Drag, dragLabel, dragGlyphId, requiresApproach: false));
        }

        if (CanStore && context.Inventory)
        {
            actions.Add(new InteractionAction(this, InteractionMode.Store, storeLabel, storeGlyphId, CanStoreIn(context.Inventory), requiresApproach: false));
        }

        if (HasInspectText())
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

            case InteractionMode.Store:
                return TryStoreDirect(context.Inventory);

            case InteractionMode.Inspect:
                if (!HasInspectText())
                {
                    return false;
                }

                InteractionFeedback.Show(GetInspectText(), this);
                return true;
        }

        return false;
    }

    public bool Supports(InteractionMode interactionMode)
    {
        return interactionMode switch
        {
            InteractionMode.Store => CanStore,
            InteractionMode.Inspect => HasInspectText(),
            _ => false
        };
    }

    public bool CanInteract(in InteractionRequest request)
    {
        return request.Mode switch
        {
            InteractionMode.Store => request.Inventory && CanStoreIn(request.Inventory),
            InteractionMode.Inspect => HasInspectText(),
            _ => false
        };
    }

    public void Interact(in InteractionRequest request)
    {
        switch (request.Mode)
        {
            case InteractionMode.Store:
                TryStoreDirect(request.Inventory);
                break;

            case InteractionMode.Inspect:
                if (HasInspectText())
                {
                    InteractionFeedback.Show(GetInspectText(), this);
                }
                break;
        }
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return pointer && SupportsDrag && !IsDragging;
    }

    public void BeginDrag(PointerContext pointer)
    {
        BeginDragInternal(pointer, allowStoreTransfer: !placementDragActive);
    }

    public void UpdateDrag(PointerContext pointer)
    {
        if (pointer)
        {
            screenOverride = pointer.ScreenPosition;
            useScreenOverride = true;
        }
    }

    public void EndDrag()
    {
        EndDragInternal(restoreInvalidPose: true);
    }

    public bool TryBeginPlacementFromInventory(PointerContext pointer, Vector2 screenPosition, Room placementRoom)
    {
        if (placementRoom)
        {
            room = placementRoom;
        }

        screenOverride = screenPosition;
        useScreenOverride = true;
        placementDragActive = true;
        return BeginDragInternal(pointer, allowStoreTransfer: false);
    }

    public void UpdatePlacementDrag(Vector2 screenPosition)
    {
        screenOverride = screenPosition;
        useScreenOverride = true;
    }

    public void FinishPlacementDrag(bool commit)
    {
        EndDragInternal(restoreInvalidPose: !commit);
        placementDragActive = false;
    }

    public void SuspendStoreTransfer()
    {
        EndDragInternal(restoreInvalidPose: true);
    }

    public void CancelStoreTransfer()
    {
        RestoreLastValidPose();
        placementDragActive = false;
    }

    public void CompleteStoreToInventory()
    {
        placementDragActive = false;
        CompleteStore();
    }

    public void ConfigureWorldItem(InventoryItemDefinition definition, int quantity, Room placementRoom)
    {
        mode = PhysicsItemMode.WorldItem;
        inventoryItemDefinition = definition;
        inventoryQuantity = Mathf.Max(1, quantity);
        room = placementRoom ? placementRoom : room;
        blocksNavigation = false;
        gameObject.SetActive(true);
        ApplyRuntimeSetup();
        CaptureState();
        RecordValidPose();
        SyncObstacleState();
    }

    public void SeedPlacementPose(Vector3 position, Quaternion rotation)
    {
        ApplyPose(position, rotation, resetVelocity: true);
        RecordValidPose();
    }

    public bool CanPlaceInRoom()
    {
        return IsCurrentPoseValid();
    }

    public bool TryGetInventoryEntry(out Inventory.Entry entry)
    {
        if (!CanStore)
        {
            entry = default;
            return false;
        }

        entry = new Inventory.Entry(inventoryItemDefinition, inventoryQuantity);
        return true;
    }

    public bool TryGetCurrentValidPose(out Vector3 position, out Quaternion rotation)
    {
        if (IsCurrentPoseValid())
        {
            position = Body ? Body.position : transform.position;
            rotation = Body ? Body.rotation : transform.rotation;
            return true;
        }

        if (hasLastValidPose)
        {
            position = lastValidPosition;
            rotation = lastValidRotation;
            return true;
        }

        position = default;
        rotation = default;
        return false;
    }

    public void RestoreLastValidPose()
    {
        if (!hasLastValidPose)
        {
            return;
        }

        ApplyPose(lastValidPosition, lastValidRotation, resetVelocity: true);
    }

    private bool BeginDragInternal(PointerContext pointer, bool allowStoreTransfer)
    {
        if (!CanStartDrag(pointer))
        {
            return false;
        }

        activePointer = pointer;
        dragBaseHeight = DragHandle.position.y;
        dragOffset = Vector3.zero;
        if (keepGrabOffset && TryGetPointerPoint(out Vector3 pointerPoint))
        {
            dragOffset = DragHandle.position - pointerPoint;
            dragOffset.z = 0f;
        }

        placementDragActive |= !allowStoreTransfer;
        CaptureBodyRuntimeState();
        Body.isKinematic = true;
        Body.useGravity = false;
        Body.constraints = BuildConstraints(freezeRotationWhileDragging);
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        SyncObstacleState();
        return true;
    }

    private void EndDragInternal(bool restoreInvalidPose)
    {
        bool validPose = IsCurrentPoseValid();
        activePointer = null;
        useScreenOverride = false;
        RestorePhysicsState();
        SyncObstacleState();

        if (restoreInvalidPose && !validPose)
        {
            RestoreLastValidPose();
        }
        else
        {
            EnforceRoomBounds();
        }
    }

    private void CaptureState()
    {
        startZPosition = transform.position.z;
        if (Body)
        {
            Body.constraints = BuildConstraints(freezeRotation: false);
        }
    }

    private void CaptureBodyRuntimeState()
    {
        cachedConstraints = Body.constraints;
        cachedUseGravity = Body.useGravity;
        cachedIsKinematic = Body.isKinematic;
        hasCachedBodyRuntimeState = true;
    }

    private void RestorePhysicsState()
    {
        if (!Body)
        {
            return;
        }

        if (!hasCachedBodyRuntimeState)
        {
            Body.isKinematic = false;
            Body.useGravity = true;
            Body.constraints = BuildConstraints(freezeRotation: false);
            return;
        }

        Body.isKinematic = cachedIsKinematic;
        Body.useGravity = cachedUseGravity;
        Body.constraints = cachedConstraints == RigidbodyConstraints.None ? BuildConstraints(freezeRotation: false) : cachedConstraints;
    }

    private RigidbodyConstraints BuildConstraints(bool freezeRotation)
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

        if (freezeRotation)
        {
            constraints |= RigidbodyConstraints.FreezeRotationZ;
        }

        return constraints;
    }

    private void ApplyPose(Vector3 position, Quaternion rotation, bool resetVelocity)
    {
        position = ClampToRoom(position);
        if (Body)
        {
            Body.position = position;
            Body.rotation = rotation;
            if (resetVelocity)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            Physics.SyncTransforms();
            return;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    private bool TryGetPointerPoint(out Vector3 point)
    {
        point = default;
        if (!activePointer)
        {
            return false;
        }

        if (useScreenOverride)
        {
            return activePointer.TryGetPointOnPlane(screenOverride, Vector3.up, new Vector3(0f, dragBaseHeight, 0f), out point);
        }

        return activePointer.TryGetDragPoint(dragBaseHeight, maxLiftHeight, out point);
    }

    private bool TryGetDragPoint(out Vector3 point)
    {
        point = default;
        if (!TryGetPointerPoint(out Vector3 rawPoint))
        {
            return false;
        }

        point = rawPoint + dragOffset;
        point.y = Mathf.Clamp(point.y, dragBaseHeight, dragBaseHeight + maxLiftHeight);
        if (lockZPosition)
        {
            point.z = startZPosition;
        }

        point = ClampToRoom(point);
        return float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
    }

    private void EnforceRoomBounds()
    {
        if (!Body || IsCurrentPoseValid())
        {
            return;
        }

        if (hasLastValidPose)
        {
            RestoreLastValidPose();
            return;
        }

        Vector3 clamped = ClampToRoom(Body.position);
        ApplyPose(clamped, Body.rotation, resetVelocity: true);
    }

    private Vector3 ClampToRoom(Vector3 point)
    {
        if (!OwnerRoom)
        {
            return point;
        }

        return OwnerRoom.ClampPoint(point, lockZPosition ? startZPosition : point.z);
    }

    private bool HasInspectText()
    {
        return !string.IsNullOrWhiteSpace(inspectText) || inventoryItemDefinition && !string.IsNullOrWhiteSpace(inventoryItemDefinition.Description);
    }

    private string GetInspectText()
    {
        if (!string.IsNullOrWhiteSpace(inspectText))
        {
            return inspectText;
        }

        if (inventoryItemDefinition && !string.IsNullOrWhiteSpace(inventoryItemDefinition.Description))
        {
            return inventoryItemDefinition.Description;
        }

        return inventoryItemDefinition ? inventoryItemDefinition.DisplayName : name;
    }

    private void RecordValidPose()
    {
        if (!Body || !IsCurrentPoseValid())
        {
            return;
        }

        lastValidPosition = Body.position;
        lastValidRotation = Body.rotation;
        hasLastValidPose = true;
    }

    private bool IsCurrentPoseValid()
    {
        if (!OwnerRoom)
        {
            return true;
        }

        return TryGetWorldBounds(out Bounds bounds)
            ? OwnerRoom.ContainsBounds(bounds)
            : OwnerRoom.ContainsPoint(Body ? Body.position : transform.position);
    }

    private bool TryGetWorldBounds(out Bounds bounds)
    {
        bounds = default;
        bool initialized = false;
        for (int i = 0; i < ItemColliders.Length; i++)
        {
            Collider itemCollider = ItemColliders[i];
            if (!itemCollider || !itemCollider.enabled)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = itemCollider.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(itemCollider.bounds.min);
                bounds.Encapsulate(itemCollider.bounds.max);
            }
        }

        return initialized;
    }

    private void SyncObstacleState()
    {
        if (!Obstacle)
        {
            return;
        }

        Obstacle.enabled = blocksNavigation && !IsDragging;
    }

    private bool CanStoreIn(Inventory targetInventory)
    {
        return targetInventory && (targetInventory.Contains(inventoryItemDefinition) || !targetInventory.IsFull);
    }

    private bool TryStoreDirect(Inventory targetInventory)
    {
        if (!targetInventory || !CanStore || !targetInventory.TryStoreAnywhere(inventoryItemDefinition, inventoryQuantity))
        {
            return false;
        }

        CompleteStore();
        return true;
    }

    private void CompleteStore()
    {
        activePointer = null;
        placementDragActive = false;
        useScreenOverride = false;

        if (destroyOnStore)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }

    private void ApplyRuntimeSetup()
    {
        if (!Body)
        {
            return;
        }

        if (mode == PhysicsItemMode.WorldItem)
        {
            blocksNavigation = false;
        }

        Body.useGravity = !IsDragging;
        Body.isKinematic = IsDragging;
        Body.constraints = BuildConstraints(IsDragging && freezeRotationWhileDragging);
    }
}

public enum PhysicsItemMode
{
    RoomProp = 0,
    WorldItem = 1,
    ObstacleProp = 2
}
