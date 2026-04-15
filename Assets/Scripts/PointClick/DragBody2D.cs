using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DragBody2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Room room;
    [SerializeField] private bool useGrabOffset;
    [SerializeField] private bool centerOnPointer = true;
    [SerializeField, Min(0f)] private float idleGravityScale = 1f;
    [SerializeField, Min(0f)] private float dragGravityScale = 0f;
    [SerializeField, Min(0f)] private float idleLinearDamping;
    [SerializeField, Min(0f)] private float dragLinearDamping = 6f;
    [SerializeField, Min(0f)] private float dragResponsiveness = 12f;
    [SerializeField, Min(0f)] private float maxDragSpeed = 14f;
    [SerializeField] private bool enforceRoomBoundsWhileIdle = true;
    [SerializeField] private bool freezeRotationWhileIdle = true;
    [SerializeField] private bool freezeRotationWhileDragging = true;

    private Collider2D[] colliders2D;
    private PointerContext activePointer;
    private Vector3 dragOffset;
    private Vector2 lastValidPosition;
    private float lastValidRotation;
    private Vector2 screenOverride;
    private bool hasLastValidPose;
    private bool isDragging;
    private bool useScreenOverride;

    public Transform RootTransform => rootTransform ? rootTransform : rootTransform = transform;
    public Vector3 RootPosition => RootTransform.position;
    public Quaternion RootRotation => RootTransform.rotation;
    public Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    public bool IsDragging => isDragging;

    private Rigidbody2D Body => body ? body : body = GetComponent<Rigidbody2D>() ?? gameObject.GetOrAddComponent<Rigidbody2D>();
    private Collider2D[] Colliders2D => colliders2D is { Length: > 0 } ? colliders2D : colliders2D = ResolveColliders2D();

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>() ?? gameObject.GetOrAddComponent<Rigidbody2D>();
        rootTransform = transform;
        room = GetComponentInParent<Room>(true);
        EnsureBodyDefaults();
        EnsureCollider2D();
    }

    private void Awake()
    {
        EnsureCollider2D();
        DisableLegacy3DPhysics();
        EnsureBodyDefaults();
        Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        RecordValidPose();
    }

    private void OnValidate()
    {
        idleGravityScale = Mathf.Max(0f, idleGravityScale);
        idleLinearDamping = Mathf.Max(0f, idleLinearDamping);
        dragLinearDamping = Mathf.Max(0f, dragLinearDamping);
        dragResponsiveness = Mathf.Max(0f, dragResponsiveness);
        maxDragSpeed = Mathf.Max(0f, maxDragSpeed);
        if (!rootTransform)
        {
            rootTransform = transform;
        }

        if (!room)
        {
            room = GetComponentInParent<Room>(true);
        }

        if (body)
        {
            EnsureBodyDefaults();
        }
    }

    private void FixedUpdate()
    {
        if (!isDragging || !activePointer)
        {
            if (enforceRoomBoundsWhileIdle)
            {
                EnforceIdleRoomBounds();
            }

            RecordValidPose();
            return;
        }

        MoveToPointer(useScreenOverride ? screenOverride : activePointer.ScreenPosition);
    }

    public void SetOwnerRoom(Room ownerRoom)
    {
        room = ownerRoom;
        if (enforceRoomBoundsWhileIdle)
        {
            EnforceIdleRoomBounds();
            RecordValidPose();
        }
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return pointer && enabled && gameObject.activeInHierarchy;
    }

    public bool BeginDrag(PointerContext pointer)
    {
        return BeginDrag(pointer, pointer ? pointer.ScreenPosition : Vector2.zero);
    }

    public bool BeginDrag(PointerContext pointer, Vector2 screenPosition)
    {
        if (!CanStartDrag(pointer))
        {
            return false;
        }

        EnsureCollider2D();
        activePointer = pointer;
        screenOverride = screenPosition;
        useScreenOverride = true;
        RecordValidPose();
        dragOffset = Vector3.zero;
        if (!centerOnPointer && useGrabOffset && TryGetPointerPoint(screenPosition, out Vector3 point))
        {
            dragOffset = RootPosition - point;
            dragOffset.z = 0f;
        }

        ApplyDragBodyState(true);
        if (TryResolveDragPosition(screenPosition, out Vector2 dragStartPosition))
        {
            ApplyMove(dragStartPosition, RootRotation.eulerAngles.z);
        }

        Body.linearVelocity = Vector2.zero;
        Body.WakeUp();
        isDragging = true;
        MoveToPointer(screenPosition);
        return true;
    }

    public void UpdateDragScreen(Vector2 screenPosition)
    {
        screenOverride = screenPosition;
        useScreenOverride = true;
    }

    public void EndDrag(bool restoreInvalidPose)
    {
        isDragging = false;
        activePointer = null;
        useScreenOverride = false;
        bool shouldRestore = restoreInvalidPose && !CanPlace() && hasLastValidPose;
        if (shouldRestore)
        {
            RestoreLastValidPose();
        }

        ApplyDragBodyState(false);
        if (enforceRoomBoundsWhileIdle)
        {
            EnforceIdleRoomBounds();
        }

        RecordValidPose();
    }

    public bool CanPlace()
    {
        return IsPoseValid(Body.position);
    }

    public bool TryGetValidPose(out Vector3 worldPosition, out Quaternion worldRotation)
    {
        if (CanPlace())
        {
            worldPosition = new Vector3(Body.position.x, Body.position.y, RootPosition.z);
            worldRotation = RootRotation;
            return true;
        }

        if (hasLastValidPose)
        {
            worldPosition = new Vector3(lastValidPosition.x, lastValidPosition.y, RootPosition.z);
            worldRotation = Quaternion.Euler(0f, 0f, lastValidRotation);
            return true;
        }

        worldPosition = default;
        worldRotation = default;
        return false;
    }

    public void RestoreLastValidPose()
    {
        if (!hasLastValidPose)
        {
            return;
        }

        ApplyMove(lastValidPosition, lastValidRotation);
    }

    public void SeedPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        ApplyMove(worldPosition, worldRotation.eulerAngles.z);
        RecordValidPose();
    }

    private void MoveToPointer(Vector2 pointerScreenPosition)
    {
        if (!TryResolveDragPosition(pointerScreenPosition, out Vector2 resolved))
        {
            return;
        }

        ApplyDragForce(resolved);
        if (IsPoseValid(Body.position))
        {
            RecordValidPose();
        }
    }

    private bool TryResolveDragPosition(Vector2 pointerScreenPosition, out Vector2 resolvedPosition)
    {
        resolvedPosition = default;
        if (!TryGetPointerPoint(pointerScreenPosition, out Vector3 point))
        {
            return false;
        }

        Vector3 pointerTarget = centerOnPointer ? point : point + dragOffset;
        Vector3 candidate = ClampToRoom(pointerTarget);
        resolvedPosition = new Vector2(candidate.x, candidate.y);
        return true;
    }

    private bool TryGetPointerPoint(Vector2 pointerScreenPosition, out Vector3 point)
    {
        point = default;
        if (!activePointer)
        {
            return false;
        }

        return activePointer.TryGetWorldPointAtDepth(pointerScreenPosition, RootPosition.z, out point);
    }

    private bool IsPoseValid(Vector2 position)
    {
        if (!OwnerRoom)
        {
            return true;
        }

        return OwnerRoom.ContainsBounds(GetWorldBounds(position));
    }

    private Bounds GetWorldBounds(Vector2 position)
    {
        Bounds bounds = new(new Vector3(position.x, position.y, RootPosition.z), Vector3.zero);
        bool initialized = false;
        Vector3 delta = new(position.x - RootPosition.x, position.y - RootPosition.y, 0f);
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            Collider2D collider = Colliders2D[i];
            if (!collider.IsUsable())
            {
                continue;
            }

            Bounds colliderBounds = collider.bounds;
            colliderBounds.center += delta;
            if (!initialized)
            {
                bounds = colliderBounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(colliderBounds.min);
                bounds.Encapsulate(colliderBounds.max);
            }
        }

        return bounds;
    }

    private void RecordValidPose()
    {
        if (!IsPoseValid(Body.position))
        {
            return;
        }

        lastValidPosition = Body.position;
        lastValidRotation = RootRotation.eulerAngles.z;
        hasLastValidPose = true;
    }

    private void ApplyMove(Vector3 worldPosition, float rotationZ)
    {
        ApplyMove(new Vector2(worldPosition.x, worldPosition.y), rotationZ);
    }

    private void ApplyMove(Vector2 position, float rotationZ)
    {
        RootTransform.position = new Vector3(position.x, position.y, RootPosition.z);
        RootTransform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        Body.position = position;
        Body.rotation = rotationZ;
    }

    private Vector3 ClampToRoom(Vector3 worldPosition)
    {
        return OwnerRoom ? OwnerRoom.ClampPosition(worldPosition) : worldPosition;
    }

    private void ApplyDragBodyState(bool dragging)
    {
        if (!Body)
        {
            return;
        }

        Body.bodyType = RigidbodyType2D.Dynamic;
        Body.constraints = GetManagedConstraints(dragging);

        if (dragging)
        {
            Body.gravityScale = dragGravityScale;
            Body.linearDamping = dragLinearDamping;
            Body.angularVelocity = 0f;
            return;
        }

        Body.gravityScale = idleGravityScale;
        Body.linearDamping = idleLinearDamping;
    }

    private void ApplyDragForce(Vector2 resolvedTarget)
    {
        Vector2 centerOffset = Body.worldCenterOfMass - Body.position;
        Vector2 centerTarget = resolvedTarget + centerOffset;
        Vector2 displacement = centerTarget - Body.worldCenterOfMass;
        float fixedDeltaTime = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
        Vector2 desiredVelocity = displacement / fixedDeltaTime;
        if (dragResponsiveness > 0f)
        {
            desiredVelocity = Vector2.Lerp(Body.linearVelocity, desiredVelocity, 1f - Mathf.Exp(-dragResponsiveness * fixedDeltaTime));
        }

        if (maxDragSpeed > 0f)
        {
            desiredVelocity = Vector2.ClampMagnitude(desiredVelocity, maxDragSpeed);
        }

        Body.linearVelocity = desiredVelocity;
    }

    private Collider2D[] ResolveColliders2D()
    {
        Collider2D[] resolved = GetComponentsInChildren<Collider2D>(true);
        if (resolved.Length > 0)
        {
            return resolved;
        }

        EnsureCollider2D();
        return GetComponentsInChildren<Collider2D>(true);
    }

    private void EnsureCollider2D()
    {
        if (GetComponentsInChildren<Collider2D>(true).Length > 0)
        {
            return;
        }

        BoxCollider2D box = gameObject.GetOrAddComponent<BoxCollider2D>();
        Bounds bounds = GetFallbackBounds();
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        box.offset = new Vector2(localCenter.x, localCenter.y);
        box.size = new Vector2(Mathf.Max(0.1f, bounds.size.x), Mathf.Max(0.1f, bounds.size.y));
        colliders2D = new Collider2D[] { box };
    }

    private void EnsureBodyDefaults()
    {
        Rigidbody2D rigidbody2D = Body;
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        rigidbody2D.gravityScale = idleGravityScale;
        rigidbody2D.linearDamping = idleLinearDamping;
        rigidbody2D.constraints = GetManagedConstraints(dragging: false);
    }

    private RigidbodyConstraints2D GetManagedConstraints(bool dragging)
    {
        RigidbodyConstraints2D constraints = Body ? Body.constraints : RigidbodyConstraints2D.None;
        constraints &= ~(RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY);
        constraints &= ~RigidbodyConstraints2D.FreezeRotation;

        bool freezeRotation = dragging ? freezeRotationWhileDragging : freezeRotationWhileIdle;
        if (freezeRotation)
        {
            constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        return constraints;
    }

    private void EnforceIdleRoomBounds()
    {
        if (!Body || !OwnerRoom || IsPoseValid(Body.position))
        {
            return;
        }

        if (hasLastValidPose)
        {
            RestoreLastValidPose();
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
            return;
        }

        Vector3 clamped = ClampToRoom(new Vector3(Body.position.x, Body.position.y, RootPosition.z));
        ApplyMove(new Vector2(clamped.x, clamped.y), RootRotation.eulerAngles.z);
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
    }

    private void DisableLegacy3DPhysics()
    {
        Collider[] colliders3D = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders3D.Length; i++)
        {
            colliders3D[i].enabled = false;
        }

        Rigidbody[] rigidbodies3D = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies3D.Length; i++)
        {
            rigidbodies3D[i].isKinematic = true;
            rigidbodies3D[i].detectCollisions = false;
        }

        NavMeshObstacle[] obstacles = GetComponentsInChildren<NavMeshObstacle>(true);
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].enabled = false;
        }
    }

    private Bounds GetFallbackBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new(transform.position, Vector3.one);
        bool initialized = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i])
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderers[i].bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds.min);
                bounds.Encapsulate(renderers[i].bounds.max);
            }
        }

        return initialized ? bounds : new Bounds(transform.position, Vector3.one);
    }
}
