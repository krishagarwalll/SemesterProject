public readonly struct InteractionRequest
{
    public InteractionRequest(PointClickController actor, PointerContext pointer, InteractionTarget target, InteractionMode mode, Inventory inventory)
    {
        Actor = actor;
        Pointer = pointer;
        Target = target;
        Mode = mode;
        Inventory = inventory;
    }

    public PointClickController Actor { get; }
    public PointerContext Pointer { get; }
    public InteractionTarget Target { get; }
    public InteractionMode Mode { get; }
    public Inventory Inventory { get; }
    public InventoryItemDefinition SelectedItem => Inventory ? Inventory.SelectedItem : null;
}
