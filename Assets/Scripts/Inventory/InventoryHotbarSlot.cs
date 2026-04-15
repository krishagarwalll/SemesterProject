using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public readonly struct InventoryHotbarSecondaryClickRequest
{
    public InventoryHotbarSecondaryClickRequest(int slotIndex, Vector2 screenPosition)
    {
        SlotIndex = slotIndex;
        ScreenPosition = screenPosition;
    }

    public int SlotIndex { get; }
    public Vector2 ScreenPosition { get; }
}

public class InventoryHotbarSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private CanvasGroup canvasGroup;

    private InventoryHotbar owner;
    private int slotIndex;
    private bool hasEntry;

    private Image Background => this.ResolveComponent(ref background);
    private Image Icon => this.ResolveComponent(ref icon, true);
    private TextMeshProUGUI LabelText => this.ResolveComponent(ref labelText, true);
    private TextMeshProUGUI QuantityText => this.ResolveComponent(ref quantityText, true);
    private CanvasGroup Group => canvasGroup ? canvasGroup : canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

    public static InventoryHotbarSlot Create(RectTransform parent, Vector2 size)
    {
        GameObject slotObject = new("Slot", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(CanvasGroup), typeof(InventoryHotbarSlot));
        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        LayoutElement layout = slotObject.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        InventoryHotbarSlot slot = slotObject.GetComponent<InventoryHotbarSlot>();
        slot.canvasGroup = slotObject.GetComponent<CanvasGroup>();
        slot.background = slotObject.GetComponent<Image>();
        slot.background.raycastTarget = true;

        GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = size * 0.72f;

        slot.icon = iconObject.GetComponent<Image>();
        slot.icon.preserveAspect = true;
        slot.icon.raycastTarget = false;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = size - new Vector2(16f, 16f);

        slot.labelText = labelObject.GetComponent<TextMeshProUGUI>();
        slot.labelText.alignment = TextAlignmentOptions.Center;
        slot.labelText.fontSize = 18f;
        slot.labelText.font = TMP_Settings.defaultFontAsset;
        slot.labelText.raycastTarget = false;

        GameObject quantityObject = new("Quantity", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform quantityRect = quantityObject.GetComponent<RectTransform>();
        quantityRect.SetParent(rect, false);
        quantityRect.anchorMin = new Vector2(1f, 0f);
        quantityRect.anchorMax = new Vector2(1f, 0f);
        quantityRect.pivot = new Vector2(1f, 0f);
        quantityRect.anchoredPosition = new Vector2(-8f, 8f);
        quantityRect.sizeDelta = new Vector2(size.x - 16f, 22f);

        slot.quantityText = quantityObject.GetComponent<TextMeshProUGUI>();
        slot.quantityText.alignment = TextAlignmentOptions.BottomRight;
        slot.quantityText.fontSize = 18f;
        slot.quantityText.font = TMP_Settings.defaultFontAsset;
        slot.quantityText.raycastTarget = false;
        return slot;
    }

    public void Bind(
        InventoryHotbar hotbar,
        int index,
        Inventory.Entry entry,
        bool entryPresent,
        bool selected,
        Color slotColor,
        Color selectedSlotColor,
        Color emptySlotColor)
    {
        owner = hotbar;
        slotIndex = index;
        hasEntry = entryPresent;

        Sprite displaySprite = entryPresent ? InventoryItemVisualResolver.GetSprite(entry.Definition) : null;
        bool showIcon = displaySprite;
        Background.color = selected ? selectedSlotColor : entryPresent ? slotColor : emptySlotColor;
        Icon.enabled = showIcon;
        Icon.sprite = displaySprite;
        if (LabelText)
        {
            LabelText.text = entryPresent ? GetFallbackLabel(entry.Definition) : string.Empty;
            LabelText.enabled = entryPresent && !showIcon;
        }

        QuantityText.text = entryPresent && entry.Quantity > 1 ? entry.Quantity.ToString() : string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            owner?.HandleSlotClick(slotIndex);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            owner?.SendMessage(
                nameof(InventoryHotbar.HandleSlotSecondaryClick),
                new InventoryHotbarSecondaryClickRequest(slotIndex, eventData.position),
                SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !hasEntry || owner == null || !owner.CanBeginSlotDrag(slotIndex))
        {
            eventData.pointerDrag = null;
            return;
        }

        Group.blocksRaycasts = false;
        Group.alpha = 0.35f;
        owner.BeginSlotDrag(slotIndex, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.UpdateSlotDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Group.blocksRaycasts = true;
        Group.alpha = 1f;
        owner?.EndSlotDrag(eventData.position);
    }

    public static string GetFallbackLabel(InventoryItemDefinition definition)
    {
        if (!definition)
        {
            return string.Empty;
        }

        string displayName = definition.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "?";
        }

        string[] words = displayName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";
        }

        string trimmed = displayName.Trim();
        return trimmed.Length >= 2
            ? trimmed[..2].ToUpperInvariant()
            : trimmed.ToUpperInvariant();
    }
}
