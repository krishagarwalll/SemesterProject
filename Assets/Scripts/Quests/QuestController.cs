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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        questUI = FindFirstObjectByType<QuestUI>(FindObjectsInactive.Include);

        Inventory.AnyItemAdded += HandleInventoryItemAdded;
        Inventory.AnyInventoryChanged += HandleInventoryChanged;
    }

    private void OnDestroy()
    {
        Inventory.AnyItemAdded -= HandleInventoryItemAdded;
        Inventory.AnyInventoryChanged -= HandleInventoryChanged;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleInventoryItemAdded(InventoryItemDefinition definition, int amount)
    {
        if (definition == null || amount <= 0) return;
        SyncCollectObjectivesFromInventory();
    }

    private void HandleInventoryChanged()
    {
        SyncCollectObjectivesFromInventory();
    }

    public int AdvanceCollectObjectives(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return 0;

        int hits = 0;
        for (int i = 0; i < activateQuests.Count; i++)
        {
            QuestProgress qp = activateQuests[i];
            for (int j = 0; j < qp.objectives.Count; j++)
            {
                QuestObjective obj = qp.objectives[j];
                if (obj.type != ObjectiveType.CollectItem) continue;
                if (obj.itemId != itemId) continue;

                obj.currentAmount = Mathf.Min(obj.currentAmount + amount, obj.requiredAmount);
                hits++;
            }
        }

        if (hits > 0) questUI?.UpdateQuestUI();
        return hits;
    }

    public void AcceptQuest(Quest quest)
    {
        if (quest == null) return;
        if (isQuestActive(quest.questID) || isQuestHandedIn(quest.questID)) return;

        activateQuests.Add(new QuestProgress(quest));
        SyncCollectObjectivesFromInventory();
        questUI?.UpdateQuestUI();
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
            questUI?.UpdateQuestUI();
        }
    }

    public void CompleteQuest(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);

        if (quest != null && quest.readyToHandIn)
        {
            if (!handInQuestIDs.Contains(questID))
            {
                handInQuestIDs.Add(questID);
            }

            activateQuests.Remove(quest);
            questUI?.UpdateQuestUI();
        }
    }

    public bool isQuestHandedIn(string questID)
    {
        return handInQuestIDs.Contains(questID);
    }

    // ── Developer query API (PascalCase, consistent naming) ──────

    public bool IsQuestActive(string questID) => isQuestActive(questID);
    public bool IsQuestHandedIn(string questID) => isQuestHandedIn(questID);

    public bool SetObjectiveDescription(string questID, int objectiveIndex, string newDescription)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);
        if (quest == null) return false;
        if (objectiveIndex < 0 || objectiveIndex >= quest.objectives.Count) return false;

        QuestObjective objective = quest.objectives[objectiveIndex];
        if (objective.description == newDescription) return false;

        objective.description = newDescription;
        questUI?.UpdateQuestUI();
        return true;
    }

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

    public void ClearRuntimeState()
    {
        activateQuests.Clear();
        handInQuestIDs.Clear();
        questUI?.UpdateQuestUI();
    }

    public bool RestoreQuestProgress(Quest quest, IReadOnlyList<int> objectiveAmounts, bool readyToHandIn)
    {
        if (quest == null || string.IsNullOrWhiteSpace(quest.questID) || isQuestHandedIn(quest.questID))
        {
            return false;
        }

        QuestProgress progress = activateQuests.Find(q => q.QuestID == quest.questID);
        if (progress == null)
        {
            progress = new QuestProgress(quest);
            activateQuests.Add(progress);
        }

        if (objectiveAmounts != null)
        {
            int count = Mathf.Min(progress.objectives.Count, objectiveAmounts.Count);
            for (int i = 0; i < count; i++)
            {
                QuestObjective objective = progress.objectives[i];
                objective.currentAmount = Mathf.Clamp(objectiveAmounts[i], 0, objective.requiredAmount);
            }
        }

        progress.readyToHandIn = readyToHandIn;
        SyncCollectObjectivesFromInventory();
        questUI?.UpdateQuestUI();
        return true;
    }

    public void MarkQuestHandedIn(string questID)
    {
        if (string.IsNullOrWhiteSpace(questID) || handInQuestIDs.Contains(questID))
        {
            return;
        }

        activateQuests.RemoveAll(q => q.QuestID == questID);
        handInQuestIDs.Add(questID);
        questUI?.UpdateQuestUI();
    }

    private void SyncCollectObjectivesFromInventory()
    {
        Inventory inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (!inventory)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < activateQuests.Count; i++)
        {
            QuestProgress progress = activateQuests[i];
            for (int j = 0; j < progress.objectives.Count; j++)
            {
                QuestObjective objective = progress.objectives[j];
                if (objective.type != ObjectiveType.CollectItem || string.IsNullOrWhiteSpace(objective.itemId))
                {
                    continue;
                }

                int count = Mathf.Clamp(inventory.CountItem(objective.itemId), 0, objective.requiredAmount);
                if (objective.currentAmount == count)
                {
                    continue;
                }

                objective.currentAmount = count;
                changed = true;
            }
        }

        if (changed)
        {
            questUI?.UpdateQuestUI();
        }
    }
}
