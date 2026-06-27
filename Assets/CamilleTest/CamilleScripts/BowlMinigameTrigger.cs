using System.Collections.Generic;
using UnityEngine;

public class BowlMinigameTrigger : MonoBehaviour, IInteractionActionProvider
{
    //place on bowl in kitchen
    private InteractionTarget target;

    private void Awake()
    {
        target = GetComponent<InteractionTarget>() ?? gameObject.AddComponent<InteractionTarget>();
        if (!GetComponentInChildren<Outline2D>(true))
        {
            gameObject.AddComponent<Outline2D>();
        }
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(this, InteractionMode.Primary, "Start", "Primary", FindKey.Instance != null, requiresApproach: false, priority: 20));
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (action.Mode != InteractionMode.Primary)
        {
            return false;
        }

        TriggerMinigame();
        return true;
    }

    public void TriggerMinigame()
    {
        Debug.Log("BOWL MINIGAME TRIGGERED");

        if (FindKey.Instance != null)
        {
            FindKey.Instance.Open();
        }
    }
}
