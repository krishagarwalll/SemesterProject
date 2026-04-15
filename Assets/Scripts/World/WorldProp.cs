using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
[RequireComponent(typeof(DragBody2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class WorldProp : MonoBehaviour, IInteractionActionProvider, IWorldDraggable
{
    [FieldHeader("References")]
    [SerializeField] private DragBody2D dragBody;

    [FieldHeader("Actions")]
    [SerializeField] private string dragLabel = "Drag";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string dragGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";

    [FieldHeader("Content")]
    [SerializeField, TextArea] private string inspectText;

    public bool SupportsDrag => DragBody && enabled && gameObject.activeInHierarchy;
    public bool IsDragging => DragBody && DragBody.IsDragging;

    private DragBody2D DragBody => dragBody ? dragBody : dragBody = GetComponent<DragBody2D>() ?? gameObject.GetOrAddComponent<DragBody2D>();

    private void Awake()
    {
        ApplyRuntimeSetup();
    }

    private void Reset()
    {
        dragBody = GetComponent<DragBody2D>() ?? gameObject.GetOrAddComponent<DragBody2D>();
        ApplyRuntimeSetup();
    }

    private void OnValidate()
    {
        ApplyRuntimeSetup();
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(this, InteractionMode.Drag, dragLabel, dragGlyphId, SupportsDrag, requiresApproach: false));
        if (!string.IsNullOrWhiteSpace(inspectText))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (action.Mode == InteractionMode.Inspect)
        {
            if (string.IsNullOrWhiteSpace(inspectText))
            {
                return false;
            }

            InteractionFeedback.Show(inspectText, this);
            return true;
        }

        if (action.Mode != InteractionMode.Drag || context.Pointer == null)
        {
            return false;
        }

        BeginDrag(context.Pointer);
        return IsDragging;
    }

    public bool CanStartDrag(PointerContext pointer)
    {
        return SupportsDrag && DragBody.CanStartDrag(pointer);
    }

    public void BeginDrag(PointerContext pointer)
    {
        DragBody.BeginDrag(pointer);
    }

    public void UpdateDrag(PointerContext pointer)
    {
        if (pointer)
        {
            DragBody.UpdateDragScreen(pointer.ScreenPosition);
        }
    }

    public void EndDrag()
    {
        DragBody.EndDrag(restoreInvalidPose: true);
    }

    private void ApplyRuntimeSetup()
    {
        this.ApplyWorldPresentation("RoomProp", "RoomProp");
    }
}
