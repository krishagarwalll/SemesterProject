using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AutoSaveIndicator : MonoBehaviour
{
    [SerializeField, Min(0f)] private float visibleSeconds = 1.5f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.35f;
    [SerializeField] private CanvasGroup group;

    private float visibleTimer;
    private float alpha;
    private RectTransform panel;

    private void Awake()
    {
        if (!group)
        {
            group = GetComponentInChildren<CanvasGroup>(true);
        }

        Restyle();
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

    private void SetAlpha(float value)
    {
        if (!group)
        {
            return;
        }

        group.alpha = value;
    }

    private void Restyle()
    {
        if (!group) return;

        panel = group.GetComponent<RectTransform>();
        if (panel)
        {
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-28f, 28f);
            panel.sizeDelta = new Vector2(224f, 52f);
        }

        Image background = group.GetComponent<Image>();
        if (background)
        {
            background.color = new Color(0.055f, 0.047f, 0.07f, 0.94f);
            background.raycastTarget = false;
        }

        TextMeshProUGUI label = group.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!label) return;

        label.text = "GAME SAVED";
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.96f, 0.93f, 0.86f, 1f);
        label.raycastTarget = false;
    }
}
