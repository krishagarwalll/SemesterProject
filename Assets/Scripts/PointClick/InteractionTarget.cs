using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionTarget : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform approachPoint;
    [SerializeField] private InteractionDistancePreset interactionDistancePreset = InteractionDistancePreset.Standard;
    [SerializeField] private bool useInteractionRadiusOverride;
    [SerializeField, Min(0.01f)] private float interactionRadiusOverride = 1.25f;
    [SerializeField] private int selectionPriority;
    [SerializeField] private Room room;
    [SerializeField] private PointerCursorKind hoverCursorKind = PointerCursorKind.Interact;
    [SerializeField] private PointerCursorKind dragCursorKind = PointerCursorKind.Dragging;

    private readonly List<InteractionAction> actionBuffer = new();
    private MonoBehaviour[] behaviours;
    private InteractableOutline outline;
    private Collider2D[] colliders2D;
    private Collider[] colliders3D;
    private Renderer[] renderers;

    public Transform InteractionPoint => interactionPoint ? interactionPoint : transform;
    public Transform ApproachPoint => approachPoint ? approachPoint : InteractionPoint;
    public float InteractionRadius => useInteractionRadiusOverride ? interactionRadiusOverride : GetPresetRadius(GetEffectiveDistancePreset());
    public int SelectionPriority => selectionPriority != 0 ? selectionPriority : GetSuggestedSelectionPriority();
    public Room OwnerRoom => room ? room : room = GetComponentInParent<Room>(true);
    public PointerCursorKind HoverCursorKind => hoverCursorKind;
    public PointerCursorKind DragCursorKind => dragCursorKind;
    public bool SupportsDrag => TryGetDraggable(out _);

    private MonoBehaviour[] Behaviours => behaviours ??= GetComponents<MonoBehaviour>();
    private InteractableOutline Outline => outline ? outline : outline = GetComponentInChildren<InteractableOutline>(true);
    private Collider2D[] Colliders2D => colliders2D ??= GetComponentsInChildren<Collider2D>(true);
    private Collider[] Colliders3D => colliders3D ??= GetComponentsInChildren<Collider>(true);
    private Renderer[] Renderers => renderers ??= GetComponentsInChildren<Renderer>(true);

    private void OnValidate()
    {
        if (!interactionPoint)
        {
            interactionPoint = transform;
        }

        interactionRadiusOverride = Mathf.Max(0.01f, interactionRadiusOverride);

        behaviours = null;
        outline = null;
        colliders2D = null;
        colliders3D = null;
        renderers = null;
    }

    public void SetHovered(bool hovered)
    {
        Outline?.SetHighlighted(hovered);
    }

    public Vector3 GetApproachPoint(Vector3 actorPosition)
    {
        if (approachPoint)
        {
            Vector3 explicitPoint = approachPoint.position;
            explicitPoint.z = transform.position.z;
            return explicitPoint;
        }

        Vector2 actorPoint = actorPosition;
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            if (!Colliders2D[i].IsUsable())
            {
                continue;
            }

            Vector2 closestPoint = Colliders2D[i].ClosestPoint(actorPoint);
            return new Vector3(closestPoint.x, closestPoint.y, transform.position.z);
        }

        for (int i = 0; i < Colliders3D.Length; i++)
        {
            if (!Colliders3D[i].IsUsable())
            {
                continue;
            }

            Vector3 closestPoint = Colliders3D[i].ClosestPoint(actorPosition);
            closestPoint.z = transform.position.z;
            return closestPoint;
        }

        Vector3 point = InteractionPoint.position;
        point.z = transform.position.z;
        return point;
    }

    public bool IsInRange(Vector3 actorPosition, float extraDistance = 0f)
    {
        float radius = InteractionRadius + extraDistance;
        Vector2 delta = (Vector2)(GetApproachPoint(actorPosition) - actorPosition);
        return delta.sqrMagnitude <= radius * radius;
    }

    public float GetPointerSelectionRadius(float minimumRadius = 0.25f)
    {
        float visualRadius = GetVisualRadius();
        return Mathf.Max(minimumRadius, InteractionRadius, visualRadius);
    }

    public float GetVisualRadius()
    {
        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < Colliders2D.Length; i++)
        {
            if (!Colliders2D[i].IsUsable())
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = Colliders2D[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(Colliders2D[i].bounds.min);
                bounds.Encapsulate(Colliders2D[i].bounds.max);
            }
        }

        if (!hasBounds)
        {
            for (int i = 0; i < Renderers.Length; i++)
            {
                if (!Renderers[i])
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = Renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(Renderers[i].bounds.min);
                    bounds.Encapsulate(Renderers[i].bounds.max);
                }
            }
        }

        if (!hasBounds)
        {
            return 0.35f;
        }

        Vector2 extents = bounds.extents;
        return Mathf.Max(0.35f, Mathf.Max(extents.x, extents.y));
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
        if (context.SelectedItem && TryGetBestAction(InteractionMode.UseSelectedItem, out action))
        {
            return true;
        }

        return TryGetBestAction(InteractionMode.Primary, out action);
    }

    public bool TryGetPrimaryAction(in InteractionContext context, out InteractionAction action)
    {
        return TryGetPreferredAction(context, out action);
    }

    public bool TryGetPromptAction(in InteractionContext context, out InteractionAction action)
    {
        if (TryGetPrimaryAction(context, out action) && action.Enabled)
        {
            return true;
        }

        if (TryGetAction(context, InteractionMode.Drag, out action) && action.Enabled)
        {
            return true;
        }

        action = default;
        return false;
    }

    public bool TryGetAction(in InteractionContext context, InteractionMode mode, out InteractionAction action)
    {
        GetActions(context, actionBuffer);
        return TryGetBestAction(mode, out action);
    }

    public bool HasAnyEnabledAction(in InteractionContext context)
    {
        GetActions(context, actionBuffer);
        for (int i = 0; i < actionBuffer.Count; i++)
        {
            if (actionBuffer[i].IsValid && actionBuffer[i].Enabled)
            {
                return true;
            }
        }

        return false;
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        return action.Enabled && action.Provider != null && action.Provider.Execute(context, action);
    }

    public bool ContainsPoint(Vector2 point)
    {
        for (int i = 0; i < Colliders2D.Length; i++)
        {
            if (Colliders2D[i].IsUsable() && Colliders2D[i].OverlapPoint(point))
            {
                return true;
            }
        }

        for (int i = 0; i < Colliders3D.Length; i++)
        {
            Collider collider = Colliders3D[i];
            if (!collider.IsUsable())
            {
                continue;
            }

            Vector3 closest = collider.ClosestPoint(new Vector3(point.x, point.y, transform.position.z));
            if (((Vector2)closest - point).sqrMagnitude <= 0.0004f)
            {
                return true;
            }
        }

        return false;
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

    private int GetSuggestedSelectionPriority()
    {
        if (TryGetComponent(out RoomPortal _))
        {
            return 80;
        }

        if (TryGetComponent(out PickupItem _))
        {
            return 120;
        }

        if (TryGetComponent(out DraggableProp _))
        {
            return 100;
        }

        if (TryGetComponent(out InteractionMessage _))
        {
            return 20;
        }

        return 0;
    }

    private InteractionDistancePreset GetEffectiveDistancePreset()
    {
        if (useInteractionRadiusOverride)
        {
            return interactionDistancePreset;
        }

        if (interactionDistancePreset != InteractionDistancePreset.Standard)
        {
            return interactionDistancePreset;
        }

        if (TryGetComponent(out RoomPortal _))
        {
            return InteractionDistancePreset.Portal;
        }

        if (TryGetComponent(out PickupItem _) || TryGetComponent(out DraggableProp _))
        {
            return InteractionDistancePreset.Reach;
        }

        return InteractionDistancePreset.Standard;
    }

    private static float GetPresetRadius(InteractionDistancePreset preset)
    {
        return preset switch
        {
            InteractionDistancePreset.Touch => 0.9f,
            InteractionDistancePreset.Standard => 1.35f,
            InteractionDistancePreset.Reach => 1.85f,
            InteractionDistancePreset.Portal => 2.25f,
            _ => 1.35f
        };
    }
}

public enum InteractionDistancePreset
{
    Touch = 0,
    Standard = 1,
    Reach = 2,
    Portal = 3
}
