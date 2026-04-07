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
    private Quaternion shotRotation;
    private bool isLive;
    private float lastAspect = -1f;
    private float lastVerticalFov = -1f;
    private float lastOrthographicSize = -1f;
    private bool lastOrthographic;

    private Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    private CinemachineCamera VirtualCamera => virtualCamera ? virtualCamera : virtualCamera = GetComponent<CinemachineCamera>();

    private void Reset()
    {
        room = GetComponentInParent<Room>(true);
    }

    private void Awake()
    {
        shotRotation = transform.rotation;
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

        shotRotation = transform.rotation;
        RefreshShot(Camera.main);
    }

    private void LateUpdate()
    {
        if (!isLive)
        {
            return;
        }

        Camera viewCamera = outputCamera ? outputCamera : Camera.main;
        if (!viewCamera)
        {
            return;
        }

        LensSettings lens = VirtualCamera.Lens;
        float aspect = viewCamera.aspect;
        float verticalFov = ResolveVerticalFov(lens, viewCamera);
        float orthographicSize = ResolveOrthographicSize(lens, viewCamera);
        if (!Mathf.Approximately(lastAspect, aspect)
            || !Mathf.Approximately(lastVerticalFov, verticalFov)
            || !Mathf.Approximately(lastOrthographicSize, orthographicSize)
            || lastOrthographic != lens.Orthographic)
        {
            RefreshShot(viewCamera);
        }
    }

    public void SetLive(bool live, Camera viewCamera = null)
    {
        isLive = live;
        outputCamera = viewCamera ? viewCamera : Camera.main;
        SetPriority(live);
        if (live)
        {
            RefreshShot(outputCamera);
        }
    }

    private void SetPriority(bool live)
    {
        if (!VirtualCamera)
        {
            return;
        }

        VirtualCamera.Priority = live ? livePriority : idlePriority;
    }

    private void RefreshShot(Camera viewCamera)
    {
        if (!OwnerRoom || !VirtualCamera)
        {
            return;
        }

        viewCamera = viewCamera ? viewCamera : Camera.main;
        LensSettings lens = VirtualCamera.Lens;
        float aspect = ResolveAspect(lens, viewCamera);
        float verticalFov = ResolveVerticalFov(lens, viewCamera);
        float orthographicSize = ResolveOrthographicSize(lens, viewCamera);
        bool orthographic = lens.Orthographic;
        transform.position = OwnerRoom.GetCameraPosition(aspect, verticalFov, orthographic, orthographicSize);
        transform.rotation = shotRotation;
        lastAspect = aspect;
        lastVerticalFov = verticalFov;
        lastOrthographicSize = orthographicSize;
        lastOrthographic = orthographic;
    }

    private static float ResolveAspect(LensSettings lens, Camera viewCamera)
    {
        if (viewCamera)
        {
            return viewCamera.aspect;
        }

        return lens.Aspect > 0f ? lens.Aspect : 16f / 9f;
    }

    private static float ResolveVerticalFov(LensSettings lens, Camera viewCamera)
    {
        if (lens.FieldOfView > 0f)
        {
            return lens.FieldOfView;
        }

        return viewCamera ? viewCamera.fieldOfView : 34f;
    }

    private static float ResolveOrthographicSize(LensSettings lens, Camera viewCamera)
    {
        if (lens.OrthographicSize > 0f)
        {
            return lens.OrthographicSize;
        }

        return viewCamera ? viewCamera.orthographicSize : 5f;
    }
}
