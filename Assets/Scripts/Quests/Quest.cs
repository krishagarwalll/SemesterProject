using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "Quests")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    public string questDescription;
    public List<QuestObjective> questObjectives;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(questID))
        {
            questID = questName = Guid.NewGuid().ToString();
        }
    }
}

[System.Serializable]
public class QuestObjective
{
    public string description;
    public ObjectiveType type;
    public int requiredAmount;
    public int currentAmount;

    public bool isCompleted => currentAmount >= requiredAmount;
}

public enum ObjectiveType
{
    CollectItem,
    TalkToNPC,
    ReachLocation,
    Custom
}

[System.Serializable]
public class QuestProgress
{
    public Quest quest;
    public List<QuestObjective> objectives;

    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        objectives = new List<QuestObjective>();

        foreach (var obj in quest.questObjectives)
        {
            objectives.Add(new QuestObjective
            {
                description = obj.description,
                type = obj.type,
                requiredAmount = obj.requiredAmount,
                currentAmount = 0
            });
        }
    }    
        
    public bool isCompleted => objectives.TrueForAll(o => o.isCompleted);

    public string QuestID => quest.questID;
}
