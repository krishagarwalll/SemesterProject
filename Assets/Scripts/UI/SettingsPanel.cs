using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject graphicsQualityRow;

    public event System.Action BackRequested;

    private void Awake()
    {
        if (!musicSlider || !sfxSlider || !backButton)
            BuildUI();
    }

    private void OnEnable()
    {
#if UNITY_WEBGL
        if (graphicsQualityRow) graphicsQualityRow.SetActive(false);
#endif
        RefreshSliders();
        if (backButton)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void OnDisable()
    {
        if (musicSlider) musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        if (backButton) backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void RefreshSliders()
    {
        if (!AudioManager.Instance) return;
        if (musicSlider)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider)
        {
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
    }

    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance) AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (AudioManager.Instance) AudioManager.Instance.SetSfxVolume(value);
    }

    private void OnBackClicked() => BackRequested?.Invoke();

    // ── Procedural UI builder ────────────────────────────────────
    // Runs when Inspector references are not wired (freshly placed prefab).

    private void BuildUI()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (!root) return;

        if (!GetComponent<Image>())
        {
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        }

        GameObject contentGO = new("Content");
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.SetParent(root, false);
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(80, 80);
        contentRT.offsetMax = new Vector2(-80, -80);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 30;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateLabel(contentRT, "Settings", 40);
        musicSlider = CreateLabeledSlider(contentRT, "Music Volume", 0f, 1f,
            AudioManager.Instance ? AudioManager.Instance.MusicVolume : 0.6f);
        sfxSlider = CreateLabeledSlider(contentRT, "SFX Volume", 0f, 1f,
            AudioManager.Instance ? AudioManager.Instance.SfxVolume : 1f);
        backButton = CreateButton(contentRT, "Back", new Vector2(200, 60));
    }

    private static void CreateLabel(RectTransform parent, string text, float fontSize)
    {
        GameObject go = new(text + "_Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 1.5f;
    }

    private static Slider CreateLabeledSlider(RectTransform parent, string label, float min, float max, float value)
    {
        GameObject rowGO = new(label + "_Row", typeof(RectTransform));
        rowGO.GetComponent<RectTransform>().SetParent(parent, false);
        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;
        LayoutElement rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 50;

        GameObject labelGO = new(label, typeof(RectTransform));
        labelGO.transform.SetParent(rowGO.transform, false);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 220;

        Slider slider = CreateSlider(rowGO.GetComponent<RectTransform>(), min, max, value);
        LayoutElement sliderLE = slider.gameObject.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1;
        sliderLE.preferredHeight = 30;

        return slider;
    }

    private static Slider CreateSlider(RectTransform parent, float min, float max, float value)
    {
        GameObject go = new("Slider", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        // Background
        GameObject bgGO = new("Background", typeof(RectTransform));
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.SetParent(rt, false);
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.sizeDelta = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // Fill area + fill
        GameObject fillAreaGO = new("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.SetParent(rt, false);
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-15, 0);

        GameObject fillGO = new("Fill", typeof(RectTransform));
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(fillAreaRT, false);
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.sizeDelta = new Vector2(10, 0);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.55f, 1f, 1f);

        // Handle area + handle
        GameObject handleAreaGO = new("Handle Slide Area", typeof(RectTransform));
        RectTransform handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.SetParent(rt, false);
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0);
        handleAreaRT.offsetMax = new Vector2(-10, 0);

        GameObject handleGO = new("Handle", typeof(RectTransform));
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.SetParent(handleAreaRT, false);
        handleRT.sizeDelta = new Vector2(20, 0);
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = new Vector2(0f, 1f);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        return slider;
    }

    private static Button CreateButton(RectTransform parent, string label, Vector2 size)
    {
        GameObject go = new(label + "Button", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.45f, 0.45f, 0.5f, 1f);
        cb.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        btn.colors = cb;

        GameObject textGO = new("Text", typeof(RectTransform));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(rt, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }
}
