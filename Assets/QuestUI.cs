using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    public Transform questListContent;
    public GameObject questEntryPrefab;
    public GameObject objectiveTextPrefab;

    public Quest testQuest;
    public int testQuestAmount;
    private List<QuestProgress> testQuests = new();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < testQuestAmount; i++)
        {
            testQuests.Add(new QuestProgress(testQuest));
        }
    }

    public void UpdateQuestUI()
    {
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var quest in testQuests)
        {
            GameObject entry = Instantiate(questEntryPrefab, questListContent);
        }
    }
    // Update is called once per frame
    
}
