using System.Collections.Generic;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }

    public List<QuestProgress> activateQuests = new();
    private QuestUI questUI;

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


}
