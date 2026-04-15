using UnityEngine;

[DisallowMultipleComponent]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomId;
    [SerializeField] private Collider2D boundsVolume;
    [SerializeField] private RoomAnchor defaultAnchor;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Transform backdropRoot;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0f, -10f);
    [SerializeField, Min(0f)] private float orthographicPadding = 0.25f;
    [SerializeField] private Vector2 orthographicSizeBuffer = new(0.5f, 0.35f);
    [SerializeField, Min(0f)] private float containmentPadding = 0.08f;

    private RoomAnchor[] anchors;
    private RoomCameraController cameraController;

    public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;
    public Collider2D BoundsVolume => boundsVolume ? boundsVolume : boundsVolume = GetComponentInChildren<Collider2D>(true);
    public RoomAnchor DefaultAnchor => defaultAnchor ? defaultAnchor : defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
    public Transform ContentRoot => contentRoot ? contentRoot : contentRoot = transform;
    public Transform BackdropRoot => backdropRoot ? backdropRoot : backdropRoot = transform.Find("Backdrop");
    public Vector3 CameraOffset => cameraOffset;
    public float DefaultItemDepth => transform.position.z;
    public float GroundY => DefaultAnchor ? DefaultAnchor.transform.position.y : BoundsVolume ? BoundsVolume.bounds.min.y : transform.position.y;
    public RoomCameraController CameraController => cameraController ? cameraController : cameraController = GetComponentInChildren<RoomCameraController>(true);

    private RoomAnchor[] Anchors => anchors ??= GetComponentsInChildren<RoomAnchor>(true);

    private void Reset()
    {
        boundsVolume = GetComponentInChildren<Collider2D>(true);
        defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
        contentRoot = transform;
        backdropRoot = transform.Find("Backdrop");
    }

    private void OnValidate()
    {
        orthographicPadding = Mathf.Max(0f, orthographicPadding);
        if (!boundsVolume)
        {
            boundsVolume = GetComponentInChildren<Collider2D>(true);
        }

        if (!defaultAnchor)
        {
            defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
        }

        if (!contentRoot)
        {
            contentRoot = transform;
        }

        if (!backdropRoot)
        {
            backdropRoot = transform.Find("Backdrop");
        }

        anchors = null;
        cameraController = null;
    }

    public Vector3 ClampPoint(Vector3 point, float zPosition)
    {
        Vector3 clampedPoint = ClampPosition(point);
        clampedPoint.z = zPosition;
        return clampedPoint;
    }

    public Vector3 ClampPosition(Vector3 point)
    {
        if (!BoundsVolume)
        {
            point.z = transform.position.z;
            return point;
        }

        Vector2 closest = BoundsVolume.ClosestPoint((Vector2)point);
        return new Vector3(closest.x, closest.y, point.z);
    }

    public Vector3 ClampBoundsCenter(Bounds itemBounds, float zPosition)
    {
        if (!BoundsVolume)
        {
            Vector3 fallbackCenter = itemBounds.center;
            fallbackCenter.z = zPosition;
            return fallbackCenter;
        }

        Bounds containmentBounds = GetContainmentBounds();
        Vector3 extents = itemBounds.extents;
        float minX = containmentBounds.min.x + extents.x;
        float maxX = containmentBounds.max.x - extents.x;
        float minY = containmentBounds.min.y + extents.y;
        float maxY = containmentBounds.max.y - extents.y;

        Vector3 boundsCenter = itemBounds.center;
        boundsCenter.x = ClampAxis(boundsCenter.x, minX, maxX, containmentBounds.center.x);
        boundsCenter.y = ClampAxis(boundsCenter.y, minY, maxY, containmentBounds.center.y);
        boundsCenter.z = zPosition;
        return boundsCenter;
    }

    public bool ContainsPoint(Vector3 point, float tolerance = 0.02f)
    {
        if (!BoundsVolume)
        {
            return true;
        }

        Bounds containmentBounds = GetContainmentBounds();
        return point.x >= containmentBounds.min.x - tolerance
            && point.x <= containmentBounds.max.x + tolerance
            && point.y >= containmentBounds.min.y - tolerance
            && point.y <= containmentBounds.max.y + tolerance;
    }

    public bool ContainsBounds(Bounds bounds, float tolerance = 0.02f)
    {
        if (!BoundsVolume)
        {
            return true;
        }

        Bounds containmentBounds = GetContainmentBounds();
        return bounds.min.x >= containmentBounds.min.x - tolerance
            && bounds.max.x <= containmentBounds.max.x + tolerance
            && bounds.min.y >= containmentBounds.min.y - tolerance
            && bounds.max.y <= containmentBounds.max.y + tolerance;
    }

    public bool TryGetAnchor(string anchorId, out RoomAnchor anchor)
    {
        if (string.IsNullOrWhiteSpace(anchorId))
        {
            anchor = DefaultAnchor;
            return anchor;
        }

        for (int i = 0; i < Anchors.Length; i++)
        {
            if (Anchors[i] && Anchors[i].AnchorId == anchorId)
            {
                anchor = Anchors[i];
                return true;
            }
        }

        anchor = DefaultAnchor;
        return anchor;
    }

    public Vector3 GetCameraPosition()
    {
        Bounds bounds = GetCameraBounds();
        return new Vector3(bounds.center.x + cameraOffset.x, bounds.center.y + cameraOffset.y, transform.position.z + cameraOffset.z);
    }

    public bool TryGetOrthographicSize(float aspect, out float size)
    {
        Bounds bounds = GetCameraBounds();
        float halfHeight = bounds.extents.y + orthographicPadding + orthographicSizeBuffer.y;
        float halfWidth = bounds.extents.x + orthographicPadding + orthographicSizeBuffer.x;
        size = Mathf.Max(0.1f, Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.01f, aspect)));
        return true;
    }

    public void SetCameraLive(bool isLive, Camera outputCamera = null, float desiredOrthographicSize = 5f, float minOrthographicSize = 2f, float maxOrthographicSize = 10f)
    {
        CameraController?.SetLive(isLive, outputCamera, desiredOrthographicSize, minOrthographicSize, maxOrthographicSize);
    }

    private Bounds GetCameraBounds()
    {
        if (BoundsVolume)
        {
            return BoundsVolume.bounds;
        }

        return new Bounds(transform.position, Vector3.one * 2f);
    }

    private Bounds GetContainmentBounds()
    {
        Bounds bounds = BoundsVolume ? BoundsVolume.bounds : new Bounds(transform.position, Vector3.one * 2f);
        float safePadding = Mathf.Max(0f, containmentPadding);
        Vector3 size = bounds.size;
        size.x = Mathf.Max(0.01f, size.x - safePadding * 2f);
        size.y = Mathf.Max(0.01f, size.y - safePadding * 2f);
        bounds.size = size;
        return bounds;
    }

    private static float ClampAxis(float value, float min, float max, float fallback)
    {
        if (max < min)
        {
            return fallback;
        }

        return Mathf.Clamp(value, min, max);
    }
}
