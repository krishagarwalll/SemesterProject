using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionMessage : MonoBehaviour, IInteractionActionProvider
{
    [SerializeField] private string primaryLabel = "Interact";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string primaryGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField, TextArea] private string primaryMessage;
    [SerializeField, TextArea] private string inspectMessage;

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        if (!string.IsNullOrWhiteSpace(primaryMessage))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Primary, primaryLabel, primaryGlyphId, requiresApproach: false));
        }

        if (!string.IsNullOrWhiteSpace(inspectMessage))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        string message = action.Mode switch
        {
            InteractionMode.Primary => primaryMessage,
            InteractionMode.Inspect => inspectMessage,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(message)) return false;
        InteractionFeedback.Show(message, this);
        return true;
    }
}
