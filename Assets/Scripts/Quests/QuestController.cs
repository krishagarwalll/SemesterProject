using System.Collections.Generic;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }

    public List<QuestProgress> activateQuests = new();
    private QuestUI questUI;

    public List<string> handInQuestIDs = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        questUI = FindFirstObjectByType<QuestUI>(FindObjectsInactive.Include);
    }

    public void AcceptQuest(Quest quest)
    {
        if (quest == null) return;
        if (isQuestActive(quest.questID) || isQuestHandedIn(quest.questID)) return;

        activateQuests.Add(new QuestProgress(quest));
        questUI.UpdateQuestUI();
    }

    public bool isQuestActive(string questID)
    {
        return activateQuests.Exists(q => q.QuestID == questID);
    }

    public bool IsQuestCompleted(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);
        return quest != null && quest.isCompleted;
    }

    public bool IsQuestReadyToHandIn(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);
        return quest != null && quest.readyToHandIn;
    }

    public void MarkQuestReadyToHandIn(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);

        if (quest != null)
        {
            quest.readyToHandIn = true;
            questUI.UpdateQuestUI();
        }
    }

    public void CompleteQuest(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);

        if (quest != null && quest.readyToHandIn)
        {
            handInQuestIDs.Add(questID);
            activateQuests.Remove(quest);
            questUI.UpdateQuestUI();
        }
    }

    public bool isQuestHandedIn(string questID)
    {
        return handInQuestIDs.Contains(questID);
    }

    // ── Developer query API (PascalCase, consistent naming) ──────

    public bool IsQuestActive(string questID) => isQuestActive(questID);
    public bool IsQuestHandedIn(string questID) => isQuestHandedIn(questID);

    public bool TryAdvanceObjective(string questID, string objectiveDescription, int amount = 1)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);
        if (quest == null) return false;
        QuestObjective objective = quest.objectives.Find(o => o.description == objectiveDescription);
        if (objective == null) return false;
        objective.currentAmount = Mathf.Min(objective.currentAmount + amount, objective.requiredAmount);
        questUI?.UpdateQuestUI();
        return true;
    }
}