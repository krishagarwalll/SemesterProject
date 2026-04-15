using UnityEngine;

[System.Obsolete("Legacy room exit interaction retained for older scenes.", false)]
[DisallowMultipleComponent]
public class RoomExit : MonoBehaviour, IInteractionHandler
{
    [SerializeField] private RoomAnchor destinationAnchor;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [SerializeField, TextArea] private string inspectText;

    private RoomTransitionService transitionService;

    private RoomTransitionService TransitionService => this.ResolveSceneComponent(ref transitionService);
    private RoomAnchor DestinationAnchor => destinationAnchor;

    public bool Supports(InteractionMode mode)
    {
        return mode switch
        {
            InteractionMode.Primary => DestinationAnchor,
            InteractionMode.Inspect => !string.IsNullOrWhiteSpace(inspectText),
            _ => false
        };
    }

    public bool CanInteract(in InteractionRequest request)
    {
        return request.Mode switch
        {
            InteractionMode.Primary => DestinationAnchor && TransitionService,
            InteractionMode.Inspect => !string.IsNullOrWhiteSpace(inspectText),
            _ => false
        };
    }

    public void Interact(in InteractionRequest request)
    {
        switch (request.Mode)
        {
            case InteractionMode.Primary:
                TransitionService.TryEnter(DestinationAnchor, fadeDuration);
                break;

            case InteractionMode.Inspect:
                InteractionFeedback.Show(inspectText, this);
                break;
        }
    }
}
