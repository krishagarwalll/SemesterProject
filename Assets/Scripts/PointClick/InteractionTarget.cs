using UnityEngine;

[DisallowMultipleComponent]
public class InteractionTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField, Min(0.01f)] private float interactionRadius = 1.25f;

    [Header("Cursor")]
    [SerializeField] private PointerCursorKind hoverCursorKind = PointerCursorKind.Interact;
    [SerializeField] private PointerCursorKind dragCursorKind = PointerCursorKind.Dragging;

    private Collider targetCollider;
    private MonoBehaviour[] behaviours;
    private IWorldInteractable interactable;
    private IWorldDraggable draggable;
    private bool resolvedInteractable;
    private bool resolvedDraggable;

    public Collider TargetCollider => this.ResolveComponent(ref targetCollider, true);
    public Transform InteractionPoint => interactionPoint ? interactionPoint : transform;
    public float InteractionRadius => interactionRadius;
    public bool SupportsClick => Interactable != null;
    public bool SupportsDrag => Draggable != null;
    public PointerCursorKind HoverCursorKind => SupportsDrag && hoverCursorKind == PointerCursorKind.Interact
        ? PointerCursorKind.DragReady
        : hoverCursorKind;
    public PointerCursorKind DragCursorKind => dragCursorKind;

    private IWorldInteractable Interactable => this.ResolveBehaviour(ref interactable, ref resolvedInteractable, ref behaviours);
    private IWorldDraggable Draggable => this.ResolveBehaviour(ref draggable, ref resolvedDraggable, ref behaviours);

    private void OnValidate()
    {
        targetCollider = null;
        behaviours = null;
        interactable = null;
        draggable = null;
        resolvedInteractable = false;
        resolvedDraggable = false;
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

    public bool TryGetInteractable(out IWorldInteractable handler)
    {
        handler = Interactable;
        return handler != null;
    }

    public bool TryGetDraggable(out IWorldDraggable handler)
    {
        handler = Draggable;
        return handler != null;
    }
}
