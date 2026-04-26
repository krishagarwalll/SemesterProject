using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionPromptPresenter : MonoBehaviour
{
    [SerializeField] private PointerContext pointer;
    [SerializeField] private PoptropicaController controller;
    [SerializeField] private Inventory inventory;
    [SerializeField] private InputGlyphLibrary glyphLibrary;
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image glyphImage;
    [SerializeField] private TextMeshProUGUI glyphFallbackText;
    [SerializeField] private Vector2 screenOffset = new(0f, 48f);

    private InteractionTarget currentTarget;

    private PointerContext Pointer => pointer ? pointer : pointer = FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include);
    private PoptropicaController Controller => controller ? controller : controller = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private RectTransform Root => root ? root : root = transform as RectTransform;
    private CanvasGroup Group => canvasGroup ? canvasGroup : canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
    private TextMeshProUGUI Label => labelText ? labelText : labelText = EnsureLabel();

    private void OnEnable()
    {
        if (Pointer)
        {
            Pointer.HoverChanged += HandleHoverChanged;
        }

        if (SceneInventory)
        {
            SceneInventory.Changed += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (pointer)
        {
            pointer.HoverChanged -= HandleHoverChanged;
        }

        if (inventory)
        {
            inventory.Changed -= Refresh;
        }
    }

    private void LateUpdate()
    {
        if (Controller && (Controller.HasActiveInteraction || Pointer && Pointer.IsDragging))
        {
            SetVisible(false);
            return;
        }

        if (!Root || !currentTarget || !Pointer || !Pointer.WorldCamera)
        {
            return;
        }

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Pointer.WorldCamera, currentTarget.InteractionPoint.position) + screenOffset;
        Root.position = screenPosition;
    }

    private void HandleHoverChanged(InteractionTarget previous, InteractionTarget current)
    {
        currentTarget = current;
        Refresh();
    }

    private void Refresh()
    {
        if (!Root)
        {
            return;
        }

        if (!currentTarget || !Controller)
        {
            SetVisible(false);
            return;
        }

        if (Controller.HasActiveInteraction || Pointer && Pointer.IsDragging)
        {
            SetVisible(false);
            return;
        }

        InteractionContext context = new(Controller, Pointer, currentTarget, SceneInventory);
        if (!currentTarget.TryGetPromptAction(context, out InteractionAction action))
        {
            SetVisible(false);
            return;
        }

        if (!action.Enabled)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        if (Label)
        {
            Label.font = glyphLibrary ? glyphLibrary.FontAsset : TMP_Settings.defaultFontAsset;
            Label.text = action.Label;
        }

        ApplyGlyph(action.GlyphId);
    }

    private TextMeshProUGUI EnsureLabel()
    {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>(true);
        if (label)
        {
            return label;
        }

        GameObject labelObject = new("PromptLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(transform, false);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(220f, 40f);

        label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = glyphLibrary ? glyphLibrary.FontAsset : TMP_Settings.defaultFontAsset;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private void ApplyGlyph(string glyphId)
    {
        if (!glyphLibrary)
        {
            if (glyphImage)
            {
                glyphImage.enabled = false;
            }

            if (glyphFallbackText)
            {
                glyphFallbackText.gameObject.SetActive(false);
            }

            return;
        }

        glyphLibrary.TryResolve(glyphId, out Sprite sprite, out string fallbackText);
        if (glyphImage)
        {
            glyphImage.enabled = sprite;
            glyphImage.sprite = sprite;
        }

        if (glyphFallbackText)
        {
            glyphFallbackText.font = glyphLibrary.FontAsset;
            glyphFallbackText.text = fallbackText;
            glyphFallbackText.gameObject.SetActive(!sprite && !string.IsNullOrWhiteSpace(fallbackText));
        }
    }

    private void SetVisible(bool visible)
    {
        if (Group)
        {
            Group.alpha = visible ? 1f : 0f;
            Group.blocksRaycasts = false;
            Group.interactable = false;
        }
        else if (Root)
        {
            Root.gameObject.SetActive(visible);
        }
    }
}
