using UnityEngine;

[DisallowMultipleComponent]
public class InteractionTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField, Min(0.01f)] private float interactionRadius = 1.25f;
    [SerializeField] private Room room;

    [Header("Cursor")]
    [SerializeField] private PointerCursorKind hoverCursorKind = PointerCursorKind.Interact;
    [SerializeField] private PointerCursorKind dragCursorKind = PointerCursorKind.Dragging;

    private Collider targetCollider;
    private MonoBehaviour[] behaviours;

    private Collider TargetCollider => this.ResolveComponent(ref targetCollider, true);
    public Transform InteractionPoint => interactionPoint ? interactionPoint : transform;
    public float InteractionRadius => interactionRadius;
    public Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    public bool SupportsDrag => TryGetDraggable(out IWorldDraggable draggable) && draggable.SupportsDrag;
    public bool SupportsClick => CanHandle(InteractionMode.Primary) || CanHandle(InteractionMode.Inspect) || CanHandle(InteractionMode.UseSelectedItem);
    public PointerCursorKind HoverCursorKind => SupportsDrag && hoverCursorKind == PointerCursorKind.Interact
        ? PointerCursorKind.DragReady
        : hoverCursorKind;
    public PointerCursorKind DragCursorKind => dragCursorKind;

    private MonoBehaviour[] Behaviours => behaviours ??= GetComponents<MonoBehaviour>();

    private void OnValidate()
    {
        targetCollider = null;
        behaviours = null;
        if (!interactionPoint)
        {
            interactionPoint = transform;
        }
    }

    public Vector3 GetApproachPoint(Vector3 actorPosition)
    {
        if (interactionPoint)
        {
            return interactionPoint.position;
        }

        return TargetCollider ? TargetCollider.ClosestPoint(actorPosition) : transform.position;
    }

    public bool IsInRange(Vector3 actorPosition, float extraDistance = 0f)
    {
        float radius = interactionRadius + extraDistance;
        Vector3 delta = GetApproachPoint(actorPosition) - actorPosition;
        delta.y = 0f;
        return delta.sqrMagnitude <= radius * radius;
    }

    public bool CanHandle(InteractionMode mode)
    {
        for (int i = 0; i < Behaviours.Length; i++)
        {
            if (Behaviours[i] is IInteractionHandler handler && handler.Supports(mode))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetHandler(in InteractionRequest request, out IInteractionHandler handler)
    {
        for (int i = 0; i < Behaviours.Length; i++)
        {
            if (Behaviours[i] is not IInteractionHandler candidate || !candidate.Supports(request.Mode) || !candidate.CanInteract(request))
            {
                continue;
            }

            handler = candidate;
            return true;
        }

        handler = null;
        return false;
    }

    public bool TryGetDraggable(out IWorldDraggable handler)
    {
        for (int i = 0; i < Behaviours.Length; i++)
        {
            if (Behaviours[i] is IWorldDraggable candidate && candidate.SupportsDrag)
            {
                handler = candidate;
                return true;
            }
        }

        handler = null;
        return false;
    }
}
