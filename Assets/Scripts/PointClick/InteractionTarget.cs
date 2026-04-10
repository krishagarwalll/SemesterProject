using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionTarget : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField, Min(0.01f)] private float interactionRadius = 1.25f;
    [SerializeField] private Room room;
    [SerializeField] private PointerCursorKind hoverCursorKind = PointerCursorKind.Interact;
    [SerializeField] private PointerCursorKind dragCursorKind = PointerCursorKind.Dragging;

    private readonly List<InteractionAction> actionBuffer = new();
    private MonoBehaviour[] behaviours;
    private InteractableOutline outline;

    public Transform InteractionPoint => interactionPoint ? interactionPoint : transform;
    public float InteractionRadius => interactionRadius;
    public Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    public PointerCursorKind HoverCursorKind => SupportsDrag && hoverCursorKind == PointerCursorKind.Interact
        ? PointerCursorKind.DragReady
        : hoverCursorKind;
    public PointerCursorKind DragCursorKind => dragCursorKind;
    public bool SupportsDrag => TryGetDraggable(out _);

    private MonoBehaviour[] Behaviours => behaviours ??= GetComponents<MonoBehaviour>();
    private InteractableOutline Outline => outline ? outline : outline = GetComponentInChildren<InteractableOutline>(true);

    private void OnValidate()
    {
        if (!interactionPoint)
        {
            interactionPoint = transform;
        }

        behaviours = null;
        outline = null;
    }

    public void SetHovered(bool hovered)
    {
        Outline?.SetHighlighted(hovered);
    }

    public Vector3 GetApproachPoint(Vector3 actorPosition)
    {
        if (interactionPoint)
        {
            return interactionPoint.position;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].IsUsable())
            {
                return colliders[i].ClosestPoint(actorPosition);
            }
        }

        return transform.position;
    }

    public bool IsInRange(Vector3 actorPosition, float extraDistance = 0f)
    {
        float radius = interactionRadius + extraDistance;
        Vector3 delta = GetApproachPoint(actorPosition) - actorPosition;
        delta.y = 0f;
        return delta.sqrMagnitude <= radius * radius;
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Clear();
        for (int i = 0; i < Behaviours.Length; i++)
        {
            if (Behaviours[i] is IInteractionActionProvider provider)
            {
                provider.GetActions(context, actions);
            }
        }
    }

    public bool TryGetPreferredAction(in InteractionContext context, out InteractionAction action)
    {
        GetActions(context, actionBuffer);
        action = default;
        if (context.SelectedItem)
        {
            if (TryGetBestAction(InteractionMode.UseSelectedItem, out action))
            {
                return true;
            }
        }

        return TryGetBestAction(InteractionMode.Primary, out action);
    }

    public bool TryGetAction(in InteractionContext context, InteractionMode mode, out InteractionAction action)
    {
        GetActions(context, actionBuffer);
        return TryGetBestAction(mode, out action);
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (!action.Enabled || action.Provider == null)
        {
            return false;
        }

        return action.Provider.Execute(context, action);
    }

    public bool TryGetDraggable(out IWorldDraggable draggable)
    {
        for (int i = 0; i < Behaviours.Length; i++)
        {
            if (Behaviours[i] is IWorldDraggable candidate)
            {
                draggable = candidate;
                return true;
            }
        }

        draggable = null;
        return false;
    }

    private bool TryGetBestAction(InteractionMode mode, out InteractionAction action)
    {
        action = default;
        bool found = false;
        for (int i = 0; i < actionBuffer.Count; i++)
        {
            InteractionAction candidate = actionBuffer[i];
            if (!candidate.IsValid || candidate.Mode != mode)
            {
                continue;
            }

            if (!found || candidate.Priority > action.Priority || candidate.Enabled && !action.Enabled)
            {
                action = candidate;
                found = true;
            }
        }

        return found;
    }
}
