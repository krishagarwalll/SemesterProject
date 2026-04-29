public readonly struct InteractionContext
{
    public InteractionContext(PoptropicaController actor, PointerContext pointer, InteractionTarget target, Inventory inventory)
    {
        Actor = actor;
        Pointer = pointer;
        Target = target;
        Inventory = inventory;
    }

    public PoptropicaController Actor { get; }
    public PointerContext Pointer { get; }
    public InteractionTarget Target { get; }
    public Inventory Inventory { get; }
}
