using UnityEngine;

[DisallowMultipleComponent]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomId;
    [SerializeField] private Collider boundsVolume;
    [SerializeField] private RoomAnchor defaultAnchor;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0.4f, -10f);
    [SerializeField, Min(0f)] private float cameraFitPadding = 0.35f;

    private RoomAnchor[] anchors;
    private RoomCameraController cameraController;

    public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;
    public Collider BoundsVolume => boundsVolume ? boundsVolume : boundsVolume = GetComponentInChildren<Collider>(true);
    public RoomAnchor DefaultAnchor => defaultAnchor ? defaultAnchor : defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
    public Vector3 CameraOffset => cameraOffset;
    public float CameraFitPadding => cameraFitPadding;
    public RoomCameraController CameraController => cameraController ? cameraController : cameraController = GetComponentInChildren<RoomCameraController>(true);
    private RoomAnchor[] Anchors => anchors ??= GetComponentsInChildren<RoomAnchor>(true);

    private void Reset()
    {
        boundsVolume = GetComponentInChildren<Collider>(true);
        defaultAnchor = GetComponentInChildren<RoomAnchor>(true);
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

        cameraFitPadding = Mathf.Max(0f, cameraFitPadding);
        anchors = null;
        cameraController = null;
    }

    public Vector3 ClampPoint(Vector3 point, float zPosition)
    {
        if (!BoundsVolume)
        {
            point.z = zPosition;
            return point;
        }

        Vector3 clampedPoint = BoundsVolume.ClosestPoint(point);
        clampedPoint.z = zPosition;
        return clampedPoint;
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

    public Vector3 GetCameraPosition(float aspect, float verticalFov, bool orthographic, float orthographicSize)
    {
        Bounds bounds = BoundsVolume ? BoundsVolume.bounds : new Bounds(transform.position, Vector3.zero);
        Vector3 desiredPosition = new(bounds.center.x + cameraOffset.x, bounds.center.y + cameraOffset.y, cameraOffset.z);

        float halfHeight;
        if (orthographic)
        {
            halfHeight = Mathf.Max(0f, orthographicSize);
        }
        else
        {
            float planeDepth = Mathf.Clamp(bounds.center.z, bounds.min.z, bounds.max.z);
            float distance = Mathf.Max(0.01f, Mathf.Abs(planeDepth - desiredPosition.z));
            halfHeight = Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f) * distance;
        }

        halfHeight += cameraFitPadding;
        float halfWidth = halfHeight * Mathf.Max(0.01f, aspect);

        desiredPosition.x = FitAxis(desiredPosition.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth, bounds.center.x);
        desiredPosition.y = FitAxis(desiredPosition.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight, bounds.center.y);
        return desiredPosition;
    }

    public void SetCameraLive(bool isLive, Camera outputCamera = null)
    {
        CameraController?.SetLive(isLive, outputCamera);
    }

    private static float FitAxis(float desired, float min, float max, float fallback)
    {
        return min > max ? fallback : Mathf.Clamp(desired, min, max);
    }
}
