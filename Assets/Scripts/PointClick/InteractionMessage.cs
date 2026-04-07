using UnityEngine;

[DisallowMultipleComponent]
public class InteractionMessage : MonoBehaviour, IInteractionHandler
{
    [SerializeField, TextArea] private string primaryMessage;
    [SerializeField, TextArea] private string inspectMessage;
    [SerializeField, TextArea] private string selectedItemMessage;
    [SerializeField] private bool consumeSelectedItem;

    public bool Supports(InteractionMode mode)
    {
        return mode switch
        {
            InteractionMode.Primary => !string.IsNullOrWhiteSpace(primaryMessage),
            InteractionMode.Inspect => !string.IsNullOrWhiteSpace(inspectMessage),
            InteractionMode.UseSelectedItem => !string.IsNullOrWhiteSpace(selectedItemMessage),
            _ => false
        };
    }

    public bool CanInteract(in InteractionRequest request)
    {
        return request.Mode != InteractionMode.UseSelectedItem || request.SelectedItem;
    }

    public void Interact(in InteractionRequest request)
    {
        string message = request.Mode switch
        {
            InteractionMode.Primary => primaryMessage,
            InteractionMode.Inspect => inspectMessage,
            InteractionMode.UseSelectedItem => selectedItemMessage,
            _ => string.Empty
        };

        if (request.Mode == InteractionMode.UseSelectedItem && consumeSelectedItem && request.Inventory && request.SelectedItem)
        {
            request.Inventory.TryRemove(request.SelectedItem);
        }

        InteractionFeedback.Show(message, this);
    }
}
