using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public GameObject dialogueBox;
    public TMP_Text dialogueText, nameText;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    
    public static DialogueController Instance { get; private set;  }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialogueBox)
        {
            dialogueCanvasGroup = dialogueCanvasGroup ? dialogueCanvasGroup : dialogueBox.GetOrAddComponent<CanvasGroup>();
            ShowDialogue(false);
        }
    }

    public void ShowDialogue(bool show)
    {
        if (!dialogueBox)
        {
            return;
        }

        dialogueBox.SetActive(show);
        dialogueCanvasGroup = dialogueCanvasGroup ? dialogueCanvasGroup : dialogueBox.GetOrAddComponent<CanvasGroup>();
        if (dialogueCanvasGroup)
        {
            dialogueCanvasGroup.alpha = show ? 1f : 0f;
            dialogueCanvasGroup.interactable = show;
            dialogueCanvasGroup.blocksRaycasts = show;
        }
    }

    public void SetNPCInfo(string npcName)
    {
        nameText.text = npcName;
    }

    public void SetDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void ClearChoices()
    {
        foreach(Transform child in choiceContainer) Destroy(child.gameObject);
    }

    public void CreateChoice(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
        choiceButton.GetComponentInChildren<TMP_Text>().text = choiceText;
        choiceButton.GetComponent<Button>().onClick.AddListener(onClick);
    }
}
