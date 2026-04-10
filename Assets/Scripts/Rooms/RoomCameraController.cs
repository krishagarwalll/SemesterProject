using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class RoomCameraController : MonoBehaviour
{
    [SerializeField] private Room room;
    [SerializeField] private int livePriority = 100;
    [SerializeField] private int idlePriority;

    private CinemachineCamera virtualCamera;
    private Camera outputCamera;
    private Vector3 localShotPosition;
    private Quaternion shotRotation;
    private bool hasCachedShot;
    private bool isLive;
    private float desiredVerticalFov = 34f;
    private float minVerticalFov = 20f;
    private float maxVerticalFov = 60f;

    private Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    private CinemachineCamera VirtualCamera => virtualCamera ? virtualCamera : virtualCamera = GetComponent<CinemachineCamera>();

    private void Reset()
    {
        room = GetComponentInParent<Room>(true);
    }

    private void Awake()
    {
        EnsureShotCached();
        RefreshShot(Camera.main);
        SetPriority(false);
    }

    private void OnEnable()
    {
        RefreshShot(Camera.main);
    }

    private void OnValidate()
    {
        if (!room)
        {
            room = GetComponentInParent<Room>(true);
        }

        livePriority = Mathf.Max(idlePriority, livePriority);
        if (Application.isPlaying)
        {
            return;
        }

        CacheShot();
        RefreshShot(Camera.main);
    }

    public void SetLive(bool live, Camera viewCamera = null, float desiredFov = 34f, float minFov = 20f, float maxFov = 60f)
    {
        EnsureShotCached();
        isLive = live;
        outputCamera = viewCamera ? viewCamera : Camera.main;
        desiredVerticalFov = desiredFov;
        minVerticalFov = minFov;
        maxVerticalFov = maxFov;
        SetPriority(live);
        if (live)
        {
            RefreshShot(outputCamera);
        }
    }

    private void LateUpdate()
    {
        if (!isLive)
        {
            return;
        }

        RefreshShot(outputCamera ? outputCamera : Camera.main);
    }

    private void SetPriority(bool live)
    {
        if (VirtualCamera)
        {
            VirtualCamera.Priority = live ? livePriority : idlePriority;
        }
    }

    private void RefreshShot(Camera viewCamera)
    {
        if (!OwnerRoom || !VirtualCamera)
        {
            return;
        }

        EnsureShotCached();
        viewCamera = viewCamera ? viewCamera : Camera.main;
        float aspect = viewCamera ? viewCamera.aspect : 16f / 9f;
        Vector3 adjustedLocalPosition = GetAdjustedLocalShotPosition(aspect, desiredVerticalFov);
        Vector3 cameraPosition = OwnerRoom.transform.TransformPoint(adjustedLocalPosition);

        float roomMaxFov = maxVerticalFov;
        if (OwnerRoom.TryGetCameraFit(cameraPosition, aspect, out RoomCameraFit fit))
        {
            roomMaxFov = Mathf.Rad2Deg * 2f * Mathf.Atan(fit.LimitedHalfHeight / Mathf.Max(0.01f, fit.NearestDepth));
        }

        float maxAllowedFov = Mathf.Max(minVerticalFov, Mathf.Min(maxVerticalFov, roomMaxFov));
        float clampedFov = Mathf.Clamp(desiredVerticalFov, minVerticalFov, maxAllowedFov);

        transform.localPosition = adjustedLocalPosition;
        transform.localRotation = shotRotation;

        LensSettings lens = VirtualCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        lens.FieldOfView = clampedFov;
        VirtualCamera.Lens = lens;
    }

    private void CacheShot()
    {
        localShotPosition = transform.localPosition;
        shotRotation = transform.localRotation;
        hasCachedShot = true;
    }

    private void EnsureShotCached()
    {
        if (!hasCachedShot)
        {
            CacheShot();
        }
    }

    private Vector3 GetAdjustedLocalShotPosition(float aspect, float desiredFov)
    {
        Vector3 adjustedLocalPosition = localShotPosition;
        Vector3 cameraPosition = OwnerRoom.transform.TransformPoint(adjustedLocalPosition);
        if (!OwnerRoom.TryGetCameraFit(cameraPosition, aspect, out RoomCameraFit fit))
        {
            return adjustedLocalPosition;
        }

        float targetDistance = fit.LimitedHalfHeight / Mathf.Tan(Mathf.Deg2Rad * desiredFov * 0.5f);
        if (fit.NearestDepth <= targetDistance)
        {
            return adjustedLocalPosition;
        }

        float forwardDolly = Mathf.Min(fit.NearestDepth - targetDistance, OwnerRoom.CameraFitMaxForwardDolly);
        adjustedLocalPosition += Vector3.forward * forwardDolly;
        return adjustedLocalPosition;
    }
}
