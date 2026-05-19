using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CutsceneSkipButtonPresenter
{
    private const string RuntimeButtonName = "SkipCutsceneButton";

    private readonly GameObject cutsceneRoot;
    private Button button;
    private CanvasGroup group;
    private float revealAfterSeconds;
    private float fadeDuration;
    private float heldFastForwardSeconds;
    private float fade;
    private bool revealed;

    public CutsceneSkipButtonPresenter(
        GameObject cutsceneRoot,
        Button configuredButton,
        float revealAfterSeconds,
        float fadeDuration)
    {
        this.cutsceneRoot = cutsceneRoot;
        button = configuredButton;
        this.revealAfterSeconds = Mathf.Max(0f, revealAfterSeconds);
        this.fadeDuration = Mathf.Max(0.01f, fadeDuration);
    }

    public Button Button => button;

    public void Prepare()
    {
        if (!button)
            button = CreateRuntimeButton();

        if (!button) return;

        group = button.GetComponent<CanvasGroup>();
        if (!group)
            group = button.gameObject.AddComponent<CanvasGroup>();

        SetAlpha(0f);
    }

    public void Tick(bool fastForwardHeld)
    {
        if (!group) return;

        if (!revealed && fastForwardHeld)
        {
            heldFastForwardSeconds += Time.unscaledDeltaTime;
            revealed = heldFastForwardSeconds >= revealAfterSeconds;
        }

        float target = revealed ? 1f : 0f;
        fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime / fadeDuration);
        SetAlpha(fade);
    }

    private Button CreateRuntimeButton()
    {
        Canvas canvas = cutsceneRoot ? cutsceneRoot.GetComponentInChildren<Canvas>(true) : null;
        if (!canvas) return null;

        Transform existing = canvas.transform.Find(RuntimeButtonName);
        if (existing && existing.TryGetComponent(out Button existingButton))
            return existingButton;

        GameObject buttonObject = new(RuntimeButtonName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(CanvasGroup));

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        // Anchored to bottom-right corner
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-28f, 28f);
        rect.sizeDelta = new Vector2(160f, 52f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock c = button.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(0.88f, 0.92f, 1f, 1f);
        c.pressedColor = new Color(0.70f, 0.75f, 0.82f, 1f);
        c.selectedColor = new Color(0.88f, 0.92f, 1f, 1f);
        button.colors = c;

        // TMP label
        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Skip";
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        return button;
    }

    private void SetAlpha(float alpha)
    {
        if (!group) return;
        group.alpha = alpha;
        group.interactable = alpha >= 0.99f;
        group.blocksRaycasts = alpha >= 0.99f;
    }
}
