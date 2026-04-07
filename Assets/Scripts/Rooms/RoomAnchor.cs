using UnityEngine;

[DisallowMultipleComponent]
public class RoomAnchor : MonoBehaviour
{
    [SerializeField] private string anchorId;

    private Room room;

    public string AnchorId => string.IsNullOrWhiteSpace(anchorId) ? name : anchorId;
    public Room Room => room ? room : room = GetComponentInParent<Room>(true);
}
