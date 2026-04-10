public readonly struct InteractionAction
{
    public InteractionAction(
        IInteractionActionProvider provider,
        InteractionMode mode,
        string label,
        string glyphId,
        bool enabled = true,
        bool requiresApproach = true,
        int priority = 0)
    {
        Provider = provider;
        Mode = mode;
        Label = label;
        GlyphId = glyphId;
        Enabled = enabled;
        RequiresApproach = requiresApproach;
        Priority = priority;
    }

    public IInteractionActionProvider Provider { get; }
    public InteractionMode Mode { get; }
    public string Label { get; }
    public string GlyphId { get; }
    public bool Enabled { get; }
    public bool RequiresApproach { get; }
    public int Priority { get; }
    public bool IsValid => Provider != null && !string.IsNullOrWhiteSpace(Label);
}
