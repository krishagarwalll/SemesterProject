using UnityEngine;

[DisallowMultipleComponent]
public class RoomBoundsConstraint : MonoBehaviour
{
    [SerializeField] private Room room;
    [SerializeField] private bool lockZPosition = true;

    public Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    public bool LockZPosition => lockZPosition;

    private void Reset()
    {
        room = GetComponentInParent<Room>(true);
    }

    public Vector3 Clamp(Vector3 point)
    {
        if (!OwnerRoom)
        {
            return point;
        }

        Vector3 clampedPoint = OwnerRoom.ClampPosition(point);
        clampedPoint.y = point.y;
        if (lockZPosition)
        {
            clampedPoint.z = transform.position.z;
        }

        return clampedPoint;
    }

    public bool Contains(Vector3 point)
    {
        return !OwnerRoom || OwnerRoom.ContainsPoint(point);
    }
}
