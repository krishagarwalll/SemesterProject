public readonly struct InteractionContext
{
    public InteractionContext(PointClickController actor, PointerContext pointer, InteractionTarget target, Inventory inventory)
    {
        Actor = actor;
        Pointer = pointer;
        Target = target;
        Inventory = inventory;
    }

    public PointClickController Actor { get; }
    public PointerContext Pointer { get; }
    public InteractionTarget Target { get; }
    public Inventory Inventory { get; }
    public InventoryItemDefinition SelectedItem => Inventory ? Inventory.SelectedItem : null;
}
