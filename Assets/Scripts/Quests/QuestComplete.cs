using UnityEngine;

public class QuestCompletionNPC : MonoBehaviour
{
    public Quest linkedQuest;
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    public void TriggerQuestCompletion()
    {
        if (linkedQuest == null) return;

        if (triggerOnce && hasTriggered) return;

        if (QuestController.Instance.isQuestActive(linkedQuest.questID) &&
            !QuestController.Instance.IsQuestReadyToHandIn(linkedQuest.questID))
        {
            QuestController.Instance.MarkQuestReadyToHandIn(linkedQuest.questID);
            hasTriggered = true;
        }
    }
}