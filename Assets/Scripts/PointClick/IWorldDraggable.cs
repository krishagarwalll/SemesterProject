public interface IWorldDraggable
{
    bool IsDragging { get; }
    bool CanStartDrag(PointerContext pointer);
    void BeginDrag(PointerContext pointer);
    void EndDrag();
}
