using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class RoomTransitionService : MonoBehaviour
{
    [SerializeField] private PoptropicaController player;
    [SerializeField] private ScreenFade screenFade;
    [SerializeField] private Room activeRoom;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField, Min(0.5f)] private float userOrthographicSize = 1.45f;
    [SerializeField, Min(0.5f)] private float minOrthographicSize = 1.25f;
    [SerializeField, Min(0.5f)] private float maxOrthographicSize = 5f;
    [SerializeField, Min(0.1f)] private float zoomStep = 0.25f;
    [SerializeField] private bool invertZoom;

    private Coroutine transitionRoutine;

    private PoptropicaController Player => player ? player : player = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
    private ScreenFade Fade => screenFade ? screenFade : screenFade = FindFirstObjectByType<ScreenFade>(FindObjectsInactive.Include);
    public Room ActiveRoom => activeRoom;
    public float UserOrthographicSize => userOrthographicSize;
    public float MinOrthographicSize => minOrthographicSize;
    public float MaxOrthographicSize => maxOrthographicSize;

    private void Reset()
    {
        player = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
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
        userOrthographicSize = Mathf.Max(0.5f, userOrthographicSize);
        minOrthographicSize = Mathf.Max(0.5f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
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

        SetUserZoom(userOrthographicSize - Mathf.Sign(scrollY) * zoomStep);
    }

    public void SetUserZoom(float orthographicSize)
    {
        userOrthographicSize = Mathf.Clamp(orthographicSize, minOrthographicSize, maxOrthographicSize);
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

        ApplyActiveRoom(room);
        Player.TryWarp(destinationPosition);

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
            room?.SetCameraLive(true, desiredOrthographicSize: userOrthographicSize, minOrthographicSize: minOrthographicSize, maxOrthographicSize: maxOrthographicSize);
            return;
        }

        activeRoom?.SetCameraLive(false, desiredOrthographicSize: userOrthographicSize, minOrthographicSize: minOrthographicSize, maxOrthographicSize: maxOrthographicSize);
        activeRoom = room;
        activeRoom?.SetCameraLive(true, desiredOrthographicSize: userOrthographicSize, minOrthographicSize: minOrthographicSize, maxOrthographicSize: maxOrthographicSize);

        if (room && AudioManager.Instance)
        {
            if (room.MusicClip) AudioManager.Instance.PlayMusic(room.MusicClip);
            else AudioManager.Instance.StopMusic();
        }
    }
}
