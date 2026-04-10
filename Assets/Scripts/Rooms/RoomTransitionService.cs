using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class RoomTransitionService : MonoBehaviour
{
    [SerializeField] private PointClickController player;
    [SerializeField] private ScreenFade screenFade;
    [SerializeField] private Room activeRoom;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField, Min(1f)] private float userVerticalFov = 50f;
    [SerializeField, Min(1f)] private float minVerticalFov = 30f;
    [SerializeField, Min(1f)] private float maxVerticalFov = 80f;
    [SerializeField, Min(0.1f)] private float zoomStep = 2f;
    [SerializeField] private bool invertZoom;

    private Coroutine transitionRoutine;

    private PointClickController Player => player ? player : player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
    private ScreenFade Fade => screenFade ? screenFade : screenFade = FindFirstObjectByType<ScreenFade>(FindObjectsInactive.Include);
    public Room ActiveRoom => activeRoom;
    public float UserVerticalFov => userVerticalFov;
    public float MinVerticalFov => minVerticalFov;
    public float MaxVerticalFov => maxVerticalFov;

    private void Reset()
    {
        player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
        screenFade = FindFirstObjectByType<ScreenFade>(FindObjectsInactive.Include);
        if (!activeRoom && player)
        {
            activeRoom = player.GetComponentInParent<Room>();
        }
    }

    private void Awake()
    {
        if (!activeRoom && Player)
        {
            Room[] rooms = FindObjectsByType<Room>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] && rooms[i].ContainsPoint(Player.transform.position))
                {
                    activeRoom = rooms[i];
                    break;
                }
            }
        }

        ApplyActiveRoom(activeRoom);
    }

    private void OnValidate()
    {
        userVerticalFov = Mathf.Max(1f, userVerticalFov);
        minVerticalFov = Mathf.Max(1f, minVerticalFov);
        maxVerticalFov = Mathf.Max(minVerticalFov, maxVerticalFov);
        zoomStep = Mathf.Max(0.1f, zoomStep);
    }

    private void OnEnable()
    {
        zoomAction.SetEnabled(true);
    }

    private void OnDisable()
    {
        zoomAction.SetEnabled(false);
    }

    private void Update()
    {
        if (!zoomAction.IsAssigned())
        {
            return;
        }

        Vector2 scroll = zoomAction.ReadValueOrDefault<Vector2>();
        float scrollY = invertZoom ? -scroll.y : scroll.y;
        if (Mathf.Abs(scrollY) < 0.01f)
        {
            return;
        }

        SetUserZoom(userVerticalFov - Mathf.Sign(scrollY) * zoomStep);
    }

    public void SetUserZoom(float verticalFov)
    {
        userVerticalFov = Mathf.Clamp(verticalFov, minVerticalFov, maxVerticalFov);
        ApplyActiveRoom(activeRoom);
    }

    public bool TryTraverse(RoomPortal portal, float fadeDuration)
    {
        if (!portal || !portal.LinkedPortal || !portal.LinkedPortal.OwnerRoom)
        {
            return false;
        }

        return TryEnter(portal.LinkedPortal.OwnerRoom, portal.LinkedPortal.SpawnPoint.position, fadeDuration);
    }

    public bool TryEnter(RoomAnchor destinationAnchor, float fadeDuration)
    {
        return destinationAnchor && TryEnter(destinationAnchor.Room, destinationAnchor.transform.position, fadeDuration);
    }

    public bool TryEnter(Room room, Vector3 destinationPosition, float fadeDuration)
    {
        if (!room || transitionRoutine != null || !Player)
        {
            return false;
        }

        transitionRoutine = StartCoroutine(EnterRoutine(room, destinationPosition, fadeDuration));
        return true;
    }

    private IEnumerator EnterRoutine(Room room, Vector3 destinationPosition, float fadeDuration)
    {
        if (Fade)
        {
            yield return Fade.FadeOut(fadeDuration);
        }

        Player.TryWarp(destinationPosition);
        ApplyActiveRoom(room);

        if (Fade)
        {
            yield return Fade.FadeIn(fadeDuration);
        }

        transitionRoutine = null;
    }

    private void ApplyActiveRoom(Room room)
    {
        if (activeRoom == room)
        {
            room?.SetCameraLive(true, desiredVerticalFov: userVerticalFov, minVerticalFov: minVerticalFov, maxVerticalFov: maxVerticalFov);
            return;
        }

        activeRoom?.SetCameraLive(false, desiredVerticalFov: userVerticalFov, minVerticalFov: minVerticalFov, maxVerticalFov: maxVerticalFov);
        activeRoom = room;
        activeRoom?.SetCameraLive(true, desiredVerticalFov: userVerticalFov, minVerticalFov: minVerticalFov, maxVerticalFov: maxVerticalFov);
    }
}
