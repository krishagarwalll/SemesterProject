using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class RoomCameraController : MonoBehaviour
{
    [SerializeField] private Room room;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool disableCinemachineBrainWhenLive = true;
    [SerializeField, Min(0f)] private float followSmoothTime = 0.12f;

    private CinemachineCamera virtualCamera;
    private CinemachineBrain cameraBrain;
    private bool isLive;
    private bool snapNextFrame;
    private float desiredOrthographicSize = 5f;
    private float minOrthographicSize = 2f;
    private float maxOrthographicSize = 10f;
    private Vector3 cameraVelocity;

    private Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    private Camera TargetCamera => targetCamera ? targetCamera : targetCamera = Camera.main;
    private CinemachineCamera VirtualCamera => virtualCamera ? virtualCamera : virtualCamera = GetComponent<CinemachineCamera>();
    private CinemachineBrain CameraBrain => cameraBrain ? cameraBrain : cameraBrain = TargetCamera ? TargetCamera.GetComponent<CinemachineBrain>() : null;

    private void Reset()
    {
        room = GetComponentInParent<Room>(true);
        targetCamera = Camera.main;
        followTarget = FindFollowTarget();
    }

    private void Awake()
    {
        if (VirtualCamera)
        {
            VirtualCamera.enabled = false;
        }

        if (!followTarget)
        {
            followTarget = FindFollowTarget();
        }
    }

    private void OnEnable()
    {
        if (VirtualCamera)
        {
            VirtualCamera.enabled = false;
        }

        if (!followTarget)
        {
            followTarget = FindFollowTarget();
        }

        if (isLive)
        {
            ApplyShot(snap: true);
        }
    }

    private void OnValidate()
    {
        if (!room)
        {
            room = GetComponentInParent<Room>(true);
        }
    }

    private void LateUpdate()
    {
        if (!isLive)
        {
            return;
        }

        ApplyShot(snap: snapNextFrame);
        snapNextFrame = false;
    }

    public void SetLive(bool live, Camera viewCamera = null, float desiredSize = 5f, float minSize = 2f, float maxSize = 10f)
    {
        isLive = live;
        snapNextFrame = true;
        targetCamera = viewCamera ? viewCamera : Camera.main;
        desiredOrthographicSize = desiredSize;
        minOrthographicSize = minSize;
        maxOrthographicSize = maxSize;

        if (!followTarget)
        {
            followTarget = FindFollowTarget();
        }

        if (VirtualCamera)
        {
            VirtualCamera.enabled = false;
        }

        if (CameraBrain)
        {
            CameraBrain.enabled = !live || !disableCinemachineBrainWhenLive;
        }

        if (live)
        {
            ApplyShot(snap: true);
        }
    }

    private void ApplyShot(bool snap)
    {
        Camera camera = TargetCamera;
        if (!camera || !OwnerRoom)
        {
            return;
        }

        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true;

        float aspect = camera.aspect > 0.01f ? camera.aspect : 16f / 9f;
        OwnerRoom.TryGetOrthographicSize(aspect, out float roomOrthographicSize);
        float orthoSize = Mathf.Clamp(
            desiredOrthographicSize,
            minOrthographicSize,
            Mathf.Max(minOrthographicSize, Mathf.Min(maxOrthographicSize, roomOrthographicSize)));
        camera.orthographicSize = orthoSize;

        Vector3 targetPos = ComputeCameraTarget(orthoSize, aspect);

        if (snap || followSmoothTime <= 0f)
        {
            cameraVelocity = Vector3.zero;
            camera.transform.position = targetPos;
        }
        else
        {
            camera.transform.position = Vector3.SmoothDamp(
                camera.transform.position, targetPos, ref cameraVelocity,
                followSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }

    private Vector3 ComputeCameraTarget(float orthoSize, float aspect)
    {
        Vector3 roomCenter = OwnerRoom.GetCameraPosition();

        if (!followTarget || !OwnerRoom.BoundsVolume)
        {
            return roomCenter;
        }

        Bounds bounds = OwnerRoom.BoundsVolume.bounds;
        Vector3 offset = OwnerRoom.CameraOffset;

        float halfW = orthoSize * aspect;
        float halfH = orthoSize;

        // Camera X/Y must keep the viewport within room bounds.
        // If the room is narrower than the viewport, fall back to room centre.
        float minX = bounds.min.x + halfW;
        float maxX = bounds.max.x - halfW;
        float minY = bounds.min.y + halfH;
        float maxY = bounds.max.y - halfH;

        float desiredX = followTarget.position.x + offset.x;
        float desiredY = followTarget.position.y + offset.y;

        float camX = maxX >= minX ? Mathf.Clamp(desiredX, minX, maxX) : bounds.center.x + offset.x;
        float camY = maxY >= minY ? Mathf.Clamp(desiredY, minY, maxY) : bounds.center.y + offset.y;

        return new Vector3(camX, camY, roomCenter.z);
    }

    [Button("Auto-Find Player Follow Target", editModeOnly: true)]
    private void AutoFindFollowTarget()
    {
        PoptropicaController pc = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
        if (pc)
        {
            followTarget = pc.transform;
            Debug.Log($"[RoomCameraController] Follow target set to '{pc.name}'.", this);
        }
        else
        {
            Debug.LogWarning("[RoomCameraController] No PoptropicaController found in scene.", this);
        }
    }

    private void OnDrawGizmos()
    {
        if (!OwnerRoom || !OwnerRoom.BoundsVolume) return;

        float aspect = Camera.main ? (Camera.main.aspect > 0.01f ? Camera.main.aspect : 16f / 9f) : 16f / 9f;
        OwnerRoom.TryGetOrthographicSize(aspect, out float orthoSize);
        orthoSize = Mathf.Clamp(orthoSize, 1.25f, 5f);

        Bounds b = OwnerRoom.BoundsVolume.bounds;
        float safeW = Mathf.Max(0f, b.size.x - orthoSize * aspect * 2f);
        float safeH = Mathf.Max(0f, b.size.y - orthoSize * 2f);
        float cx = b.center.x, cy = b.center.y, cz = b.center.z;
        float hw = safeW * 0.5f, hh = safeH * 0.5f;

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        Vector3 tl = new(cx - hw, cy + hh, cz);
        Vector3 tr = new(cx + hw, cy + hh, cz);
        Vector3 bl = new(cx - hw, cy - hh, cz);
        Vector3 br = new(cx + hw, cy - hh, cz);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);
    }

    private static Transform FindFollowTarget()
    {
        PoptropicaController pc = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
        return pc ? pc.transform : null;
    }
}
