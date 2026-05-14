using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionTarget))]
public class NPC : MonoBehaviour, INPCInteractable, IInteractionActionProvider
{
    [SerializeField] private string talkLabel = "Talk";
    [SerializeField] private string continueLabel = "Continue";
    [SerializeField] private string glyphId = "Primary";
    [SerializeField] private int actionPriority = 20;
    [SerializeField, Min(0f)] private float dismissDistance = 4f;

    public NPCDialogue dialogueData;

    private DialogueController dialogueUI;
    private PoptropicaController cachedPlayer;
    private int dialogueIndex;
    private bool isTyping;
    private bool isDialogueActive;

    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState questState = QuestState.NotStarted;

    private PoptropicaController Player => cachedPlayer
        ? cachedPlayer
        : cachedPlayer = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);

    private void Awake() => EnsureInteractionSetup();
    private void Reset() => EnsureInteractionSetup();

    private void Start()
    {
        dialogueUI = DialogueController.Instance;
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // Space or Enter to fast-forward / advance
        bool advancePressed = Keyboard.current != null
            ? Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame
            : Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

        if (advancePressed)
        {
            Interact();
            return;
        }

        // Auto-dismiss when player walks away
        if (dismissDistance > 0f && Player)
        {
            float dist = Vector2.Distance(transform.position, Player.transform.position);
            if (dist > dismissDistance)
                StopDialogue();
        }
    }

    public bool CanInteract() => enabled && gameObject.activeInHierarchy && dialogueData;

    public void Interact()
    {
        if (!CanInteract()) return;
        if (isDialogueActive) NextLine();
        else StartDialogue();
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
        if (action.Mode != InteractionMode.Primary || !action.Enabled) return false;
        Interact();
        return true;
    }

    private void StartDialogue()
    {
        dialogueUI = dialogueUI ? dialogueUI : DialogueController.Instance;
        if (!dialogueUI || !dialogueData || dialogueData.dialogueLines == null || dialogueData.dialogueLines.Length == 0)
            return;

        SyncQuestState();

        if (questState == QuestState.NotStarted && dialogueData.autoGiveQuestOnStart
            && dialogueData.quest && QuestController.Instance != null)
        {
            QuestController.Instance.AcceptQuest(dialogueData.quest);
            SyncQuestState();
        }

        dialogueIndex = questState switch
        {
            QuestState.InProgress => dialogueData.questInProgressIndex,
            QuestState.Completed => dialogueData.questCompletedIndex,
            _ => 0
        };

        dialogueIndex = Mathf.Clamp(dialogueIndex, 0, dialogueData.dialogueLines.Length - 1);
        isDialogueActive = true;

        dialogueUI.SetNPCInfo(dialogueData.npcName);
        dialogueUI.ShowDialogue(true);
        DisplayCurrentLine();
    }

    private void NextLine()
    {
        if (!dialogueUI || dialogueData?.dialogueLines == null || dialogueData.dialogueLines.Length == 0)
        {
            StopDialogue();
            return;
        }

        if (isTyping)
        {
            // First press: complete current line immediately
            StopAllCoroutines();
            dialogueUI.SetDialogueText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            return;
        }

        dialogueUI.ClearChoices();

        if (dialogueData.endDialogueLines != null
            && dialogueIndex < dialogueData.endDialogueLines.Length
            && dialogueData.endDialogueLines[dialogueIndex])
        {
            StopDialogue();
            return;
        }

        if (dialogueData.choices != null)
        {
            foreach (DialogueChoice choice in dialogueData.choices)
            {
                if (choice.dialogueIndex == dialogueIndex)
                {
                    DisplayChoices(choice);
                    return;
                }
            }
        }

        if (++dialogueIndex < dialogueData.dialogueLines.Length)
            DisplayCurrentLine();
        else
            StopDialogue();
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

        if (dialogueData.autoProgressLines != null
            && dialogueIndex < dialogueData.autoProgressLines.Length
            && dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void DisplayChoices(DialogueChoice choice)
    {
        if (!dialogueUI || choice?.choices == null || choice.nextDialogueIndexes == null || choice.givesQuest == null)
            return;

        for (int i = 0; i < choice.choices.Length; i++)
        {
            if (i >= choice.nextDialogueIndexes.Length || i >= choice.givesQuest.Length) continue;
            int nextIndex = choice.nextDialogueIndexes[i];
            bool givesQuest = choice.givesQuest[i];
            dialogueUI.CreateChoice(choice.choices[i], () => chooseOption(nextIndex, givesQuest));
        }
    }

    public void chooseOption(int nextIndex, bool givesQuest)
    {
        if (givesQuest && QuestController.Instance != null && dialogueData?.quest)
        {
            QuestController.Instance.AcceptQuest(dialogueData.quest);
            questState = QuestState.InProgress;
        }

        dialogueIndex = dialogueData?.dialogueLines != null
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
        completionNPC?.TriggerQuestCompletion();
        if (completionNPC) SyncQuestState();

        if (dialogueData?.quest != null
            && dialogueData.isQuestHandInNPC
            && questState == QuestState.Completed
            && QuestController.Instance != null
            && !QuestController.Instance.isQuestHandedIn(dialogueData.quest.questID))
        {
            HandleQuestCompletion(dialogueData.quest);
        }

        StopAllCoroutines();
        isDialogueActive = false;
        isTyping = false;

        if (dialogueUI)
        {
            dialogueUI.SetDialogueText("");
            dialogueUI.ClearChoices();
            dialogueUI.ShowDialogue(false);
        }
    }

    public void HandleQuestCompletion(Quest quest)
    {
        QuestController.Instance?.CompleteQuest(quest.questID);
    }

    private void SyncQuestState()
    {
        if (dialogueData?.quest == null || QuestController.Instance == null) return;

        string questID = dialogueData.quest.questID;
        if (QuestController.Instance.isQuestHandedIn(questID) || QuestController.Instance.IsQuestReadyToHandIn(questID))
            questState = QuestState.Completed;
        else if (QuestController.Instance.isQuestActive(questID))
            questState = QuestState.InProgress;
        else
            questState = QuestState.NotStarted;
    }

    private void EnsureInteractionSetup()
    {
        if (!TryGetComponent(out InteractionTarget _))
            gameObject.AddComponent<InteractionTarget>();

        if (!GetComponentInChildren<Outline2D>(true))
            gameObject.AddComponent<Outline2D>();
    }

    private string GetActionLabel()
    {
        string label = isDialogueActive ? continueLabel : talkLabel;
        return string.IsNullOrWhiteSpace(label) ? "Talk" : label;
    }
}
