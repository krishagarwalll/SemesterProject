public interface IWorldDraggable
{
    bool SupportsDrag { get; }
    bool IsDragging { get; }
    bool CanStartDrag(PointerContext pointer);
    void BeginDrag(PointerContext pointer);
    void UpdateDrag(PointerContext pointer);
    void EndDrag();
}
