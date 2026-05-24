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
    [SerializeField] private Vector2 screenOffset = new(0f, 16f);

    private InteractionTarget currentTarget;

    private PointerContext Pointer => pointer ? pointer : pointer = FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include);
    private PoptropicaController Controller => controller ? controller : controller = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
    private Inventory SceneInventory => inventory ? inventory : inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
    private RectTransform Root => root ? root : root = transform as RectTransform;
    private CanvasGroup Group => canvasGroup ? canvasGroup : canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

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
        Evaluate();
    }

    private void HandleHoverChanged(InteractionTarget previous, InteractionTarget current)
    {
        currentTarget = current;
        Evaluate();
    }

    private void Refresh() => Evaluate();

    private void Evaluate()
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

        if (Controller.HasActiveInteraction || (Pointer && Pointer.IsDragging))
        {
            SetVisible(false);
            return;
        }

        InteractionContext context = new(Controller, Pointer, currentTarget, SceneInventory);
        if (!currentTarget.TryGetPromptAction(context, out InteractionAction action) || !action.Enabled)
        {
            SetVisible(false);
            return;
        }

        if (!action.RequiresApproach && !currentTarget.IsInRange(Controller.transform.position))
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        if (labelText)
        {
            labelText.text = action.Label;
        }

        ApplyGlyph(action.GlyphId);

        if (Pointer && Pointer.WorldCamera)
        {
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Pointer.WorldCamera, currentTarget.InteractionPoint.position) + screenOffset;
            RectTransform parentRect = Root.parent as RectTransform;
            if (parentRect)
            {
                Canvas parentCanvas = parentRect.GetComponentInParent<Canvas>();
                Camera uiCamera = parentCanvas && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, uiCamera, out Vector2 localPoint))
                {
                    Root.anchoredPosition = localPoint;
                }
            }
        }
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
