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
        {
            button = CreateRuntimeButton();
        }

        if (!button)
        {
            return;
        }

        group = button.GetComponent<CanvasGroup>();
        if (!group)
        {
            group = button.gameObject.AddComponent<CanvasGroup>();
        }

        SetAlpha(0f);
    }

    public void Tick(bool fastForwardHeld)
    {
        if (!group)
        {
            return;
        }

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
        if (!canvas)
        {
            return null;
        }

        Transform existing = canvas.transform.Find(RuntimeButtonName);
        if (existing && existing.TryGetComponent(out Button existingButton))
        {
            return existingButton;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!font)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject buttonObject = new(RuntimeButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-32f, 32f);
        rect.sizeDelta = new Vector2(148f, 48f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = "Skip";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = 20;
        label.font = font;

        return buttonObject.GetComponent<Button>();
    }

    private void SetAlpha(float alpha)
    {
        if (!group)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = alpha >= 0.99f;
        group.blocksRaycasts = alpha >= 0.99f;
    }
}
