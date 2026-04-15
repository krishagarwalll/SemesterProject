using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class RoomCameraController : MonoBehaviour
{
    [SerializeField] private Room room;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool disableCinemachineBrainWhenLive = true;
    [SerializeField] private bool snapImmediately = true;

    private CinemachineCamera virtualCamera;
    private CinemachineBrain cameraBrain;
    private bool isLive;
    private float desiredOrthographicSize = 5f;
    private float minOrthographicSize = 2f;
    private float maxOrthographicSize = 10f;

    private Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    private Camera TargetCamera => targetCamera ? targetCamera : targetCamera = Camera.main;
    private CinemachineCamera VirtualCamera => virtualCamera ? virtualCamera : virtualCamera = GetComponent<CinemachineCamera>();
    private CinemachineBrain CameraBrain => cameraBrain ? cameraBrain : cameraBrain = TargetCamera ? TargetCamera.GetComponent<CinemachineBrain>() : null;

    private void Reset()
    {
        room = GetComponentInParent<Room>(true);
        targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (VirtualCamera)
        {
            VirtualCamera.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (VirtualCamera)
        {
            VirtualCamera.enabled = false;
        }

        if (isLive)
        {
            ApplyShot();
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
        if (isLive && !snapImmediately)
        {
            ApplyShot();
        }
    }

    public void SetLive(bool live, Camera viewCamera = null, float desiredSize = 5f, float minSize = 2f, float maxSize = 10f)
    {
        isLive = live;
        targetCamera = viewCamera ? viewCamera : Camera.main;
        desiredOrthographicSize = desiredSize;
        minOrthographicSize = minSize;
        maxOrthographicSize = maxSize;

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
            ApplyShot();
        }
    }

    private void ApplyShot()
    {
        Camera camera = TargetCamera;
        if (!camera || !OwnerRoom)
        {
            return;
        }

        camera.transform.position = OwnerRoom.GetCameraPosition();
        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true;

        float aspect = camera.aspect > 0.01f ? camera.aspect : 16f / 9f;
        OwnerRoom.TryGetOrthographicSize(aspect, out float roomOrthographicSize);
        camera.orthographicSize = Mathf.Clamp(
            desiredOrthographicSize,
            minOrthographicSize,
            Mathf.Max(minOrthographicSize, Mathf.Min(maxOrthographicSize, roomOrthographicSize)));
    }
}
