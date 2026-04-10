using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryHotbar : MonoBehaviour
{
    private const string DragPreviewName = "InventoryDragPreview";
    private static readonly Vector2 ExpandedSlotPosition = new(-12f, -72f);

    [SerializeField] private Inventory inventory;
    [SerializeField] private InventoryTransferController transferController;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Button backpackButton;

    [SerializeField] private bool collapsed;
    [SerializeField, Min(0f)] private float collapsedOffset = 220f;
    [SerializeField, Min(0f)] private float slideSpeed = 1200f;

    [SerializeField] private Vector2 slotSize = new(88f, 88f);
    [SerializeField] private Color slotColor = new(0.14f, 0.14f, 0.16f, 0.92f);
    [SerializeField] private Color selectedSlotColor = new(0.85f, 0.72f, 0.37f, 0.98f);
    [SerializeField] private Color emptySlotColor = new(0.08f, 0.08f, 0.09f, 0.7f);

    private readonly List<InventoryHotbarSlot> slots = new();
    private Vector2 targetAnchoredPosition;
    private Canvas rootCanvas;
    private RectTransform dragPreviewRoot;
    private Image dragPreviewBackground;
    private Image dragPreviewIcon;
    private TextMeshProUGUI dragPreviewLabel;
    private TextMeshProUGUI dragPreviewQuantity;
    private int dragSourceIndex = -1;
    private Vector2 lastDragScreenPosition;
    private bool worldPlacementActive;

    private RectTransform Panel => panel ? panel : panel = transform as RectTransform;
    private RectTransform SlotContainer => slotContainer ? slotContainer : slotContainer = EnsureSlotContainer();
    private Button BackpackButton => backpackButton ? backpackButton : backpackButton = EnsureBackpackButton();
    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private InventoryTransferController TransferController => transferController ? transferController : transferController = FindFirstObjectByType<InventoryTransferController>(FindObjectsInactive.Include);
    private Canvas RootCanvas => rootCanvas ? rootCanvas : rootCanvas = GetComponentInParent<Canvas>();

    public bool IsCollapsed => collapsed;

    private void Reset()
    {
        panel = transform as RectTransform;
        slotContainer = EnsureSlotContainer();
        backpackButton = EnsureBackpackButton();
        ApplyPanelLayout();
        EnsureSlots();
    }

    private void Awake()
    {
        ApplyPanelLayout();
        RebuildSlotCache();
        EnsureSlots();
        Refresh();
        UpdateTargetPosition(true);
    }

    private void OnValidate()
    {
        ApplyPanelLayout();
        slotContainer = EnsureSlotContainer();
        backpackButton = EnsureBackpackButton();
        RebuildSlotCache();
        EnsureSlots();
        UpdateTargetPosition(true);
        Refresh();
    }

    private void OnEnable()
    {
        if (SceneInventory)
        {
            SceneInventory.Changed += Refresh;
        }

        BackpackButton.onClick.AddListener(ToggleCollapsed);
        Refresh();
        UpdateTargetPosition(true);
    }

    private void OnDisable()
    {
        if (inventory)
        {
            inventory.Changed -= Refresh;
        }

        if (backpackButton)
        {
            backpackButton.onClick.RemoveListener(ToggleCollapsed);
        }

        EndSlotDrag(Vector2.zero, cancelled: true);
    }

    private void Update()
    {
        if (SlotContainer)
        {
            SlotContainer.anchoredPosition = Vector2.MoveTowards(SlotContainer.anchoredPosition, targetAnchoredPosition, slideSpeed * Time.unscaledDeltaTime);
        }
    }

    public void HandleSlotClick(int slotIndex)
    {
        if (!SceneInventory)
        {
            return;
        }

        if (!SceneInventory.TryGetEntry(slotIndex, out _))
        {
            SceneInventory.ClearSelection();
            return;
        }

        SceneInventory.Select(slotIndex);
    }

    public bool CanBeginSlotDrag(int slotIndex)
    {
        return !collapsed
            && !worldPlacementActive
            && (!TransferController || !TransferController.IsActive)
            && SceneInventory
            && SceneInventory.TryGetEntry(slotIndex, out _);
    }

    public void BeginSlotDrag(int slotIndex, Vector2 screenPosition)
    {
        if (!SceneInventory || !SceneInventory.TryGetEntry(slotIndex, out Inventory.Entry entry))
        {
            return;
        }

        dragSourceIndex = slotIndex;
        lastDragScreenPosition = screenPosition;
        worldPlacementActive = false;
        EnsureDragPreview();
        UpdateDragPreview(entry, screenPosition);
    }

    public void UpdateSlotDrag(Vector2 screenPosition)
    {
        if (dragSourceIndex < 0)
        {
            return;
        }

        lastDragScreenPosition = screenPosition;
        if (!worldPlacementActive
            && TransferController
            && !TryGetInventoryDropTarget(screenPosition, out _, out _)
            && TransferController.TryBeginPlacementFromInventory(dragSourceIndex))
        {
            worldPlacementActive = true;
            HideDragPreview();
            return;
        }

        if (!worldPlacementActive && dragPreviewRoot)
        {
            dragPreviewRoot.anchoredPosition = ScreenToCanvasPosition(screenPosition);
        }
    }

    public void EndSlotDrag(Vector2 screenPosition, bool cancelled = false)
    {
        if (dragSourceIndex < 0)
        {
            HideDragPreview();
            return;
        }

        if (screenPosition == Vector2.zero)
        {
            screenPosition = lastDragScreenPosition;
        }

        if (!cancelled
            && !worldPlacementActive
            && TransferController
            && !TryGetInventoryDropTarget(screenPosition, out _, out _)
            && TransferController.TryBeginPlacementFromInventory(dragSourceIndex))
        {
            worldPlacementActive = true;
        }

        if (worldPlacementActive)
        {
            TransferController?.EndPlacementDrag(screenPosition, cancelled);
            dragSourceIndex = -1;
            lastDragScreenPosition = Vector2.zero;
            worldPlacementActive = false;
            HideDragPreview();
            return;
        }

        if (!cancelled && !worldPlacementActive && SceneInventory)
        {
            if (TryGetInventoryDropTarget(screenPosition, out int slotIndex, out _))
            {
                if (slotIndex >= 0 && slotIndex != dragSourceIndex)
                {
                    SceneInventory.Move(dragSourceIndex, slotIndex);
                }
                else if (SceneInventory.TryGetEntry(dragSourceIndex, out _))
                {
                    SceneInventory.Select(dragSourceIndex, toggle: false);
                }
            }
            else if (SceneInventory.TryGetEntry(dragSourceIndex, out _))
            {
                SceneInventory.Select(dragSourceIndex, toggle: false);
            }
        }

        dragSourceIndex = -1;
        lastDragScreenPosition = Vector2.zero;
        worldPlacementActive = false;
        HideDragPreview();
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        UpdateTargetPosition(false);
    }

    public bool IsInventoryArea(Vector2 screenPosition)
    {
        return TryGetInventoryDropTarget(screenPosition, out _, out _);
    }

    public bool TryGetInventoryDropTarget(Vector2 screenPosition, out int slotIndex, out bool overBackpack)
    {
        slotIndex = -1;
        overBackpack = false;

        Camera eventCamera = RootCanvas && RootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? RootCanvas.worldCamera : null;
        if (BackpackButton && RectTransformUtility.RectangleContainsScreenPoint(BackpackButton.transform as RectTransform, screenPosition, eventCamera))
        {
            overBackpack = true;
            return true;
        }

        if (!collapsed && TryGetExactSlotIndex(screenPosition, eventCamera, out slotIndex))
        {
            return true;
        }

        if (Panel && RectTransformUtility.RectangleContainsScreenPoint(Panel, screenPosition, eventCamera))
        {
            if (!collapsed && TryGetClosestSlotIndex(screenPosition, out slotIndex))
            {
                return true;
            }

            overBackpack = true;
            return true;
        }

        return false;
    }

    private void Refresh()
    {
        EnsureSlots();
        Inventory currentInventory = SceneInventory;
        for (int i = 0; i < slots.Count; i++)
        {
            Inventory.Entry entry = default;
            bool hasEntry = currentInventory && currentInventory.TryGetEntry(i, out entry);
            bool selected = currentInventory && currentInventory.SelectedIndex == i;
            slots[i].Bind(this, i, entry, hasEntry, selected, slotColor, selectedSlotColor, emptySlotColor);
        }
    }

    private void EnsureSlots()
    {
        if (slots.Count == 0)
        {
            RebuildSlotCache();
        }

        int slotCount = SceneInventory ? SceneInventory.Capacity : 6;
        while (slots.Count < slotCount)
        {
            slots.Add(InventoryHotbarSlot.Create(SlotContainer, slotSize));
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(i < slotCount);
        }
    }

    private void RebuildSlotCache()
    {
        slots.Clear();
        if (!SlotContainer)
        {
            return;
        }

        slots.AddRange(SlotContainer.GetComponentsInChildren<InventoryHotbarSlot>(true));
    }

    private void EnsureDragPreview()
    {
        if (dragPreviewRoot || !RootCanvas)
        {
            return;
        }

        Transform existing = RootCanvas.transform.Find(DragPreviewName);
        if (existing)
        {
            dragPreviewRoot = existing as RectTransform;
            dragPreviewBackground = dragPreviewRoot ? dragPreviewRoot.GetComponent<Image>() : null;
            dragPreviewIcon = dragPreviewRoot && dragPreviewRoot.Find("Icon") ? dragPreviewRoot.Find("Icon").GetComponent<Image>() : null;
            dragPreviewLabel = dragPreviewRoot && dragPreviewRoot.Find("Label") ? dragPreviewRoot.Find("Label").GetComponent<TextMeshProUGUI>() : null;
            dragPreviewQuantity = dragPreviewRoot && dragPreviewRoot.Find("Quantity") ? dragPreviewRoot.Find("Quantity").GetComponent<TextMeshProUGUI>() : null;
            return;
        }

        GameObject previewObject = new(DragPreviewName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragPreviewRoot = previewObject.GetComponent<RectTransform>();
        dragPreviewRoot.SetParent(RootCanvas.transform, false);
        dragPreviewRoot.anchorMin = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.anchorMax = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.pivot = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.sizeDelta = slotSize;

        CanvasGroup group = previewObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0.92f;

        dragPreviewBackground = previewObject.GetComponent<Image>();
        dragPreviewBackground.raycastTarget = false;

        GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(dragPreviewRoot, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = slotSize * 0.72f;

        dragPreviewIcon = iconObject.GetComponent<Image>();
        dragPreviewIcon.preserveAspect = true;
        dragPreviewIcon.raycastTarget = false;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(dragPreviewRoot, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 8f);
        labelRect.offsetMax = new Vector2(-8f, -8f);

        dragPreviewLabel = labelObject.GetComponent<TextMeshProUGUI>();
        dragPreviewLabel.alignment = TextAlignmentOptions.Center;
        dragPreviewLabel.fontSize = 18f;
        dragPreviewLabel.font = TMP_Settings.defaultFontAsset;
        dragPreviewLabel.raycastTarget = false;

        GameObject quantityObject = new("Quantity", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform quantityRect = quantityObject.GetComponent<RectTransform>();
        quantityRect.SetParent(dragPreviewRoot, false);
        quantityRect.anchorMin = new Vector2(1f, 0f);
        quantityRect.anchorMax = new Vector2(1f, 0f);
        quantityRect.pivot = new Vector2(1f, 0f);
        quantityRect.anchoredPosition = new Vector2(-8f, 8f);
        quantityRect.sizeDelta = new Vector2(slotSize.x - 16f, 22f);

        dragPreviewQuantity = quantityObject.GetComponent<TextMeshProUGUI>();
        dragPreviewQuantity.alignment = TextAlignmentOptions.BottomRight;
        dragPreviewQuantity.fontSize = 18f;
        dragPreviewQuantity.font = TMP_Settings.defaultFontAsset;
        dragPreviewQuantity.raycastTarget = false;

        dragPreviewRoot.gameObject.SetActive(false);
    }

    private void UpdateDragPreview(Inventory.Entry entry, Vector2 screenPosition)
    {
        if (!dragPreviewRoot)
        {
            return;
        }

        InventoryItemDefinition definition = entry.Definition;
        Sprite previewSprite = InventoryItemVisualResolver.GetSprite(definition);
        bool showIcon = previewSprite;
        dragPreviewRoot.gameObject.SetActive(true);
        dragPreviewRoot.anchoredPosition = ScreenToCanvasPosition(screenPosition);
        dragPreviewBackground.color = slotColor;
        dragPreviewIcon.enabled = showIcon;
        dragPreviewIcon.sprite = previewSprite;
        dragPreviewLabel.enabled = !showIcon;
        dragPreviewLabel.text = InventoryHotbarSlot.GetFallbackLabel(definition);
        dragPreviewQuantity.text = entry.Quantity > 1 ? entry.Quantity.ToString() : string.Empty;
    }

    private void HideDragPreview()
    {
        if (dragPreviewRoot)
        {
            dragPreviewRoot.gameObject.SetActive(false);
        }
    }

    private void UpdateTargetPosition(bool applyImmediately)
    {
        targetAnchoredPosition = collapsed
            ? new Vector2(collapsedOffset, ExpandedSlotPosition.y)
            : ExpandedSlotPosition;
        if (applyImmediately && SlotContainer)
        {
            SlotContainer.anchoredPosition = targetAnchoredPosition;
        }
    }

    private void ApplyPanelLayout()
    {
        if (!Panel)
        {
            return;
        }

        Panel.anchorMin = new Vector2(1f, 0.5f);
        Panel.anchorMax = new Vector2(1f, 0.5f);
        Panel.pivot = new Vector2(1f, 0.5f);
        Panel.sizeDelta = new Vector2(slotSize.x + 24f, slotSize.y * 6f + 120f);
    }

    private RectTransform EnsureSlotContainer()
    {
        Transform child = transform.Find("Slots");
        RectTransform container = child as RectTransform;
        if (!container)
        {
            GameObject containerObject = new("Slots", typeof(RectTransform), typeof(VerticalLayoutGroup));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(transform, false);
        }

        VerticalLayoutGroup layout = container.GetOrAddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        container.anchorMin = new Vector2(1f, 1f);
        container.anchorMax = new Vector2(1f, 1f);
        container.pivot = new Vector2(1f, 1f);
        container.sizeDelta = new Vector2(slotSize.x, slotSize.y * 6f + 60f);
        container.anchoredPosition = ExpandedSlotPosition;
        return container;
    }

    private Button EnsureBackpackButton()
    {
        Transform child = transform.Find("BackpackButton");
        Button button = child ? child.GetComponent<Button>() : null;
        if (!button)
        {
            GameObject buttonObject = new("BackpackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(slotSize.x, 52f);
            rect.anchoredPosition = new Vector2(-12f, -12f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = slotColor;
            button = buttonObject.GetComponent<Button>();

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.text = "Bag";
            label.font = TMP_Settings.defaultFontAsset;
        }

        return button;
    }

    private bool TryGetExactSlotIndex(Vector2 screenPosition, Camera eventCamera, out int slotIndex)
    {
        slotIndex = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i] || !slots[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            RectTransform slotRect = slots[i].transform as RectTransform;
            if (slotRect && RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, eventCamera))
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool TryGetClosestSlotIndex(Vector2 screenPosition, out int slotIndex)
    {
        slotIndex = -1;
        if (collapsed)
        {
            return false;
        }

        Camera eventCamera = RootCanvas && RootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? RootCanvas.worldCamera : null;
        if (TryGetExactSlotIndex(screenPosition, eventCamera, out slotIndex))
        {
            return true;
        }

        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i] || !slots[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            RectTransform slotRect = slots[i].transform as RectTransform;
            if (!slotRect)
            {
                continue;
            }

            Vector2 slotScreenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, slotRect.position);
            float distance = (slotScreenPoint - screenPosition).sqrMagnitude;
            if (distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            slotIndex = i;
        }

        return slotIndex >= 0;
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        RectTransform canvasRect = RootCanvas.transform as RectTransform;
        Camera eventCamera = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : RootCanvas.worldCamera;
        if (!canvasRect || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
        {
            return screenPosition;
        }

        return localPosition;
    }
}
