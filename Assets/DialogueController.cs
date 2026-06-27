using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueController : MonoBehaviour
{
    public GameObject dialogueBox;
    public TMP_Text dialogueText, nameText;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private Button dismissButton;
    [SerializeField, Min(1f)] private float dialogueUiScaleMultiplier = 1.45f;
    
    public static DialogueController Instance { get; private set;  }
    public event System.Action DismissRequested;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialogueBox)
        {
            dialogueCanvasGroup = dialogueCanvasGroup ? dialogueCanvasGroup : dialogueBox.GetOrAddComponent<CanvasGroup>();
            EnsureDismissButton();
            ApplyDialogueScale();
            ShowDialogue(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (dismissButton) dismissButton.onClick.RemoveListener(RequestDismiss);
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

        if (!show)
        {
            SetDialogueText("");
            ClearChoices();
        }
    }

    public void SetNPCInfo(string npcName)
    {
        if (!nameText) return;
        nameText.text = npcName;
    }

    public void SetDialogueText(string text)
    {
        if (!dialogueText) return;
        dialogueText.text = text;
    }

    public void ClearChoices()
    {
        if (!choiceContainer) return;
        foreach(Transform child in choiceContainer) Destroy(child.gameObject);
        choiceContainer.gameObject.SetActive(false);
    }

    public void CreateChoice(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        if (!choiceContainer || !choiceButtonPrefab) return;
        choiceContainer.gameObject.SetActive(true);
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
        if (choiceButton.TryGetComponent(out Button button))
        {
            button.onClick.AddListener(onClick);
        }

        TMP_Text label = choiceButton.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.text = choiceText;
        }
    }

    private void EnsureDismissButton()
    {
        if (dismissButton)
        {
            PositionDismissButton(dismissButton.transform as RectTransform);
            dismissButton.onClick.RemoveListener(RequestDismiss);
            dismissButton.onClick.AddListener(RequestDismiss);
            return;
        }

        RectTransform parent = dialogueBox.transform as RectTransform;
        if (!parent) return;

        GameObject buttonObject = new("DismissButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        PositionDismissButton(rect);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.25f, 0.95f);

        dismissButton = buttonObject.GetComponent<Button>();
        dismissButton.targetGraphic = image;
        dismissButton.onClick.AddListener(RequestDismiss);

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "X";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void PositionDismissButton(RectTransform rect)
    {
        if (!rect) return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-12f, -12f);
        rect.sizeDelta = new Vector2(54f, 54f);
        rect.SetAsLastSibling();
    }

    private void ApplyDialogueScale()
    {
        if (dialogueText) dialogueText.fontSize *= dialogueUiScaleMultiplier;
        if (nameText) nameText.fontSize *= dialogueUiScaleMultiplier;

        if (choiceButtonPrefab)
        {
            TMP_Text[] labels = choiceButtonPrefab.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].fontSize *= dialogueUiScaleMultiplier;
            }
        }
    }

    public bool IsPointerOverDialogueBox()
    {
        if (!dialogueBox || !dialogueBox.activeInHierarchy || Mouse.current == null)
        {
            return false;
        }

        RectTransform rect = dialogueBox.transform as RectTransform;
        if (!rect)
        {
            return false;
        }

        Canvas canvas = dialogueBox.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), eventCamera);
    }

    private void RequestDismiss()
    {
        DismissRequested?.Invoke();
    }
}
