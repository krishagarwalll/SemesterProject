using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InteractionTarget))]
public class PhysicsItem : MonoBehaviour, IInteractionHandler, IWorldDraggable
{
    [Header("Type")]
    [SerializeField] private PhysicsItemMode mode = PhysicsItemMode.RoomProp;
    [SerializeField] private Room room;

    [Header("References")]
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform dragHandle;
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    [Header("Drag")]
    [SerializeField] private bool canDrag = true;
    [SerializeField] private bool keepGrabOffset = true;
    [SerializeField, Min(0f)] private float springForce = 120f;
    [SerializeField, Min(0f)] private float dragDamping = 20f;
    [SerializeField, Min(0f)] private float maxForce = 250f;
    [SerializeField, Min(0f)] private float maxLiftHeight;
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
    [SerializeField] private bool disableGameObjectOnStore = true;
    [SerializeField] private bool destroyOnStore;

    private PointerContext activePointer;
    private Collider[] itemColliders;
    private Collider[] playerColliders;
    private Vector3 dragOffset;
    private float dragBaseHeight;
    private float startZPosition;
    private RigidbodyConstraints baseConstraints;
    private bool cachedBaseConstraints;

    public bool SupportsDrag => canDrag && !CanStore;
    public bool IsDragging => activePointer != null;

    private Rigidbody Body => this.ResolveComponent(ref body);
    private Transform DragHandle => dragHandle ? dragHandle : transform;
    private NavMeshObstacle Obstacle => navMeshObstacle ? navMeshObstacle : navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
    private Collider[] ItemColliders => itemColliders ??= GetComponentsInChildren<Collider>(true);
    private Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    private bool CanStore => mode == PhysicsItemMode.WorldItem && inventoryItemDefinition && inventoryQuantity > 0;

    private void Reset()
    {
        this.ResolveComponent(ref body);
        dragHandle = transform;
        navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
        room = GetComponentInParent<Room>(true);
    }

    private void Awake()
    {
        SyncState(captureStartState: true, captureBaseConstraints: true);
    }

    private void OnEnable()
    {
        SyncState(captureStartState: true, captureBaseConstraints: true);
    }

    private void OnDisable()
    {
        SetPlayerCollisionIgnored(false);
        activePointer = null;
    }

    private void OnValidate()
    {
        springForce = Mathf.Max(0f, springForce);
        dragDamping = Mathf.Max(0f, dragDamping);
        maxForce = Mathf.Max(0f, maxForce);
        maxLiftHeight = Mathf.Max(0f, maxLiftHeight);
        inventoryQuantity = Mathf.Max(1, inventoryQuantity);
        if (mode == PhysicsItemMode.WorldItem)
        {
            canDrag = false;
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

        CaptureBaseConstraints();
        SyncObstacleState();
        SyncConstraints();
    }

    private void FixedUpdate()
    {
        if (!IsDragging || activePointer == null || !TryGetDragPoint(out Vector3 targetPoint))
        {
            return;
        }

        Vector3 handlePosition = DragHandle.position;
        Vector3 force = (targetPoint - handlePosition) * springForce - Body.GetPointVelocity(handlePosition) * dragDamping;
        if (maxForce > 0f)
        {
            force = Vector3.ClampMagnitude(force, maxForce);
        }

        Body.AddForceAtPosition(force, handlePosition, ForceMode.Force);
        Body.WakeUp();
    }

    public bool Supports(InteractionMode mode)
    {
        return mode switch
        {
            InteractionMode.Primary => CanStore,
            InteractionMode.Inspect => HasInspectText(),
            _ => false
        };
    }

    public bool CanInteract(in InteractionRequest request)
    {
        return request.Mode switch
        {
            InteractionMode.Primary => request.Inventory && CanStore && CanStoreIn(request.Inventory),
            InteractionMode.Inspect => HasInspectText(),
            _ => false
        };
    }

    public void Interact(in InteractionRequest request)
    {
        switch (request.Mode)
        {
            case InteractionMode.Primary:
                Store(request.Inventory);
                break;

            case InteractionMode.Inspect:
                InteractionFeedback.Show(GetInspectText(), this);
                break;
        }
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return SupportsDrag && pointer && enabled && gameObject.activeInHierarchy && Body;
    }

    public void BeginDrag(PointerContext pointer)
    {
        if (!CanStartDrag(pointer))
        {
            return;
        }

        activePointer = pointer;
        dragBaseHeight = DragHandle.position.y;
        dragOffset = TryGetPointerPoint(out Vector3 point) && keepGrabOffset ? DragHandle.position - point : Vector3.zero;

        SyncPlayerCollisionState();
        SyncObstacleState();
        SyncConstraints();
        Body.WakeUp();
    }

    public void EndDrag()
    {
        if (!IsDragging)
        {
            return;
        }

        activePointer = null;
        SyncPlayerCollisionState();
        SyncObstacleState();
        SyncConstraints();
    }

    private bool TryGetPointerPoint(out Vector3 point)
    {
        point = default;
        return activePointer && activePointer.TryGetDragPoint(dragBaseHeight, maxLiftHeight, out point);
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

        if (OwnerRoom)
        {
            point = OwnerRoom.ClampPoint(point, lockZPosition ? startZPosition : point.z);
        }

        return true;
    }

    private void Store(Inventory targetInventory)
    {
        if (!targetInventory || !CanStore || !CanStoreIn(targetInventory) || !targetInventory.TryAdd(inventoryItemDefinition, inventoryQuantity))
        {
            return;
        }

        CompleteStore();
    }

    private void CompleteStore()
    {
        if (destroyOnStore)
        {
            Destroy(gameObject);
            return;
        }

        if (disableGameObjectOnStore)
        {
            gameObject.SetActive(false);
            return;
        }

        activePointer = null;
        SyncState(captureStartState: false, captureBaseConstraints: false);
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

    private void SyncState(bool captureStartState, bool captureBaseConstraints)
    {
        if (captureStartState)
        {
            startZPosition = transform.position.z;
        }

        if (captureBaseConstraints)
        {
            CaptureBaseConstraints();
        }

        SyncPlayerCollisionState();
        SyncObstacleState();
        SyncConstraints();
    }

    private void CaptureBaseConstraints()
    {
        if (!Body)
        {
            return;
        }

        baseConstraints = Body.constraints & ~GetManagedConstraints(includeDragRotation: true);
        cachedBaseConstraints = true;
    }

    private void SyncObstacleState()
    {
        if (!Obstacle)
        {
            return;
        }

        bool obstacleEnabled = blocksNavigation && !IsDragging;
        Obstacle.enabled = obstacleEnabled;
        Obstacle.carving = blocksNavigation;
        Obstacle.carveOnlyStationary = true;
    }

    private void SyncPlayerCollisionState()
    {
        SetPlayerCollisionIgnored(ShouldIgnorePlayerCollision);
    }

    private void SyncConstraints()
    {
        if (!Body)
        {
            return;
        }

        if (!cachedBaseConstraints)
        {
            CaptureBaseConstraints();
        }

        Body.constraints = baseConstraints | GetManagedConstraints(includeDragRotation: IsDragging && freezeRotationWhileDragging);
        if (!lockZPosition || Mathf.Approximately(Body.position.z, startZPosition))
        {
            return;
        }

        Vector3 position = Body.position;
        position.z = startZPosition;
        Body.position = position;
    }

    private void SetPlayerCollisionIgnored(bool ignored)
    {
        Collider[] colliders = GetPlayerColliders();
        if (colliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < ItemColliders.Length; i++)
        {
            Collider itemCollider = ItemColliders[i];
            if (!itemCollider)
            {
                continue;
            }

            for (int j = 0; j < colliders.Length; j++)
            {
                Collider playerCollider = colliders[j];
                if (playerCollider)
                {
                    Physics.IgnoreCollision(itemCollider, playerCollider, ignored);
                }
            }
        }
    }

    private Collider[] GetPlayerColliders()
    {
        if (playerColliders != null && playerColliders.Length > 0)
        {
            return playerColliders;
        }

        PointClickController player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
        playerColliders = player ? player.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
        return playerColliders;
    }

    private bool CanStoreIn(Inventory targetInventory)
    {
        return targetInventory && (targetInventory.Contains(inventoryItemDefinition) || !targetInventory.IsFull);
    }

    private bool ShouldIgnorePlayerCollision => IsDragging || !blocksNavigation;

    private RigidbodyConstraints GetManagedConstraints(bool includeDragRotation)
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

        if (includeDragRotation)
        {
            constraints |= RigidbodyConstraints.FreezeRotation;
        }

        return constraints;
    }
}

public enum PhysicsItemMode
{
    RoomProp = 0,
    WorldItem = 1,
    ObstacleProp = 2
}
