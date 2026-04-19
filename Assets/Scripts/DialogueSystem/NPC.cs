using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NPC : MonoBehaviour, INPCInteractable
{
    private DialogueController dialogueUI;
    private int dialogueIndex;
    public NPCDialogue dialogueData;
    
    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState questState = QuestState.NotStarted;

    private bool isTyping, isDialogueActive;

    private void Start()
    {
        dialogueUI = DialogueController.Instance;
    }
    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    private void StartDialogue()
    {
        syncQuestState();

        if (questState == QuestState.NotStarted)
        {
            dialogueIndex = 0;
        }
        
        else if (questState == QuestState.InProgress)
        {
            Debug.Log("In Progress");
            dialogueIndex =  dialogueData.questInProgressIndex;
        }
        
        else if (questState == QuestState.Completed)
        {
            dialogueIndex = dialogueData.questCompletedIndex;
        }
        isDialogueActive = true;
    
        dialogueUI.SetNPCInfo(dialogueData.npcName);
        dialogueUI.ShowDialogue(true);

        DisplayCurrentLine();

    }

    /*private void syncQuestState()
    {
        if (dialogueData.quest == null) return;

        string questID = dialogueData.quest.questID;
        if (QuestController.Instance.IsQuestCompleted(questID) || QuestController.Instance.isQuestHandedIn(questID))
        {
            questState = QuestState.Completed;
        }
        if (QuestController.Instance.isQuestActive(questID))
        {
            questState = QuestState.InProgress;
        }

        else
        {
            questState = QuestState.NotStarted;
        }
    } */
    private void syncQuestState()
    {
        if (dialogueData.quest == null) return;

        string questID = dialogueData.quest.questID;

        if (QuestController.Instance.isQuestHandedIn(questID) ||
            QuestController.Instance.IsQuestReadyToHandIn(questID))
        {
            questState = QuestState.Completed;
        }
        else if (QuestController.Instance.isQuestActive(questID))
        {
            questState = QuestState.InProgress;
        }
        else
        {
            questState = QuestState.NotStarted;
        }
    }

    private void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();

            dialogueUI.SetDialogueText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
        }
        
        dialogueUI.ClearChoices();

        if (dialogueData.endDialogueLines.Length > dialogueIndex && dialogueData.endDialogueLines[dialogueIndex])
        {
            StopDialogue();
            return;
        }

        foreach (DialogueChoice dialogueChoice in dialogueData.choices)
        {
            if (dialogueChoice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(dialogueChoice);
                return;
            }
        }
        
        
        if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            StopDialogue();
        }
    }
    private IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");

        foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter);
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }
        isTyping = false;

        if (dialogueData.autoProgressLines != null &&
            dialogueIndex < dialogueData.autoProgressLines.Length &&
            dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void DisplayChoices(DialogueChoice choice)
    {
        for (int i = 0; i < choice.choices.Length; i++)
        {
            int nextIndex = choice.nextDialogueIndexes[i];
            bool givesQuest = choice.givesQuest[i];
            dialogueUI.CreateChoice(choice.choices[i],() => chooseOption(nextIndex, givesQuest));
        }
    }

    public void chooseOption(int nextIndex, bool givesQuest)
    {
        if (givesQuest)
        {
            QuestController.Instance.AcceptQuest(dialogueData.quest);
            questState = QuestState.InProgress;
        }
        dialogueIndex = nextIndex;
        dialogueUI.ClearChoices();
        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    public void StopDialogue()
    {
        QuestCompletionNPC completionNPC = GetComponent<QuestCompletionNPC>();
        if (completionNPC != null)
        {
            completionNPC.TriggerQuestCompletion();
            syncQuestState();
        }
        
        if (dialogueData.quest != null &&
            dialogueData.isQuestHandInNPC &&
            questState == QuestState.Completed &&
            !QuestController.Instance.isQuestHandedIn(dialogueData.quest.questID))
        {
            HandleQuestCompletion(dialogueData.quest);
        }

        StopAllCoroutines();
        isDialogueActive = false;
        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogue(false);
    }

    public void HandleQuestCompletion(Quest quest)
    {
        QuestController.Instance.CompleteQuest(quest.questID);
    }
}
