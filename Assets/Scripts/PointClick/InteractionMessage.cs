using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionMessage : MonoBehaviour, IInteractionActionProvider
{
    [SerializeField] private string primaryLabel = "Interact";
    [SerializeField] private string inspectLabel = "Inspect";
    [SerializeField] private string useItemLabel = "Use";
    [SerializeField] private string primaryGlyphId = "Primary";
    [SerializeField] private string inspectGlyphId = "Context";
    [SerializeField] private string useItemGlyphId = "Primary";
    [SerializeField, TextArea] private string primaryMessage;
    [SerializeField, TextArea] private string inspectMessage;
    [SerializeField, TextArea] private string selectedItemMessage;
    [SerializeField] private bool consumeSelectedItem;

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        if (!string.IsNullOrWhiteSpace(primaryMessage))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Primary, primaryLabel, primaryGlyphId));
        }

        if (!string.IsNullOrWhiteSpace(inspectMessage))
        {
            actions.Add(new InteractionAction(this, InteractionMode.Inspect, inspectLabel, inspectGlyphId, requiresApproach: false, priority: -10));
        }

        if (!string.IsNullOrWhiteSpace(selectedItemMessage))
        {
            bool enabled = context.SelectedItem;
            actions.Add(new InteractionAction(this, InteractionMode.UseSelectedItem, useItemLabel, useItemGlyphId, enabled));
        }
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        string message = action.Mode switch
        {
            InteractionMode.Primary => primaryMessage,
            InteractionMode.Inspect => inspectMessage,
            InteractionMode.UseSelectedItem => selectedItemMessage,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (action.Mode == InteractionMode.UseSelectedItem && consumeSelectedItem && context.Inventory && context.SelectedItem)
        {
            context.Inventory.TryRemove(context.SelectedItem);
        }

        InteractionFeedback.Show(message, this);
        return true;
    }
}
