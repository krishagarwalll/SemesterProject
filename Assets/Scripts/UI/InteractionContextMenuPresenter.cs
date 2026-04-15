using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionContextMenuPresenter : MonoBehaviour
{
    private const float DefaultMinPanelWidth = 260f;
    private const float DefaultRowHeight = 36f;

    [SerializeField] private PointerContext pointer;
    [SerializeField] private PointClickController controller;
    [SerializeField] private Inventory inventory;
    [SerializeField] private InputGlyphLibrary glyphLibrary;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private Button rowTemplate;
    [SerializeField, Min(120f)] private float minPanelWidth = DefaultMinPanelWidth;
    [SerializeField, Min(24f)] private float rowHeight = DefaultRowHeight;

    private readonly List<Button> rowCache = new();
    private readonly List<InteractionTarget> targets = new();
    private readonly List<InteractionTarget> hoveredTargets = new();
    private readonly List<InteractionAction> actions = new();
    private readonly List<MenuRowAction> menuActions = new();
    private InteractionTarget currentTarget;
    private Canvas rootCanvas;

    private PointerContext Pointer => pointer ? pointer : pointer = FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include);
    private PointClickController Controller => controller ? controller : controller = FindFirstObjectByType<PointClickController>(FindObjectsInactive.Include);
    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private RectTransform Panel => panel ? panel : panel = transform as RectTransform;
    private CanvasGroup Group => canvasGroup ? canvasGroup : canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
    private RectTransform RowContainer => rowContainer ? rowContainer : rowContainer = transform as RectTransform;
    private Canvas RootCanvas => rootCanvas ? rootCanvas : rootCanvas = GetComponentInParent<Canvas>();

    private void OnEnable()
    {
        EnsureLayout();
        if (Pointer)
        {
            Pointer.SecondaryPressed += HandleSecondaryPressed;
            Pointer.PrimaryPressed += HandlePrimaryPressed;
        }

        Hide();
    }

    private void OnDisable()
    {
        if (pointer)
        {
            pointer.SecondaryPressed -= HandleSecondaryPressed;
            pointer.PrimaryPressed -= HandlePrimaryPressed;
        }

        Hide();
    }

    private void HandleSecondaryPressed(PointerContext context)
    {
        if (!Controller || !context)
        {
            Hide();
            return;
        }

        BuildMenuActions(context);
        if (menuActions.Count == 0)
        {
            Hide();
            return;
        }

        Show(context.ScreenPosition);
    }

    private void HandlePrimaryPressed(PointerContext context)
    {
        if (!IsVisible)
        {
            return;
        }

        Camera eventCamera = RootCanvas && RootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? RootCanvas.worldCamera : null;
        if (Panel && RectTransformUtility.RectangleContainsScreenPoint(Panel, context.ScreenPosition, eventCamera))
        {
            return;
        }

        if (IsVisible)
        {
            Hide();
        }
    }

    private void Show(Vector2 screenPosition)
    {
        if (!Panel)
        {
            return;
        }

        Pointer?.SetContextMenuOpen(true);
        RebuildRows();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(Panel);
        SetVisible(true);
        Panel.position = screenPosition;
        ClampToCanvas();
    }

    private void Hide()
    {
        if (!Panel)
        {
            return;
        }

        currentTarget = null;
        menuActions.Clear();
        Pointer?.SetContextMenuOpen(false);
        SetVisible(false);
    }

    private void RebuildRows()
    {
        EnsureRows(menuActions.Count);
        for (int i = 0; i < rowCache.Count; i++)
        {
            bool active = i < menuActions.Count;
            rowCache[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            MenuRowAction rowAction = menuActions[i];
            InteractionAction action = rowAction.Action;
            rowCache[i].onClick.RemoveAllListeners();
            rowCache[i].interactable = action.Enabled;
            TextMeshProUGUI label = rowCache[i].GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = rowAction.DisplayLabel;
            if (glyphLibrary)
            {
                label.font = glyphLibrary.FontAsset;
            }

            Image glyph = rowCache[i].transform.Find("Glyph") ? rowCache[i].transform.Find("Glyph").GetComponent<Image>() : null;
            if (glyph)
            {
                if (glyphLibrary && glyphLibrary.TryResolve(action.GlyphId, out Sprite sprite, out _))
                {
                    glyph.enabled = sprite;
                    glyph.sprite = sprite;
                }
                else
                {
                    glyph.enabled = false;
                }
            }

            rowCache[i].onClick.AddListener(() =>
            {
                if (Controller && rowAction.Target)
                {
                    Controller.ExecuteAction(rowAction.Target, rowAction.Action);
                }

                Hide();
            });
        }
    }

    private void BuildMenuActions(PointerContext context)
    {
        currentTarget = null;
        menuActions.Clear();
        targets.Clear();

        if (context.SecondaryClickedTarget)
        {
            targets.Add(context.SecondaryClickedTarget);
        }

        context.GetHoveredTargets(hoveredTargets);
        for (int i = 0; i < hoveredTargets.Count; i++)
        {
            InteractionTarget hovered = hoveredTargets[i];
            if (hovered && !targets.Contains(hovered))
            {
                targets.Add(hovered);
            }
        }

        bool showTargetName = targets.Count > 1;
        for (int i = 0; i < targets.Count; i++)
        {
            InteractionTarget target = targets[i];
            if (!target)
            {
                continue;
            }

            currentTarget ??= target;
            Controller.GetAvailableActions(target, actions);
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                InteractionAction action = actions[actionIndex];
                if (!ShouldShowInContextMenu(action))
                {
                    continue;
                }

                string label = showTargetName ? $"{action.Label} - {target.name}" : action.Label;
                menuActions.Add(new MenuRowAction(target, action, label));
            }
        }
    }

    private static bool ShouldShowInContextMenu(in InteractionAction action)
    {
        return action.IsValid && action.Mode != InteractionMode.Primary && action.Mode != InteractionMode.Drag;
    }

    private void EnsureRows(int count)
    {
        if (!rowTemplate)
        {
            rowTemplate = CreateDefaultRow();
        }

        while (rowCache.Count < count)
        {
            Button row = Instantiate(rowTemplate, RowContainer);
            row.gameObject.SetActive(true);
            rowCache.Add(row);
        }
    }

    private Button CreateDefaultRow()
    {
        GameObject rowObject = new("ContextRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, rowHeight);
        Image image = rowObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        Button button = rowObject.GetComponent<Button>();
        rowObject.transform.SetParent(RowContainer, false);
        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = rowHeight;
        layoutElement.preferredHeight = rowHeight;
        layoutElement.minWidth = minPanelWidth - 16f;
        layoutElement.preferredWidth = minPanelWidth - 16f;

        GameObject glyphObject = new("Glyph", typeof(RectTransform), typeof(Image));
        RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
        glyphRect.SetParent(rect, false);
        glyphRect.anchorMin = new Vector2(0f, 0.5f);
        glyphRect.anchorMax = new Vector2(0f, 0.5f);
        glyphRect.pivot = new Vector2(0f, 0.5f);
        glyphRect.anchoredPosition = new Vector2(12f, 0f);
        glyphRect.sizeDelta = new Vector2(20f, 20f);

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(40f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.font = glyphLibrary ? glyphLibrary.FontAsset : TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        button.gameObject.SetActive(false);
        return button;
    }

    private void EnsureLayout()
    {
        if (!Panel || !RowContainer)
        {
            return;
        }

        LayoutElement panelLayout = Panel.GetOrAddComponent<LayoutElement>();
        panelLayout.minWidth = minPanelWidth;
        panelLayout.preferredWidth = minPanelWidth;

        VerticalLayoutGroup layout = RowContainer.GetOrAddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = Panel.GetOrAddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Panel.sizeDelta = new Vector2(Mathf.Max(Panel.sizeDelta.x, minPanelWidth), Panel.sizeDelta.y);
    }

    private void SetVisible(bool visible)
    {
        if (Group)
        {
            Group.alpha = visible ? 1f : 0f;
            Group.blocksRaycasts = visible;
            Group.interactable = visible;
        }
        else if (Panel)
        {
            Panel.gameObject.SetActive(visible);
        }
    }

    private bool IsVisible => Group ? Group.alpha > 0.001f : Panel && Panel.gameObject.activeSelf;

    private void ClampToCanvas()
    {
        if (!Panel || !RootCanvas || RootCanvas.renderMode == RenderMode.WorldSpace)
        {
            return;
        }

        RectTransform canvasRect = RootCanvas.transform as RectTransform;
        Camera eventCamera = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : RootCanvas.worldCamera;
        if (!canvasRect || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Panel.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfPanel = Panel.rect.size * 0.5f;
        localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvas.x + halfPanel.x, halfCanvas.x - halfPanel.x);
        localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvas.y + halfPanel.y, halfCanvas.y - halfPanel.y);
        Panel.localPosition = localPoint;
    }

    private readonly struct MenuRowAction
    {
        public MenuRowAction(InteractionTarget target, InteractionAction action, string displayLabel)
        {
            Target = target;
            Action = action;
            DisplayLabel = displayLabel;
        }

        public InteractionTarget Target { get; }
        public InteractionAction Action { get; }
        public string DisplayLabel { get; }
    }
}
