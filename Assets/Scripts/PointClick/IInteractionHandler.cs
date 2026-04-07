public interface IInteractionHandler
{
    bool Supports(InteractionMode mode);
    bool CanInteract(in InteractionRequest request);
    void Interact(in InteractionRequest request);
}
