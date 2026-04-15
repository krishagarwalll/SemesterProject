using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DragBody2D : MonoBehaviour
{
    [FieldHeader("References")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform rootTransform;
    [SerializeField] private Room room;

    [FieldHeader("Pointer")]
    [SerializeField] private bool useGrabOffset;
    [SerializeField] private bool centerOnPointer = true;

    [FieldHeader("Idle Physics")]
    [SerializeField, Min(0f)] private float idleGravityScale = 1f;
    [SerializeField, Min(0f)] private float idleLinearDamping;

    [FieldHeader("Drag Physics")]
    [SerializeField, Min(0f)] private float dragGravityScale = 0f;
    [SerializeField, Min(0f)] private float dragLinearDamping = 6f;
    [SerializeField, Min(0f)] private float dragResponsiveness = 12f;
    [SerializeField, Min(0f)] private float maxDragAcceleration = 72f;
    [SerializeField, Min(0f)] private float dragVerticalDamping = 10f;
    [SerializeField, Min(0f)] private float maxDragSpeed = 14f;
    [SerializeField] private bool limitVerticalLift = true;
    [SerializeField, Min(0f), ConditionalField(nameof(limitVerticalLift))] private float maxLiftHeight = 1.35f;
    [SerializeField, Range(0f, 1f)] private float releaseVelocityRetention = 0.92f;

    [FieldHeader("Constraints")]
    [SerializeField] private bool enforceRoomBoundsWhileIdle = true;
    [SerializeField] private bool freezeRotationWhileIdle = true;
    [SerializeField] private bool freezeRotationWhileDragging = true;
    [SerializeField] private bool ignorePlayerCollisions = true;

    private Collider2D[] colliders2D;
    private PointerContext activePointer;
    private Vector3 dragOffset;
    private Vector2 lastValidPosition;
    private float lastValidRotation;
    private Vector2 screenOverride;
    private bool hasLastValidPose;
    private bool isDragging;
    private float dragBaseHeight;
    private bool activeLiftLimit;
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
        ApplyCollisionIgnores();
        Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        RecordValidPose();
    }

    private void OnEnable()
    {
        ApplyCollisionIgnores();
    }

    private void OnValidate()
    {
        idleGravityScale = Mathf.Max(0f, idleGravityScale);
        idleLinearDamping = Mathf.Max(0f, idleLinearDamping);
        dragLinearDamping = Mathf.Max(0f, dragLinearDamping);
        dragResponsiveness = Mathf.Max(0f, dragResponsiveness);
        maxDragAcceleration = Mathf.Max(0f, maxDragAcceleration);
        dragVerticalDamping = Mathf.Max(0f, dragVerticalDamping);
        maxDragSpeed = Mathf.Max(0f, maxDragSpeed);
        maxLiftHeight = Mathf.Max(0f, maxLiftHeight);
        releaseVelocityRetention = Mathf.Clamp01(releaseVelocityRetention);
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
        return BeginDrag(pointer, pointer ? pointer.ScreenPosition : Vector2.zero, limitVerticalLift);
    }

    public bool BeginDrag(PointerContext pointer, Vector2 screenPosition)
    {
        return BeginDrag(pointer, screenPosition, limitVerticalLift);
    }

    public bool BeginUnrestrictedDrag(PointerContext pointer, Vector2 screenPosition)
    {
        return BeginDrag(pointer, screenPosition, constrainLiftHeight: false);
    }

    public bool BeginDrag(PointerContext pointer, Vector2 screenPosition, bool constrainLiftHeight)
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
        dragBaseHeight = GetDragBaseHeight();
        activeLiftLimit = constrainLiftHeight;
        if (!centerOnPointer && useGrabOffset && TryGetPointerPoint(screenPosition, out Vector3 point))
        {
            dragOffset = RootPosition - point;
            dragOffset.z = 0f;
        }

        ApplyDragBodyState(true);
        Body.linearVelocity *= 0.2f;
        Body.angularVelocity = 0f;
        Body.WakeUp();
        isDragging = true;
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
        else if (Body)
        {
            Body.linearVelocity *= releaseVelocityRetention;
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

        if (!activePointer.TryGetWorldPointAtDepth(pointerScreenPosition, RootPosition.z, out point))
        {
            return false;
        }

        float liftLimit = GetEffectiveLiftHeight();
        if (activeLiftLimit && (liftLimit > 0f || dragBaseHeight > point.y))
        {
            point.y = Mathf.Clamp(point.y, dragBaseHeight, dragBaseHeight + liftLimit);
        }

        return true;
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
        if (!OwnerRoom)
        {
            return worldPosition;
        }

        Bounds candidateBounds = GetWorldBounds(new Vector2(worldPosition.x, worldPosition.y));
        return OwnerRoom.ClampBoundsCenter(candidateBounds, worldPosition.z);
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
        float mass = Mathf.Max(0.1f, Body.mass);
        float verticalMassScale = Mathf.Sqrt(mass);
        float horizontalResponsiveness = dragResponsiveness;
        float verticalResponsiveness = dragResponsiveness <= 0f ? 0f : dragResponsiveness / verticalMassScale;
        float horizontalMaxSpeed = maxDragSpeed;
        float verticalMaxSpeed = maxDragSpeed <= 0f ? 0f : maxDragSpeed / Mathf.Sqrt(Mathf.Max(1f, mass * 0.5f));
        float horizontalVelocityDelta = maxDragAcceleration <= 0f ? float.PositiveInfinity : maxDragAcceleration * fixedDeltaTime;
        float verticalVelocityDelta = maxDragAcceleration <= 0f ? float.PositiveInfinity : (maxDragAcceleration / mass) * fixedDeltaTime;

        Vector2 desiredVelocity = new(
            displacement.x * horizontalResponsiveness,
            displacement.y * verticalResponsiveness);

        if (horizontalMaxSpeed > 0f)
        {
            desiredVelocity.x = Mathf.Clamp(desiredVelocity.x, -horizontalMaxSpeed, horizontalMaxSpeed);
        }

        if (verticalMaxSpeed > 0f)
        {
            desiredVelocity.y = Mathf.Clamp(desiredVelocity.y, -verticalMaxSpeed, verticalMaxSpeed);
        }

        Vector2 velocity = Body.linearVelocity;
        velocity.x = MoveTowardsAxis(velocity.x, desiredVelocity.x, horizontalVelocityDelta);
        velocity.y = MoveTowardsAxis(velocity.y, desiredVelocity.y, verticalVelocityDelta);
        velocity.y *= 1f / (1f + dragVerticalDamping * fixedDeltaTime * verticalMassScale);
        Body.linearVelocity = velocity;
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

        Vector3 clamped = ClampToRoom(new Vector3(Body.position.x, Body.position.y, RootPosition.z));
        ApplyMove(new Vector2(clamped.x, clamped.y), RootRotation.eulerAngles.z);
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        if (IsPoseValid(Body.position))
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
    }

    private void ApplyCollisionIgnores()
    {
        if (!ignorePlayerCollisions)
        {
            return;
        }

        PointClickController actor = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
        if (!actor)
        {
            return;
        }

        Collider2D[] actorColliders = actor.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            Collider2D itemCollider = Colliders2D[i];
            if (!itemCollider || !itemCollider.enabled)
            {
                continue;
            }

            for (int j = 0; j < actorColliders.Length; j++)
            {
                Collider2D actorCollider = actorColliders[j];
                if (!actorCollider || !actorCollider.enabled)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(itemCollider, actorCollider, true);
            }
        }
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

    private float GetDragBaseHeight()
    {
        if (OwnerRoom)
        {
            return OwnerRoom.GroundY;
        }

        return RootPosition.y;
    }

    private float GetEffectiveLiftHeight()
    {
        float mass = Mathf.Max(0.1f, Body ? Body.mass : 1f);
        return maxLiftHeight <= 0f ? 0f : maxLiftHeight / Mathf.Sqrt(mass);
    }

    private static float MoveTowardsAxis(float current, float target, float maxDelta)
    {
        return float.IsFinite(maxDelta) ? Mathf.MoveTowards(current, target, maxDelta) : target;
    }
}
