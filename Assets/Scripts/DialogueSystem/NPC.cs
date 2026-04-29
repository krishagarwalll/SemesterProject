using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class NPC : MonoBehaviour, INPCInteractable, IInteractionActionProvider
{
    private DialogueController dialogueUI;
    private int dialogueIndex;
    public NPCDialogue dialogueData;
    [SerializeField] private string talkLabel = "Talk";
    [SerializeField] private string continueLabel = "Continue";
    [SerializeField] private string glyphId = "Primary";
    [SerializeField] private int actionPriority = 20;
    
    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState questState = QuestState.NotStarted;

    private bool isTyping, isDialogueActive;

    private void Awake()
    {
        EnsureInteractionSetup();
    }

    private void Start()
    {
        dialogueUI = DialogueController.Instance;
    }

    private void Reset()
    {
        EnsureInteractionSetup();
    }

    public bool CanInteract()
    {
        return enabled && gameObject.activeInHierarchy && dialogueData;
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    public void GetActions(in InteractionContext context, List<InteractionAction> actions)
    {
        actions.Add(new InteractionAction(
            this,
            InteractionMode.Primary,
            GetActionLabel(),
            glyphId,
            CanInteract(),
            requiresApproach: !isDialogueActive,
            priority: actionPriority));
    }

    public bool Execute(in InteractionContext context, in InteractionAction action)
    {
        if (action.Mode != InteractionMode.Primary || !action.Enabled)
        {
            return false;
        }

        Interact();
        return true;
    }

    private void StartDialogue()
    {
        dialogueUI = dialogueUI ? dialogueUI : DialogueController.Instance;
        if (!dialogueUI || !dialogueData || dialogueData.dialogueLines == null || dialogueData.dialogueLines.Length == 0)
        {
            return;
        }

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

        dialogueIndex = Mathf.Clamp(dialogueIndex, 0, dialogueData.dialogueLines.Length - 1);
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
        if (dialogueData == null || dialogueData.quest == null || QuestController.Instance == null) return;

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
        if (!dialogueUI || dialogueData == null || dialogueData.dialogueLines == null || dialogueData.dialogueLines.Length == 0)
        {
            StopDialogue();
            return;
        }

        if (isTyping)
        {
            StopAllCoroutines();

            dialogueUI.SetDialogueText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
        }
        
        dialogueUI.ClearChoices();

        if (dialogueData.endDialogueLines != null &&
            dialogueData.endDialogueLines.Length > dialogueIndex &&
            dialogueData.endDialogueLines[dialogueIndex])
        {
            StopDialogue();
            return;
        }

        if (dialogueData.choices != null)
        {
            foreach (DialogueChoice dialogueChoice in dialogueData.choices)
            {
                if (dialogueChoice.dialogueIndex == dialogueIndex)
                {
                    DisplayChoices(dialogueChoice);
                    return;
                }
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
        if (!dialogueUI || choice == null || choice.choices == null || choice.nextDialogueIndexes == null || choice.givesQuest == null)
        {
            return;
        }

        for (int i = 0; i < choice.choices.Length; i++)
        {
            if (i >= choice.nextDialogueIndexes.Length || i >= choice.givesQuest.Length)
            {
                continue;
            }

            int nextIndex = choice.nextDialogueIndexes[i];
            bool givesQuest = choice.givesQuest[i];
            dialogueUI.CreateChoice(choice.choices[i],() => chooseOption(nextIndex, givesQuest));
        }
    }

    public void chooseOption(int nextIndex, bool givesQuest)
    {
        if (givesQuest)
        {
            if (QuestController.Instance != null && dialogueData && dialogueData.quest)
            {
                QuestController.Instance.AcceptQuest(dialogueData.quest);
            }
            questState = QuestState.InProgress;
        }
        dialogueIndex = dialogueData != null && dialogueData.dialogueLines != null
            ? Mathf.Clamp(nextIndex, 0, dialogueData.dialogueLines.Length - 1)
            : nextIndex;
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
        
        if (dialogueData != null &&
            dialogueData.quest != null &&
            dialogueData.isQuestHandInNPC &&
            questState == QuestState.Completed &&
            QuestController.Instance != null &&
            !QuestController.Instance.isQuestHandedIn(dialogueData.quest.questID))
        {
            HandleQuestCompletion(dialogueData.quest);
        }

        StopAllCoroutines();
        isDialogueActive = false;
        if (dialogueUI)
        {
            dialogueUI.SetDialogueText("");
            dialogueUI.ClearChoices();
            dialogueUI.ShowDialogue(false);
        }
    }

    public void HandleQuestCompletion(Quest quest)
    {
        if (QuestController.Instance != null && quest)
        {
            QuestController.Instance.CompleteQuest(quest.questID);
        }
    }

    private void EnsureInteractionSetup()
    {
        if (!TryGetComponent(out InteractionTarget _))
        {
            gameObject.AddComponent<InteractionTarget>();
        }

        if (!GetComponentInChildren<Outline2D>(true))
        {
            gameObject.AddComponent<Outline2D>();
        }
    }

    private string GetActionLabel()
    {
        string label = isDialogueActive ? continueLabel : talkLabel;
        return string.IsNullOrWhiteSpace(label) ? "Talk" : label;
    }
}
