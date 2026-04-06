using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InteractionTarget))]
public class PhysicsItem : MonoBehaviour, IWorldInteractable, IWorldDraggable
{
    private const int MaxPlayerResolveIterations = 6;
    private const float PlayerClearance = 0.1f;
    private const float MaxPlayerResolveStep = 0.75f;
    private const float DistanceEpsilon = 0.0001f;

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
    [SerializeField] private bool disableObstacleWhileDragging = true;

    [Header("Constraints")]
    [SerializeField] private bool lockZPosition;
    [SerializeField] private bool lockRotationX = true;
    [SerializeField] private bool lockRotationY = true;

    [Header("Inventory")]
    [SerializeField] private InventoryItemDefinition inventoryItemDefinition;
    [SerializeField, Min(1)] private int inventoryQuantity = 1;
    [SerializeField] private bool disableGameObjectOnStore = true;
    [SerializeField] private bool destroyOnStore;

    private PointerContext activePointer;
    private Inventory inventory;
    private PointClickController sceneController;
    private Collider[] itemColliders;
    private Collider[] playerColliders;
    private Vector3 dragOffset;
    private float dragBaseHeight;
    private float startZPosition;
    private RigidbodyConstraints baseConstraints;
    private bool cachedBaseConstraints;
    private bool obstacleWasEnabled;

    public bool IsDragging => activePointer != null;

    private Rigidbody Body => this.ResolveComponent(ref body);
    private Transform DragHandle => dragHandle ? dragHandle : transform;
    private NavMeshObstacle Obstacle => navMeshObstacle ? navMeshObstacle : navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
    private Inventory SceneInventory => this.ResolveSceneComponent(ref inventory);
    private PointClickController SceneController => this.ResolveSceneComponent(ref sceneController);
    private Collider[] ItemColliders => itemColliders ??= GetComponentsInChildren<Collider>(true);
    private Collider[] PlayerColliders => playerColliders ??= SceneController ? SceneController.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
    private bool CanStore => inventoryItemDefinition && inventoryQuantity > 0;

    private void Reset()
    {
        this.ResolveComponent(ref body);
        dragHandle = transform;
        navMeshObstacle = GetComponentInChildren<NavMeshObstacle>(true);
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
        if (IsDragging)
        {
            EndDrag();
        }
    }

    private void OnValidate()
    {
        springForce = Mathf.Max(0f, springForce);
        dragDamping = Mathf.Max(0f, dragDamping);
        maxForce = Mathf.Max(0f, maxForce);
        maxLiftHeight = Mathf.Max(0f, maxLiftHeight);
        inventoryQuantity = Mathf.Max(1, inventoryQuantity);
        if (!Body)
        {
            return;
        }

        if (!dragHandle)
        {
            dragHandle = transform;
        }

        CaptureBaseConstraints();
        SyncObstacleState();
        SyncConstraints();
    }

    private void FixedUpdate()
    {
        if (!IsDragging || activePointer == null || !activePointer.TryGetDragPoint(dragBaseHeight, maxLiftHeight, out Vector3 point))
        {
            return;
        }

        ResolvePlayerOverlap();

        Vector3 handlePosition = DragHandle.position;
        Vector3 targetPoint = point + dragOffset;
        targetPoint.y = Mathf.Clamp(targetPoint.y, dragBaseHeight, dragBaseHeight + maxLiftHeight);
        if (lockZPosition)
        {
            targetPoint.z = startZPosition;
        }

        Vector3 force = (targetPoint - handlePosition) * springForce - Body.GetPointVelocity(handlePosition) * dragDamping;
        if (maxForce > 0f)
        {
            force = Vector3.ClampMagnitude(force, maxForce);
        }

        Body.AddForceAtPosition(force, handlePosition, ForceMode.Force);
        Body.WakeUp();
    }

    public bool CanInteract(PointClickController controller)
    {
        return SceneInventory && CanStore;
    }

    public void Interact(PointClickController controller)
    {
        if (!CanInteract(controller))
        {
            return;
        }

        if (IsDragging)
        {
            EndDrag();
        }

        if (!SceneInventory.TryAdd(inventoryItemDefinition, inventoryQuantity))
        {
            return;
        }

        if (destroyOnStore)
        {
            Destroy(gameObject);
            return;
        }

        if (disableGameObjectOnStore)
        {
            gameObject.SetActive(false);
        }
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return canDrag && pointer && Body && enabled && gameObject.activeInHierarchy;
    }

    public void BeginDrag(PointerContext pointer)
    {
        if (!CanStartDrag(pointer) || !pointer.TryGetDragPoint(DragHandle.position.y, maxLiftHeight, out Vector3 point))
        {
            return;
        }

        activePointer = pointer;
        dragBaseHeight = DragHandle.position.y;
        dragOffset = keepGrabOffset ? DragHandle.position - point : Vector3.zero;
        obstacleWasEnabled = Obstacle && Obstacle.enabled;

        if (Obstacle && disableObstacleWhileDragging)
        {
            Obstacle.enabled = false;
        }

        SyncConstraints();
        Body.WakeUp();
    }

    public void EndDrag()
    {
        if (Obstacle && disableObstacleWhileDragging)
        {
            Obstacle.enabled = blocksNavigation && obstacleWasEnabled;
        }

        activePointer = null;
        SyncConstraints();
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
        if (Obstacle)
        {
            Obstacle.enabled = blocksNavigation;
            Obstacle.carving = blocksNavigation;
            Obstacle.carveOnlyStationary = true;
        }
    }

    private void ResolvePlayerOverlap()
    {
        PointClickController controller = SceneController;
        if (!controller || PlayerColliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < MaxPlayerResolveIterations; i++)
        {
            Vector3 displacement = GetPlayerDisplacement();
            if (displacement.sqrMagnitude <= DistanceEpsilon)
            {
                return;
            }

            if (!controller.TryDisplace(Vector3.ClampMagnitude(displacement, MaxPlayerResolveStep)))
            {
                return;
            }
        }
    }

    private Vector3 GetPlayerDisplacement()
    {
        Vector3 displacement = Vector3.zero;
        for (int i = 0; i < ItemColliders.Length; i++)
        {
            Collider itemCollider = ItemColliders[i];
            if (!itemCollider)
            {
                continue;
            }

            for (int j = 0; j < PlayerColliders.Length; j++)
            {
                Collider playerCollider = PlayerColliders[j];
                if (!playerCollider)
                {
                    continue;
                }

                if (!Physics.ComputePenetration(
                        itemCollider,
                        itemCollider.transform.position,
                        itemCollider.transform.rotation,
                        playerCollider,
                        playerCollider.transform.position,
                        playerCollider.transform.rotation,
                        out Vector3 direction,
                        out float distance))
                {
                    displacement += GetPlayerClearanceOffset(itemCollider, playerCollider);
                    continue;
                }

                Vector3 separation = -direction * (distance + PlayerClearance);
                separation.y = 0f;
                displacement += separation;
            }
        }

        return displacement;
    }

    private static Vector3 GetPlayerClearanceOffset(Collider itemCollider, Collider playerCollider)
    {
        Vector3 itemPoint = itemCollider.ClosestPoint(playerCollider.bounds.center);
        Vector3 playerPoint = playerCollider.ClosestPoint(itemPoint);
        Vector3 horizontalDelta = playerPoint - itemPoint;
        horizontalDelta.y = 0f;
        float distance = horizontalDelta.magnitude;
        if (distance >= PlayerClearance)
        {
            return Vector3.zero;
        }

        Vector3 direction = distance > DistanceEpsilon
            ? horizontalDelta / distance
            : (playerCollider.bounds.center - itemCollider.bounds.center).normalized;
        direction.y = 0f;
        if (direction.sqrMagnitude <= DistanceEpsilon)
        {
            direction = Vector3.right;
        }

        return direction.normalized * (PlayerClearance - distance);
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
