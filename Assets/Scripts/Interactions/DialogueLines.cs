using System;
using UnityEngine;
public class DialogueLines : MonoBehaviour
{
    [TextArea(3,10)]
    [SerializeField] private String[] dialogues;

    public int currentDialogueIndex = 0;
    
    public string GetCurrentDialogue()
    {
        if (currentDialogueIndex < dialogues.Length)
        {
            return dialogues[currentDialogueIndex];
        }

        return "No dialogue available";
    }

    public void AdvanceDialogue()
    {
        currentDialogueIndex++;
    }
}
