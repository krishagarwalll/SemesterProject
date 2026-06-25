using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WebGLPauseQuickActions : MonoBehaviour
{
    private const string RootName = "WebGLQuickActions";

    private TextMeshProUGUI fullscreenLabel;
    private TextMeshProUGUI muteLabel;
    private float refreshTimer;

    public static void EnsureCreated(GameObject pauseMenu)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!pauseMenu || pauseMenu.transform.Find(RootName)) return;

        GameObject root = new(RootName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(WebGLPauseQuickActions));
        root.transform.SetParent(pauseMenu.transform, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(360f, 68f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.055f, 0.047f, 0.07f, 0.94f);

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        WebGLPauseQuickActions actions = root.GetComponent<WebGLPauseQuickActions>();
        actions.fullscreenLabel = CreateButton(root.transform, "Fullscreen", actions.ToggleFullscreen);
        actions.muteLabel = CreateButton(root.transform, "Mute", actions.ToggleMute);
        actions.RefreshLabels();
#endif
    }

    private void Update()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = 0.25f;
        RefreshLabels();
    }

    private void ToggleFullscreen()
    {
        WebGLDisplay.ToggleFullscreen();
        RefreshLabels();
    }

    private void ToggleMute()
    {
        AudioManager.Instance?.ToggleMute();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (fullscreenLabel)
            fullscreenLabel.text = WebGLDisplay.IsFullscreen ? "EXIT FULLSCREEN" : "FULLSCREEN";

        if (muteLabel)
            muteLabel.text = AudioManager.Instance && AudioManager.Instance.IsMuted ? "UNMUTE" : "MUTE";
    }

    private static TextMeshProUGUI CreateButton(Transform parent, string name, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.24f, 0.18f, 0.3f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        LayoutElement element = buttonObject.GetComponent<LayoutElement>();
        element.preferredWidth = 168f;
        element.preferredHeight = 52f;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = name.ToUpperInvariant();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 20f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.96f, 0.93f, 0.86f, 1f);
        label.raycastTarget = false;
        return label;
    }
}
