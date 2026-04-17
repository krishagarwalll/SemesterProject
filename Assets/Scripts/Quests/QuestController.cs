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
        else Destroy(this);

        questUI = FindObjectOfType<QuestUI>();
    }

    public void AcceptQuest(Quest quest)
    {
        if (isQuestActive(quest.questID)) return;
        
        activateQuests.Add(new QuestProgress(quest));
        
        questUI.UpdateQuestUI();
    }

    public bool isQuestActive(string questID) => activateQuests.Exists(q => q.QuestID == questID);

    public bool IsQuestCompleted(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);
        return quest != null && quest.objectives.TrueForAll(o => o.isCompleted);
    }

    public void CompleteQuest(string questID)
    {
        QuestProgress quest = activateQuests.Find(q => q.QuestID == questID);
        if (quest != null)
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


}
