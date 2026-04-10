using System.Collections.Generic;

public interface IInteractionActionProvider
{
    void GetActions(in InteractionContext context, List<InteractionAction> actions);
    bool Execute(in InteractionContext context, in InteractionAction action);
}
