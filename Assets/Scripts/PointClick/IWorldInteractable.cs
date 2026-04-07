public interface IWorldInteractable
{
    bool CanInteract(PointClickController controller);
    void Interact(PointClickController controller);
}
