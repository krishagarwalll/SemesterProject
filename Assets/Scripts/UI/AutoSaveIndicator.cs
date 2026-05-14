using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AutoSaveIndicator : MonoBehaviour
{
    [SerializeField, Min(0f)] private float visibleSeconds = 1.5f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.35f;

    private CanvasGroup group;
    private float visibleTimer;
    private float alpha;

    private void Awake()
    {
        EnsureIndicator();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        SaveManager.SaveWritten += Show;
    }

    private void OnDisable()
    {
        SaveManager.SaveWritten -= Show;
    }

    private void Update()
    {
        if (!group)
        {
            return;
        }

        if (visibleTimer > 0f)
        {
            visibleTimer -= Time.unscaledDeltaTime;
            alpha = Mathf.MoveTowards(alpha, 1f, Time.unscaledDeltaTime / fadeSeconds);
        }
        else
        {
            alpha = Mathf.MoveTowards(alpha, 0f, Time.unscaledDeltaTime / fadeSeconds);
        }

        SetAlpha(alpha);
    }

    private void Show()
    {
        visibleTimer = visibleSeconds;
        enabled = true;
    }

    private void EnsureIndicator()
    {
        Transform existing = transform.Find("AutoSaveIndicator");
        if (existing)
        {
            group = existing.GetComponent<CanvasGroup>() ?? existing.gameObject.AddComponent<CanvasGroup>();
            return;
        }

        GameObject indicator = new("AutoSaveIndicator", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rect = indicator.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-32f, 32f);
        rect.sizeDelta = new Vector2(220f, 58f);

        Image background = indicator.GetComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.08f, 0.78f);
        background.raycastTarget = false;

        group = indicator.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject icon = new("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);
        Image iconImage = icon.GetComponent<Image>();
        iconImage.color = new Color(0.35f, 0.75f, 1f, 1f);
        iconImage.raycastTarget = false;

        GameObject iconLabelObject = new("IconLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform iconLabelRect = iconLabelObject.GetComponent<RectTransform>();
        iconLabelRect.SetParent(iconRect, false);
        iconLabelRect.anchorMin = Vector2.zero;
        iconLabelRect.anchorMax = Vector2.one;
        iconLabelRect.offsetMin = Vector2.zero;
        iconLabelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI iconLabel = iconLabelObject.GetComponent<TextMeshProUGUI>();
        iconLabel.text = "S";
        iconLabel.fontSize = 18f;
        iconLabel.alignment = TextAlignmentOptions.Center;
        iconLabel.color = new Color(0.02f, 0.04f, 0.05f, 1f);
        iconLabel.raycastTarget = false;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(56f, 0f);
        labelRect.offsetMax = new Vector2(-14f, 0f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Autosaved";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void SetAlpha(float value)
    {
        if (!group)
        {
            return;
        }

        group.alpha = value;
    }
}
