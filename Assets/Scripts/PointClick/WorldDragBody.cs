using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class WorldDragBody : MonoBehaviour
{
    private const float SupportSurfaceTolerance = 0.05f;

    [SerializeField] private Rigidbody body;
    [SerializeField] private NavMeshObstacle navMeshObstacle;
    [SerializeField] private RoomBoundsConstraint boundsConstraint;

    private PointerContext activePointer;
    private PointClickController player;
    private Collider[] colliders;
    private Renderer[] renderers;
    private WorldDragProfile profile;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    private Vector3 lastValidDragPosition;
    private float dragBaseHeight;
    private float dragDepthZ;
    private bool originalObstacleEnabled;
    private bool originalObstacleCarving;
    private RigidbodyConstraints originalConstraints;

    public bool IsDragging => activePointer;
    public Room OwnerRoom => BoundsConstraint ? BoundsConstraint.OwnerRoom : GetComponentInParent<Room>(true);
    public Transform RootTransform => Body ? Body.transform : transform;
    public Vector3 RootPosition => RootTransform.position;
    public Quaternion RootRotation => RootTransform.rotation;

    private Rigidbody Body => body ? body : body = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>(true);
    private NavMeshObstacle Obstacle => navMeshObstacle ? navMeshObstacle : navMeshObstacle = RootTransform.GetComponentInChildren<NavMeshObstacle>(true);
    private RoomBoundsConstraint BoundsConstraint => boundsConstraint ? boundsConstraint : boundsConstraint = RootTransform.GetComponent<RoomBoundsConstraint>() ?? RootTransform.GetComponentInParent<RoomBoundsConstraint>(true);
    private PointClickController Player => player ? player : player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
    private Collider[] Colliders => colliders ??= RootTransform.GetComponentsInChildren<Collider>(true);
    private Renderer[] Renderers => renderers ??= RootTransform.GetComponentsInChildren<Renderer>(true);

    private void Reset()
    {
        body = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>(true);
        navMeshObstacle = body ? body.GetComponentInChildren<NavMeshObstacle>(true) : GetComponentInChildren<NavMeshObstacle>(true);
        boundsConstraint = body ? body.GetComponent<RoomBoundsConstraint>() ?? body.GetComponentInParent<RoomBoundsConstraint>(true) : GetComponent<RoomBoundsConstraint>() ?? GetComponentInParent<RoomBoundsConstraint>(true);
    }

    private void FixedUpdate()
    {
        if (!IsDragging || !Body)
        {
            return;
        }

        CacheValidPose();
        Vector3 desiredDragPosition = GetDesiredDragPositionOrFallback();
        if (!CanAdvanceDragPosition(desiredDragPosition))
        {
            desiredDragPosition = lastValidDragPosition;
        }
        else
        {
            lastValidDragPosition = desiredDragPosition;
        }

        ApplyDragForce(desiredDragPosition);
    }

    private void OnDisable()
    {
        if (IsDragging)
        {
            EndDrag(restoreInvalidPose: true);
        }
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return pointer && Body && enabled && gameObject.activeInHierarchy;
    }

    public bool BeginDrag(PointerContext pointer, in WorldDragProfile dragProfile)
    {
        if (!CanStartDrag(pointer) || IsDragging)
        {
            return false;
        }

        activePointer = pointer;
        profile = dragProfile;
        dragBaseHeight = RootPosition.y;
        dragDepthZ = RootPosition.z;
        lastValidPosition = RootPosition;
        lastValidRotation = RootRotation;
        lastValidDragPosition = GetLiftedDragPosition(lastValidPosition);

        CacheConstraints();
        ApplyManagedConstraints();
        DisableObstacle();
        Body.WakeUp();
        return true;
    }

    public void EndDrag(bool restoreInvalidPose)
    {
        if (!Body)
        {
            activePointer = null;
            return;
        }

        if (!IsDragging)
        {
            RestoreObstacle();
            return;
        }

        bool validPose = CanPlace(RootPosition, RootRotation);
        RestoreObstacle();
        activePointer = null;
        Body.constraints = originalConstraints | profile.ManagedConstraints;
        if (restoreInvalidPose && !validPose)
        {
            SnapToPose(lastValidPosition, lastValidRotation, resetVelocity: true);
        }
    }

    public bool CanPlace()
    {
        return CanPlace(RootPosition, RootRotation);
    }

    public void SnapToPose(Vector3 position, Quaternion rotation, bool resetVelocity)
    {
        if (!Body)
        {
            RootTransform.SetPositionAndRotation(position, rotation);
            return;
        }

        Body.position = position;
        Body.rotation = rotation;
        if (resetVelocity)
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }

        lastValidPosition = position;
        lastValidRotation = rotation;
        lastValidDragPosition = GetLiftedDragPosition(position);
        Physics.SyncTransforms();
    }

    public void RestoreLastValidPose()
    {
        SnapToPose(lastValidPosition, lastValidRotation, resetVelocity: true);
    }

    public void MoveToDragStart(PointerContext pointer, in WorldDragProfile dragProfile)
    {
        MoveToDragStart(pointer, pointer ? pointer.ScreenPosition : default, dragProfile);
    }

    public void MoveToDragStart(PointerContext pointer, Vector2 screenPosition, in WorldDragProfile dragProfile)
    {
        if (!Body || !pointer)
        {
            return;
        }

        profile = dragProfile;
        dragBaseHeight = RootPosition.y;
        dragDepthZ = RootPosition.z;
        lastValidPosition = RootPosition;
        lastValidRotation = RootRotation;
        lastValidDragPosition = GetLiftedDragPosition(lastValidPosition);
        Vector3 position = GetDesiredDragPositionOrFallback(pointer, screenPosition);
        position.y = dragBaseHeight + profile.LiftHeight;
        SnapToPose(position, RootRotation, resetVelocity: true);
    }

    public Bounds GetWorldBounds(Vector3 position, Quaternion rotation)
    {
        return WorldDragUtility.GetWorldBounds(Renderers, Colliders, RootPosition, position);
    }

    private void ApplyDragForce(Vector3 desiredRootPosition)
    {
        Vector3 desiredCenterPosition = desiredRootPosition + GetCenterOffset();
        Vector3 displacement = desiredCenterPosition - Body.worldCenterOfMass;
        Vector3 force = displacement * profile.SpringStrength - Body.linearVelocity * profile.SpringDamper;
        Body.AddForce(force, ForceMode.Force);
    }

    private void CacheValidPose()
    {
        if (!CanPlace(RootPosition, RootRotation))
        {
            return;
        }

        lastValidPosition = RootPosition;
        lastValidRotation = RootRotation;
    }

    private bool CanAdvanceDragPosition(Vector3 desiredRootPosition)
    {
        if (!RequestPlayerClearance(desiredRootPosition))
        {
            return false;
        }

        return CanPlace(desiredRootPosition, RootRotation);
    }

    private bool RequestPlayerClearance(Vector3 candidatePosition)
    {
        if (!Player || profile.PlayerClearance <= 0f)
        {
            return true;
        }

        Bounds candidateBounds = GetWorldBounds(candidatePosition, RootRotation);
        if (!candidateBounds.Intersects(Player.GetWorldBounds(profile.PlayerClearance)))
        {
            return true;
        }

        if (!Player.RequestSmoothClearance(candidateBounds, profile.PlayerClearance))
        {
            return false;
        }

        return !candidateBounds.Intersects(Player.GetWorldBounds(profile.PlayerClearance));
    }

    private bool CanPlace(Vector3 position, Quaternion rotation)
    {
        Bounds bounds = GetWorldBounds(position, rotation);
        if (!IsWithinRoom(bounds))
        {
            return false;
        }

        return !WorldDragUtility.IsBlocked(bounds, rotation, profile.BlockingLayers, Colliders, SupportSurfaceTolerance, ShouldIgnoreBlocker);
    }

    private bool ShouldIgnoreBlocker(Collider hit)
    {
        return hit && Player && hit.GetComponentInParent<PointClickController>() == Player;
    }

    private Vector3 GetDesiredDragPositionOrFallback()
    {
        return GetDesiredDragPositionOrFallback(activePointer, activePointer ? activePointer.ScreenPosition : default);
    }

    private Vector3 GetDesiredDragPositionOrFallback(PointerContext pointer, Vector2 screenPosition)
    {
        if (TryGetDesiredDragPosition(pointer, screenPosition, out Vector3 position))
        {
            return position;
        }

        return lastValidDragPosition;
    }

    private bool TryGetDesiredDragPosition(PointerContext pointer, Vector2 screenPosition, out Vector3 position)
    {
        position = default;
        if (!pointer)
        {
            return false;
        }

        bool hit = profile.LockZPosition
            ? pointer.TryGetPointOnPlane(screenPosition, Vector3.forward, new Vector3(0f, dragBaseHeight, dragDepthZ), out position)
            : pointer.TryGetPointOnPlane(screenPosition, Vector3.up, new Vector3(0f, dragBaseHeight, 0f), out position);
        if (!hit)
        {
            return false;
        }

        position.y = dragBaseHeight + profile.LiftHeight;
        if (profile.LockZPosition)
        {
            position.z = dragDepthZ;
        }

        position = ClampToRoom(position);

        return float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z);
    }

    private Vector3 GetCenterOffset()
    {
        return Body.worldCenterOfMass - RootPosition;
    }

    private Vector3 GetLiftedDragPosition(Vector3 position)
    {
        position.y = dragBaseHeight + profile.LiftHeight;
        if (profile.LockZPosition)
        {
            position.z = dragDepthZ;
        }

        return ClampToRoom(position);
    }

    private Vector3 ClampToRoom(Vector3 position)
    {
        if (BoundsConstraint)
        {
            return BoundsConstraint.Clamp(position);
        }

        if (!OwnerRoom)
        {
            return position;
        }

        return profile.LockZPosition
            ? OwnerRoom.ClampPoint(position, dragDepthZ)
            : OwnerRoom.ClampPosition(position);
    }

    private bool IsWithinRoom(Bounds bounds)
    {
        if (BoundsConstraint)
        {
            return BoundsConstraint.Contains(bounds);
        }

        return !OwnerRoom || OwnerRoom.ContainsBounds(bounds);
    }

    private void CacheConstraints()
    {
        if (!Body)
        {
            return;
        }

        originalConstraints = Body.constraints;
    }

    private void ApplyManagedConstraints()
    {
        if (!Body)
        {
            return;
        }

        Body.constraints = originalConstraints | profile.ManagedConstraints;
    }

    private void DisableObstacle()
    {
        if (!Obstacle || !profile.BlocksNavigation)
        {
            return;
        }

        originalObstacleEnabled = Obstacle.enabled;
        originalObstacleCarving = Obstacle.carving;
        Obstacle.enabled = false;
    }

    private void RestoreObstacle()
    {
        if (!Obstacle || !profile.BlocksNavigation)
        {
            return;
        }

        Obstacle.enabled = originalObstacleEnabled;
        Obstacle.carving = originalObstacleCarving;
    }
}

public readonly struct WorldDragProfile
{
    public WorldDragProfile(
        LayerMask blockingLayers,
        bool blocksNavigation,
        bool lockZPosition,
        bool lockRotationX,
        bool lockRotationY,
        float liftHeight,
        float springStrength,
        float springDamper,
        float playerClearance)
    {
        BlockingLayers = blockingLayers;
        BlocksNavigation = blocksNavigation;
        LockZPosition = lockZPosition;
        LockRotationX = lockRotationX;
        LockRotationY = lockRotationY;
        LiftHeight = Mathf.Max(0f, liftHeight);
        SpringStrength = Mathf.Max(0f, springStrength);
        SpringDamper = Mathf.Max(0f, springDamper);
        PlayerClearance = Mathf.Max(0f, playerClearance);
    }

    public LayerMask BlockingLayers { get; }
    public bool BlocksNavigation { get; }
    public bool LockZPosition { get; }
    public bool LockRotationX { get; }
    public bool LockRotationY { get; }
    public float LiftHeight { get; }
    public float SpringStrength { get; }
    public float SpringDamper { get; }
    public float PlayerClearance { get; }

    public RigidbodyConstraints ManagedConstraints
    {
        get
        {
            RigidbodyConstraints constraints = RigidbodyConstraints.None;
            if (LockZPosition)
            {
                constraints |= RigidbodyConstraints.FreezePositionZ;
            }

            if (LockRotationX)
            {
                constraints |= RigidbodyConstraints.FreezeRotationX;
            }

            if (LockRotationY)
            {
                constraints |= RigidbodyConstraints.FreezeRotationY;
            }

            return constraints;
        }
    }
}
