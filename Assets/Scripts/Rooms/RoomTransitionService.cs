using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class RoomTransitionService : MonoBehaviour
{
    [SerializeField] private PointClickController player;
    [SerializeField] private ScreenFade screenFade;
    [SerializeField] private Room activeRoom;

    private Coroutine transitionRoutine;

    private PointClickController Player => player ? player : player = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
    private ScreenFade Fade => screenFade ? screenFade : screenFade = FindFirstObjectByType<ScreenFade>(FindObjectsInactive.Include);
    public Room ActiveRoom => activeRoom;

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

    public bool TryEnter(RoomAnchor destinationAnchor, float fadeDuration)
    {
        if (!destinationAnchor)
        {
            return false;
        }

        return TryEnter(destinationAnchor.Room, destinationAnchor, fadeDuration);
    }

    public bool TryEnter(Room room, string anchorId, float fadeDuration)
    {
        return room && room.TryGetAnchor(anchorId, out RoomAnchor anchor) && TryEnter(room, anchor, fadeDuration);
    }

    public bool TryEnter(Room room, RoomAnchor anchor, float fadeDuration)
    {
        if (!room || !anchor || transitionRoutine != null || !Player)
        {
            return false;
        }

        transitionRoutine = StartCoroutine(EnterRoutine(room, anchor, fadeDuration));
        return true;
    }

    private IEnumerator EnterRoutine(Room room, RoomAnchor anchor, float fadeDuration)
    {
        if (Fade)
        {
            yield return Fade.FadeOut(fadeDuration);
        }

        Player.TryWarp(anchor.transform.position);
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
            room?.SetCameraLive(true);
            return;
        }

        activeRoom?.SetCameraLive(false);
        activeRoom = room;
        activeRoom?.SetCameraLive(true);
    }
}
