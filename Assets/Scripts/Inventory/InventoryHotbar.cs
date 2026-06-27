using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryHotbar : MonoBehaviour
{
    private const string DragPreviewName = "InventoryDragPreview";
    private const string InventoryContextMenuName = "InventoryContextMenu";

    // Pivot is left-center; anchor is panel top-left. Negative Y places it inside the panel.
    private Vector2 ExpandedSlotPosition => new(12f, -(slotSize.y * 0.5f + 12f));

    [SerializeField] private Inventory inventory;
    [SerializeField] private InventoryTransferController transferController;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Button backpackButton;
    [SerializeField] private InventoryHotbarSlot collectibleSlot;

    [SerializeField] private bool collapsed;
    [SerializeField, Min(0f)] private float collapsedOffset = 220f;
    [SerializeField, Min(0f)] private float slideSpeed = 1200f;

    [SerializeField] private Vector2 slotSize = new(112f, 112f);
    [SerializeField] private Color slotColor = new(0.740566f, 0.8280362f, 1f, 0.70980394f);
    [SerializeField] private Color emptySlotColor = new(0.07716268f, 0.12713027f, 0.3207547f, 0.85882354f);

    private readonly List<InventoryHotbarSlot> slots = new();
    private readonly List<int> slotInventoryIndexes = new();
    private Vector2 targetAnchoredPosition;
    private Canvas rootCanvas;
    private RectTransform dragPreviewRoot;
    private Image dragPreviewBackground;
    private Image dragPreviewIcon;
    private TextMeshProUGUI dragPreviewLabel;
    private TextMeshProUGUI dragPreviewQuantity;
    private RectTransform contextMenuRoot;
    private Button contextInspectButton;
    private Button contextDropButton;
    private int contextSlotIndex = -1;
    private int dragSourceIndex = -1;
    private Vector2 lastDragScreenPosition;
    private bool worldPlacementActive;

    private Vector2 EffectiveSlotSize => new(Mathf.Max(112f, slotSize.x), Mathf.Max(112f, slotSize.y));
    private RectTransform Panel => panel ? panel : panel = transform as RectTransform;
    private RectTransform SlotContainer => slotContainer ? slotContainer : slotContainer = EnsureSlotContainer();
    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private InventoryTransferController TransferController => transferController ? transferController : transferController = FindFirstObjectByType<InventoryTransferController>(FindObjectsInactive.Include);
    private Canvas RootCanvas => rootCanvas ? rootCanvas : rootCanvas = GetComponentInParent<Canvas>();

    public bool IsCollapsed => collapsed;

    private void Reset()
    {
        panel = transform as RectTransform;
        ApplyPanelLayout();
    }

    private void Awake()
    {
        slotSize = EffectiveSlotSize;
        ApplyPanelLayout();
        // Always call directly — lazy property short-circuits if the serialized
        // field is already set from scene data, which skips layout group cleanup.
        slotContainer = EnsureSlotContainer();
        backpackButton = EnsureBackpackButton();
        collectibleSlot = EnsureCollectibleSlot();
        RebuildSlotCache();
        EnsureSlots();
        Refresh();
        UpdateTargetPosition(applyImmediately: true);
    }

    private void OnValidate()
    {
        // Safe edit-mode recalc only — no GameObject creation, no scene queries.
        slotSize = EffectiveSlotSize;
        if (Panel) ApplyPanelLayout();
    }

    private void OnEnable()
    {
        slotContainer = EnsureSlotContainer();
        backpackButton = EnsureBackpackButton();
        collectibleSlot = EnsureCollectibleSlot();
        RebuildSlotCache();

        if (SceneInventory)
            SceneInventory.Changed += Refresh;

        // Use field (not lazy property) — avoid creating UI elements during enable.
        if (backpackButton)
            backpackButton.onClick.AddListener(ToggleCollapsed);

        Refresh();
        UpdateTargetPosition(applyImmediately: true);
    }

    private void OnDisable()
    {
        if (inventory)
            inventory.Changed -= Refresh;

        if (backpackButton)
            backpackButton.onClick.RemoveListener(ToggleCollapsed);

        EndSlotDrag(Vector2.zero, cancelled: true);
    }

    private void Update()
    {
        // Use field directly — avoid triggering EnsureSlotContainer every frame.
        if (slotContainer)
        {
            slotContainer.anchoredPosition = Vector2.MoveTowards(
                slotContainer.anchoredPosition, targetAnchoredPosition, slideSpeed * Time.unscaledDeltaTime);
        }
    }

    public void HandleSlotClick(int slotIndex)
    {
        HideContextMenu();
    }

    private Vector2 GetSlotScreenCenter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || !slots[slotIndex]) return Vector2.zero;
        RectTransform slotRect = slots[slotIndex].transform as RectTransform;
        if (!slotRect) return Vector2.zero;
        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        Camera cam = GetEventCamera();
        return cam ? cam.WorldToScreenPoint(center) : RectTransformUtility.WorldToScreenPoint(null, center);
    }

    public void HandleSlotSecondaryClick(int slotIndex, Vector2 screenPosition)
    {
        HideContextMenu();
    }

    public void HandleSlotSecondaryClick(InventoryHotbarSecondaryClickRequest request)
    {
        HandleSlotSecondaryClick(request.SlotIndex, request.ScreenPosition);
    }

    public bool CanBeginSlotDrag(int slotIndex)
    {
        int inventoryIndex = GetInventoryIndexForSlot(slotIndex);
        return !collapsed
            && !worldPlacementActive
            && (!TransferController || !TransferController.IsActive)
            && SceneInventory
            && inventoryIndex >= 0
            && SceneInventory.TryGetEntry(inventoryIndex, out Inventory.Entry entry)
            && entry.Definition
            && !entry.Definition.CollectibleOnly;
    }

    public void BeginSlotDrag(int slotIndex, Vector2 screenPosition)
    {
        int inventoryIndex = GetInventoryIndexForSlot(slotIndex);
        if (!SceneInventory || inventoryIndex < 0 || !SceneInventory.TryGetEntry(inventoryIndex, out Inventory.Entry entry)) return;

        dragSourceIndex = inventoryIndex;
        lastDragScreenPosition = screenPosition;
        worldPlacementActive = false;
        EnsureDragPreview();
        UpdateDragPreview(entry, screenPosition);
    }

    public void UpdateSlotDrag(Vector2 screenPosition)
    {
        if (dragSourceIndex < 0) return;

        lastDragScreenPosition = screenPosition;
        bool overInventory = IsInventoryZone(screenPosition);
        bool canWorldPreview = TransferController && !overInventory && TransferController.CanPreviewPlacementAt(screenPosition);

        if (worldPlacementActive)
        {
            if (!canWorldPreview)
            {
                TransferController?.EndPlacementTransfer(screenPosition, cancelled: true);
                worldPlacementActive = false;
                RestoreSlotDragPreview(screenPosition);
                return;
            }

            TransferController?.UpdatePlacementTransfer(screenPosition);
            return;
        }

        if (canWorldPreview && TransferController.TryBeginPlacementTransfer(dragSourceIndex, screenPosition))
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
            screenPosition = lastDragScreenPosition;

        bool overInventory = IsInventoryZone(screenPosition);
        bool canWorldPreview = TransferController && !overInventory && TransferController.CanPreviewPlacementAt(screenPosition);

        if (worldPlacementActive)
        {
            bool commitPlacement = !cancelled && canWorldPreview;
            TransferController?.EndPlacementTransfer(screenPosition, cancelled: !commitPlacement);
            dragSourceIndex = -1;
            lastDragScreenPosition = Vector2.zero;
            worldPlacementActive = false;
            HideDragPreview();
            if (commitPlacement) return;
        }

        if (!cancelled
            && !worldPlacementActive
            && !overInventory
            && TransferController
            && TransferController.TryUseEntryOnWorldTarget(dragSourceIndex, screenPosition))
        {
            dragSourceIndex = -1;
            lastDragScreenPosition = Vector2.zero;
            worldPlacementActive = false;
            HideDragPreview();
            return;
        }

        if (!cancelled && !worldPlacementActive && canWorldPreview
            && TransferController
            && TransferController.TryBeginPlacementTransfer(dragSourceIndex, screenPosition))
        {
            TransferController.EndPlacementTransfer(screenPosition, cancelled: false);
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
                int targetInventoryIndex = GetInventoryIndexForSlot(slotIndex);
                if (targetInventoryIndex >= 0 && targetInventoryIndex != dragSourceIndex)
                    SceneInventory.Move(dragSourceIndex, targetInventoryIndex);
            }
        }

        dragSourceIndex = -1;
        lastDragScreenPosition = Vector2.zero;
        worldPlacementActive = false;
        HideDragPreview();
    }

    private void RestoreSlotDragPreview(Vector2 screenPosition)
    {
        if (!SceneInventory || !SceneInventory.TryGetEntry(dragSourceIndex, out Inventory.Entry entry))
        {
            HideDragPreview();
            return;
        }

        EnsureDragPreview();
        UpdateDragPreview(entry, screenPosition);
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        UpdateTargetPosition(applyImmediately: false);
    }

    public bool IsInventoryArea(Vector2 screenPosition) => IsInventoryZone(screenPosition);

    public bool BlocksWorldInteractionAt(Vector2 screenPosition)
    {
        Camera eventCamera = GetEventCamera();
        if (backpackButton && RectTransformUtility.RectangleContainsScreenPoint(
                backpackButton.transform as RectTransform, screenPosition, eventCamera))
            return true;

        return !collapsed && TryGetExactSlotIndex(screenPosition, eventCamera, out _);
    }

    public void ShowTransferPreview(Inventory.Entry entry, Vector2 screenPosition)
    {
        EnsureDragPreview();
        UpdateDragPreview(entry, screenPosition);
    }

    public void UpdateTransferPreview(Vector2 screenPosition)
    {
        if (dragPreviewRoot)
            dragPreviewRoot.anchoredPosition = ScreenToCanvasPosition(screenPosition);
    }

    public void HideTransferPreview() => HideDragPreview();

    public bool TryGetStoreDropTarget(Vector2 screenPosition, out int slotIndex, out bool overBackpack)
    {
        slotIndex = -1;
        overBackpack = false;

        Camera eventCamera = GetEventCamera();
        if (backpackButton && RectTransformUtility.RectangleContainsScreenPoint(
                backpackButton.transform as RectTransform, screenPosition, eventCamera))
        {
            overBackpack = true;
            return true;
        }

        return !collapsed && TryGetExactSlotIndex(screenPosition, eventCamera, out slotIndex);
    }

    public bool TryGetInventoryDropTarget(Vector2 screenPosition, out int slotIndex, out bool overBackpack)
    {
        slotIndex = -1;
        overBackpack = false;

        Camera eventCamera = GetEventCamera();
        if (backpackButton && RectTransformUtility.RectangleContainsScreenPoint(
                backpackButton.transform as RectTransform, screenPosition, eventCamera))
        {
            overBackpack = true;
            return true;
        }

        if (!collapsed && TryGetExactSlotIndex(screenPosition, eventCamera, out slotIndex))
            return true;

        if (panel && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, eventCamera))
        {
            overBackpack = true;
            return true;
        }

        return false;
    }

    private void Refresh()
    {
        EnsureSlots();
        Inventory currentInventory = SceneInventory;
        slotInventoryIndexes.Clear();

        int visibleSlot = 0;
        Inventory.Entry collectibleEntry = default;
        bool hasCollectible = false;

        if (currentInventory)
        {
            for (int inventoryIndex = 0; inventoryIndex < currentInventory.Entries.Count && visibleSlot < slots.Count; inventoryIndex++)
            {
                Inventory.Entry entry = currentInventory.Entries[inventoryIndex];
                if (!entry.IsOccupied)
                {
                    continue;
                }

                if (entry.Definition && entry.Definition.CollectibleOnly)
                {
                    collectibleEntry = hasCollectible && collectibleEntry.Definition == entry.Definition
                        ? new Inventory.Entry(entry.Definition, collectibleEntry.Quantity + entry.Quantity)
                        : entry;
                    hasCollectible = true;
                    continue;
                }

                slotInventoryIndexes.Add(inventoryIndex);
                slots[visibleSlot].Bind(this, visibleSlot, entry, true, slotColor, emptySlotColor);
                visibleSlot++;
            }

            for (int inventoryIndex = 0; inventoryIndex < currentInventory.Entries.Count && visibleSlot < slots.Count; inventoryIndex++)
            {
                Inventory.Entry entry = currentInventory.Entries[inventoryIndex];
                if (entry.IsOccupied)
                {
                    continue;
                }

                slotInventoryIndexes.Add(inventoryIndex);
                slots[visibleSlot].Bind(this, visibleSlot, default, false, slotColor, emptySlotColor);
                visibleSlot++;
            }
        }

        while (slotInventoryIndexes.Count < slots.Count)
        {
            slotInventoryIndexes.Add(-1);
        }

        for (int i = visibleSlot; i < slots.Count; i++)
        {
            slots[i].Bind(this, i, default, false, slotColor, emptySlotColor);
        }

        if (collectibleSlot)
        {
            collectibleSlot.Bind(this, -1, collectibleEntry, hasCollectible, slotColor, emptySlotColor);
            collectibleSlot.gameObject.SetActive(hasCollectible);
        }
    }

    private void EnsureSlots()
    {
        if (slots.Count == 0) RebuildSlotCache();

        int slotCount = SceneInventory ? SceneInventory.Capacity : 6;
        while (slots.Count < slotCount)
            slots.Add(InventoryHotbarSlot.Create(SlotContainer, slotSize));

        for (int i = 0; i < slots.Count; i++)
        {
            ApplySlotLayout(slots[i]);
            slots[i].gameObject.SetActive(i < slotCount);
        }
    }

    private void RebuildSlotCache()
    {
        slots.Clear();
        if (!slotContainer) return;
        slots.AddRange(slotContainer.GetComponentsInChildren<InventoryHotbarSlot>(true));
    }

    private void EnsureDragPreview()
    {
        if (dragPreviewRoot || !RootCanvas) return;

        Transform existing = RootCanvas.transform.Find(DragPreviewName);
        if (existing)
        {
            dragPreviewRoot = existing as RectTransform;
            dragPreviewBackground = dragPreviewRoot ? dragPreviewRoot.GetComponent<Image>() : null;
            var iconT = dragPreviewRoot ? dragPreviewRoot.Find("Icon") : null;
            var labelT = dragPreviewRoot ? dragPreviewRoot.Find("Label") : null;
            var quantityT = dragPreviewRoot ? dragPreviewRoot.Find("Quantity") : null;
            dragPreviewIcon = iconT ? iconT.GetComponent<Image>() : null;
            dragPreviewLabel = labelT ? labelT.GetComponent<TextMeshProUGUI>() : null;
            dragPreviewQuantity = quantityT ? quantityT.GetComponent<TextMeshProUGUI>() : null;
            return;
        }

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

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
        if (font) dragPreviewLabel.font = font;
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
        if (font) dragPreviewQuantity.font = font;
        dragPreviewQuantity.raycastTarget = false;

        dragPreviewRoot.gameObject.SetActive(false);
    }

    private void UpdateDragPreview(Inventory.Entry entry, Vector2 screenPosition)
    {
        if (!dragPreviewRoot) return;

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
            dragPreviewRoot.gameObject.SetActive(false);
    }

    private void EnsureContextMenu()
    {
        if (contextMenuRoot || !RootCanvas) return;

        Transform existing = RootCanvas.transform.Find(InventoryContextMenuName);
        if (existing)
        {
            contextMenuRoot = existing as RectTransform;
            var inspectT = contextMenuRoot ? contextMenuRoot.Find("Inspect") : null;
            var dropT = contextMenuRoot ? contextMenuRoot.Find("Drop") : null;
            contextInspectButton = inspectT ? inspectT.GetComponent<Button>() : null;
            contextDropButton = dropT ? dropT.GetComponent<Button>() : null;
            return;
        }

        GameObject menuObject = new(InventoryContextMenuName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contextMenuRoot = menuObject.GetComponent<RectTransform>();
        contextMenuRoot.SetParent(RootCanvas.transform, false);
        contextMenuRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contextMenuRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contextMenuRoot.pivot = new Vector2(0.5f, 0.5f);
        contextMenuRoot.sizeDelta = new Vector2(220f, 0f);

        menuObject.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

        VerticalLayoutGroup layout = menuObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = menuObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        contextInspectButton = CreateContextButton("Inspect", "Inspect");
        contextDropButton = CreateContextButton("Drop", "Drop");
        contextMenuRoot.gameObject.SetActive(false);
    }

    private Button CreateContextButton(string objectName, string labelText)
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        GameObject buttonObject = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(contextMenuRoot, false);
        rect.sizeDelta = new Vector2(0f, 34f);
        buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);

        LayoutElement le = buttonObject.GetComponent<LayoutElement>();
        le.preferredWidth = 204f;
        le.preferredHeight = 34f;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.fontSize = 18f;
        if (font) label.font = font;
        label.text = labelText;

        return buttonObject.GetComponent<Button>();
    }

    private void ConfigureContextMenu(Inventory.Entry entry)
    {
        if (!contextInspectButton || !contextDropButton) return;

        InventoryItemDefinition definition = entry.Definition;
        bool canInspect = definition && !string.IsNullOrWhiteSpace(definition.Description);
        bool canDrop = definition && definition.CanPlaceBackIntoWorld && TransferController;

        contextInspectButton.gameObject.SetActive(canInspect);
        contextInspectButton.onClick.RemoveAllListeners();
        if (canInspect)
        {
            contextInspectButton.onClick.AddListener(() =>
            {
                InteractionFeedback.Show(definition.Description, definition);
                HideContextMenu();
            });
        }

        contextDropButton.gameObject.SetActive(canDrop);
        contextDropButton.onClick.RemoveAllListeners();
        if (canDrop)
        {
            contextDropButton.onClick.AddListener(() =>
            {
                if (contextSlotIndex >= 0)
                    TransferController.TryDropEntryToWorld(contextSlotIndex);
                HideContextMenu();
                Refresh();
            });
        }
    }

    private void ShowContextMenu(Vector2 screenPosition)
    {
        if (!contextMenuRoot) return;
        contextMenuRoot.gameObject.SetActive(true);
        contextMenuRoot.anchoredPosition = ScreenToCanvasPosition(screenPosition);
    }

    private void HideContextMenu()
    {
        contextSlotIndex = -1;
        if (contextMenuRoot)
            contextMenuRoot.gameObject.SetActive(false);
    }

    private void UpdateTargetPosition(bool applyImmediately)
    {
        // Collapsed: container slides up (positive Y = above panel top anchor = off-screen)
        targetAnchoredPosition = collapsed
            ? new Vector2(ExpandedSlotPosition.x, collapsedOffset)
            : ExpandedSlotPosition;

        if (applyImmediately && slotContainer)
            slotContainer.anchoredPosition = targetAnchoredPosition;
    }

    private void ApplyPanelLayout()
    {
        if (!Panel) return;

        // Top-centre horizontal bar: 6 slots + gaps + backpack button + padding
        Panel.anchorMin = new Vector2(0.5f, 1f);
        Panel.anchorMax = new Vector2(0.5f, 1f);
        Panel.pivot = new Vector2(0.5f, 1f);
        Panel.anchoredPosition = new Vector2(0f, -24f);
        Panel.sizeDelta = new Vector2(slotSize.x * 6f + 12f * 5f + slotSize.x + 48f, slotSize.y + 24f);
    }

    private RectTransform EnsureSlotContainer()
    {
        Transform child = transform.Find("Slots");
        RectTransform container = child as RectTransform;
        if (!container)
        {
            GameObject containerObject = new("Slots", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(transform, false);
        }

        // Remove any layout group that isn't HorizontalLayoutGroup.
        // Use DestroyImmediate so the removal takes effect before we add HLG below —
        // Destroy() is deferred and would leave both VLG and HLG active on the same frame,
        // causing the VLG to keep driving vertical layout.
        foreach (LayoutGroup lg in container.GetComponents<LayoutGroup>())
        {
            if (lg is not HorizontalLayoutGroup)
                DestroyImmediate(lg);
        }

        HorizontalLayoutGroup layout = container.GetOrAddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        // Anchor to panel top-left; pivot at left-centre so the container slides vertically.
        container.anchorMin = new Vector2(0f, 1f);
        container.anchorMax = new Vector2(0f, 1f);
        container.pivot = new Vector2(0f, 0.5f);
        container.sizeDelta = new Vector2(slotSize.x * 6f + 12f * 5f, slotSize.y);
        container.anchoredPosition = ExpandedSlotPosition;
        return container;
    }

    private Button EnsureBackpackButton()
    {
        Transform child = transform.Find("BackpackButton");
        Button button = child ? child.GetComponent<Button>() : null;
        RectTransform rect = null;
        if (!button)
        {
            GameObject buttonObject = new("BackpackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);

            buttonObject.GetComponent<Image>().color = slotColor;
            button = buttonObject.GetComponent<Button>();

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font)
            {
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
                label.font = font;
            }
        }

        rect = rect ? rect : button.transform as RectTransform;
        if (rect)
        {
            // Right end of the top bar, vertically centred. Keep it square.
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(slotSize.x, slotSize.x);
            rect.anchoredPosition = new Vector2(-12f, 0f);
        }

        return button;
    }

    private InventoryHotbarSlot EnsureCollectibleSlot()
    {
        Transform child = transform.Find("MemoryFragmentSlot");
        InventoryHotbarSlot slot = child ? child.GetComponent<InventoryHotbarSlot>() : null;
        if (!slot)
        {
            slot = InventoryHotbarSlot.Create(transform as RectTransform, slotSize);
            slot.name = "MemoryFragmentSlot";
        }

        RectTransform rect = slot.transform as RectTransform;
        if (rect)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = slotSize;
            rect.anchoredPosition = new Vector2(12f, 0f);
        }

        return slot;
    }

    private void ApplySlotLayout(InventoryHotbarSlot slot)
    {
        if (!slot) return;

        RectTransform rect = slot.transform as RectTransform;
        if (rect)
        {
            rect.sizeDelta = slotSize;
        }

        LayoutElement layout = slot.GetComponent<LayoutElement>();
        if (layout)
        {
            layout.preferredWidth = slotSize.x;
            layout.preferredHeight = slotSize.y;
        }
    }

    private bool TryGetExactSlotIndex(Vector2 screenPosition, Camera eventCamera, out int slotIndex)
    {
        slotIndex = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i] || !slots[i].gameObject.activeInHierarchy) continue;
            RectTransform slotRect = slots[i].transform as RectTransform;
            if (slotRect && RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, eventCamera))
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool IsInventoryZone(Vector2 screenPosition)
    {
        Camera eventCamera = GetEventCamera();
        if (backpackButton && RectTransformUtility.RectangleContainsScreenPoint(
                backpackButton.transform as RectTransform, screenPosition, eventCamera))
            return true;

        if (!collapsed && TryGetExactSlotIndex(screenPosition, eventCamera, out _))
            return true;

        return panel && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, eventCamera);
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        RectTransform canvasRect = RootCanvas.transform as RectTransform;
        Camera eventCamera = GetEventCamera();
        if (!canvasRect || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
            return screenPosition;

        return localPosition;
    }

    private Camera GetEventCamera()
    {
        return RootCanvas && RootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? RootCanvas.worldCamera : null;
    }

    private int GetInventoryIndexForSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < slotInventoryIndexes.Count ? slotInventoryIndexes[slotIndex] : -1;
    }
}
