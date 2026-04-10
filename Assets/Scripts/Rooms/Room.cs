using UnityEngine;

[DisallowMultipleComponent]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomId;
    [SerializeField] private Collider boundsVolume;
    [SerializeField] private RoomAnchor defaultAnchor;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0.4f, -10f);
    [SerializeField, Min(0f)] private float cameraFitPadding = 0.1f;
    [SerializeField] private Vector2 cameraFitSizeBuffer = new(2.25f, 1.5f);
    [SerializeField] private float cameraFitDepthBuffer = -1f;
    [SerializeField, Min(0f)] private float cameraFitMaxForwardDolly = 1.75f;

    private RoomAnchor[] anchors;
    private RoomCameraController cameraController;

    public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;
    public Collider BoundsVolume => boundsVolume ? boundsVolume : boundsVolume = GetComponentInChildren<Collider>(true);
    public RoomAnchor DefaultAnchor => defaultAnchor ? defaultAnchor : defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
    public Transform ContentRoot => contentRoot ? contentRoot : contentRoot = transform;
    public Vector3 CameraOffset => cameraOffset;
    public float CameraFitPadding => cameraFitPadding;
    public float CameraFitMaxForwardDolly => cameraFitMaxForwardDolly;
    public float DefaultItemDepth => BoundsVolume ? BoundsVolume.bounds.center.z : transform.position.z;
    public RoomCameraController CameraController => cameraController ? cameraController : cameraController = GetComponentInChildren<RoomCameraController>(true);
    private RoomAnchor[] Anchors => anchors ??= GetComponentsInChildren<RoomAnchor>(true);

    private void Reset()
    {
        boundsVolume = GetComponentInChildren<Collider>(true);
        defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
        contentRoot = transform;
    }

    private void OnValidate()
    {
        if (!boundsVolume)
        {
            boundsVolume = GetComponentInChildren<Collider>(true);
        }

        if (!defaultAnchor)
        {
            defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
        }

        if (!contentRoot)
        {
            contentRoot = transform;
        }

        cameraFitPadding = Mathf.Max(0f, cameraFitPadding);
        cameraFitDepthBuffer = Mathf.Max(-10f, cameraFitDepthBuffer);
        cameraFitMaxForwardDolly = Mathf.Max(0f, cameraFitMaxForwardDolly);
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
            return point;
        }

        return BoundsVolume.ClosestPoint(point);
    }

    public bool ContainsPoint(Vector3 point, float tolerance = 0.02f)
    {
        if (!BoundsVolume)
        {
            return true;
        }

        Vector3 closestPoint = BoundsVolume.ClosestPoint(point);
        return (closestPoint - point).sqrMagnitude <= tolerance * tolerance;
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
        Bounds bounds = BoundsVolume ? BoundsVolume.bounds : new Bounds(transform.position, Vector3.zero);
        return new Vector3(bounds.center.x + cameraOffset.x, bounds.center.y + cameraOffset.y, cameraOffset.z);
    }

    public bool TryGetCameraFit(Vector3 cameraPosition, float aspect, out RoomCameraFit fit)
    {
        Bounds bounds = GetAdjustedCameraBounds();
        Vector3 cameraLocalPosition = transform.InverseTransformPoint(cameraPosition);
        float nearestFaceZ = cameraLocalPosition.z <= bounds.center.z ? bounds.min.z : bounds.max.z;
        float nearestDepth = Mathf.Abs(nearestFaceZ - cameraLocalPosition.z) + cameraFitDepthBuffer;
        if (nearestDepth <= 0.01f)
        {
            fit = default;
            return false;
        }

        float availableHalfHeight = Mathf.Max(0.01f, bounds.extents.y - cameraFitPadding);
        float availableHalfWidth = Mathf.Max(0.01f, bounds.extents.x - cameraFitPadding);
        float limitedHalfHeight = Mathf.Min(availableHalfHeight, availableHalfWidth / Mathf.Max(0.01f, aspect));
        fit = new RoomCameraFit(nearestDepth, limitedHalfHeight);
        return true;
    }

    private Bounds GetAdjustedCameraBounds()
    {
        Bounds bounds = BoundsVolume ? BoundsVolume.bounds : new Bounds(transform.position, Vector3.one * 2f);
        Vector3 adjustedExtents = bounds.extents + new Vector3(cameraFitSizeBuffer.x, cameraFitSizeBuffer.y, 0f);
        adjustedExtents.x = Mathf.Max(0.01f, adjustedExtents.x);
        adjustedExtents.y = Mathf.Max(0.01f, adjustedExtents.y);
        adjustedExtents.z = Mathf.Max(0.01f, bounds.extents.z);
        bounds.extents = adjustedExtents;
        return bounds;
    }

    public void SetCameraLive(bool isLive, Camera outputCamera = null, float desiredVerticalFov = 34f, float minVerticalFov = 20f, float maxVerticalFov = 60f)
    {
        CameraController?.SetLive(isLive, outputCamera, desiredVerticalFov, minVerticalFov, maxVerticalFov);
    }

}

public readonly struct RoomCameraFit
{
    public RoomCameraFit(float nearestDepth, float limitedHalfHeight)
    {
        NearestDepth = nearestDepth;
        LimitedHalfHeight = limitedHalfHeight;
    }

    public float NearestDepth { get; }
    public float LimitedHalfHeight { get; }
}
