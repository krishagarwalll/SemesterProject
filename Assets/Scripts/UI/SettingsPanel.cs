using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject graphicsQualityRow;

    public event System.Action BackRequested;

    private void Awake()
    {
        EnsurePanelCanReceiveInput();
        if (!musicSlider || !sfxSlider || !backButton || !fullscreenToggle || !qualityDropdown)
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

        if (fullscreenToggle)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        RefreshQualityDropdown();
    }

    private void OnDisable()
    {
        if (musicSlider) musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
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

    private void RefreshQualityDropdown()
    {
        if (!qualityDropdown)
        {
            return;
        }

        qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private static void OnQualityChanged(int qualityIndex)
    {
        if (qualityIndex < 0 || qualityIndex >= QualitySettings.names.Length)
        {
            return;
        }

        QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
        PlayerPrefs.SetInt("Settings_Quality", qualityIndex);
        PlayerPrefs.Save();
    }

    private static void OnFullscreenChanged(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt("Settings_Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnBackClicked() => BackRequested?.Invoke();

    private void EnsurePanelCanReceiveInput()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
        }

        if (!TryGetComponent(out CanvasGroup group))
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        if (!group)
        {
            Debug.LogWarning("[SettingsPanel] Missing CanvasGroup; settings UI raycasts may not work.", this);
            return;
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

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
        qualityDropdown = CreateLabeledDropdown(contentRT, "Quality", QualitySettings.names, QualitySettings.GetQualityLevel());
        graphicsQualityRow = qualityDropdown ? qualityDropdown.transform.parent.gameObject : null;
        fullscreenToggle = CreateLabeledToggle(contentRT, "Fullscreen", Screen.fullScreen);
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

    private static TMP_Dropdown CreateLabeledDropdown(RectTransform parent, string label, string[] options, int value)
    {
        GameObject rowGO = CreateRow(parent, label);
        CreateRowLabel(rowGO.transform, label);

        GameObject dropdownGO = new("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
        dropdownGO.transform.SetParent(rowGO.transform, false);
        Image image = dropdownGO.GetComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.22f, 1f);

        LayoutElement layout = dropdownGO.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 44f;

        TMP_Dropdown dropdown = dropdownGO.GetComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        dropdown.AddOptions(new List<string>(options));
        dropdown.value = Mathf.Clamp(value, 0, Mathf.Max(0, options.Length - 1));
        dropdown.targetGraphic = image;

        TextMeshProUGUI caption = CreateDropdownText(dropdownGO.transform, "Label", TextAlignmentOptions.MidlineLeft);
        caption.margin = new Vector4(12f, 0f, 32f, 0f);
        dropdown.captionText = caption;

        RectTransform template = CreateDropdownTemplate(dropdownGO.transform, out TextMeshProUGUI item);
        dropdown.template = template;
        dropdown.itemText = item;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static RectTransform CreateDropdownTemplate(Transform parent, out TextMeshProUGUI itemLabel)
    {
        GameObject templateGO = new("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform template = templateGO.GetComponent<RectTransform>();
        template.SetParent(parent, false);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, 2f);
        template.sizeDelta = new Vector2(0f, 180f);
        templateGO.SetActive(false);

        Image templateImage = templateGO.GetComponent<Image>();
        templateImage.color = new Color(0.12f, 0.14f, 0.16f, 1f);

        GameObject viewportGO = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        RectTransform viewport = viewportGO.GetComponent<RectTransform>();
        viewport.SetParent(template, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.16f, 1f);

        GameObject contentGO = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform content = contentGO.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject itemGO = new("Item", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        RectTransform item = itemGO.GetComponent<RectTransform>();
        item.SetParent(content, false);
        item.sizeDelta = new Vector2(0f, 36f);
        itemGO.GetComponent<LayoutElement>().preferredHeight = 36f;

        GameObject backgroundGO = new("Item Background", typeof(RectTransform), typeof(Image));
        RectTransform background = backgroundGO.GetComponent<RectTransform>();
        background.SetParent(item, false);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundGO.GetComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.22f, 1f);

        GameObject checkGO = new("Item Checkmark", typeof(RectTransform), typeof(Image));
        RectTransform check = checkGO.GetComponent<RectTransform>();
        check.SetParent(item, false);
        check.anchorMin = new Vector2(0f, 0.5f);
        check.anchorMax = new Vector2(0f, 0.5f);
        check.anchoredPosition = new Vector2(18f, 0f);
        check.sizeDelta = new Vector2(12f, 12f);
        Image checkImage = checkGO.GetComponent<Image>();
        checkImage.color = new Color(0.2f, 0.55f, 1f, 1f);

        itemLabel = CreateDropdownText(item, "Item Label", TextAlignmentOptions.MidlineLeft);
        itemLabel.margin = new Vector4(38f, 0f, 8f, 0f);

        Toggle toggle = itemGO.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkImage;

        ScrollRect scrollRect = templateGO.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        return template;
    }

    private static Toggle CreateLabeledToggle(RectTransform parent, string label, bool value)
    {
        GameObject rowGO = CreateRow(parent, label);
        CreateRowLabel(rowGO.transform, label);

        GameObject toggleGO = new("Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        toggleGO.transform.SetParent(rowGO.transform, false);
        LayoutElement layout = toggleGO.GetComponent<LayoutElement>();
        layout.preferredWidth = 56f;
        layout.preferredHeight = 44f;

        GameObject backgroundGO = new("Background", typeof(RectTransform), typeof(Image));
        RectTransform background = backgroundGO.GetComponent<RectTransform>();
        background.SetParent(toggleGO.transform, false);
        background.anchorMin = new Vector2(0.5f, 0.5f);
        background.anchorMax = new Vector2(0.5f, 0.5f);
        background.sizeDelta = new Vector2(36f, 36f);
        Image backgroundImage = backgroundGO.GetComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.2f, 0.22f, 1f);

        GameObject checkGO = new("Checkmark", typeof(RectTransform), typeof(Image));
        RectTransform check = checkGO.GetComponent<RectTransform>();
        check.SetParent(background, false);
        check.anchorMin = new Vector2(0.5f, 0.5f);
        check.anchorMax = new Vector2(0.5f, 0.5f);
        check.sizeDelta = new Vector2(20f, 20f);
        Image checkImage = checkGO.GetComponent<Image>();
        checkImage.color = new Color(0.2f, 0.55f, 1f, 1f);

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkImage;
        toggle.isOn = value;
        return toggle;
    }

    private static GameObject CreateRow(RectTransform parent, string label)
    {
        GameObject rowGO = new(label + "_Row", typeof(RectTransform));
        rowGO.GetComponent<RectTransform>().SetParent(parent, false);
        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = false;
        LayoutElement rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 50;
        return rowGO;
    }

    private static void CreateRowLabel(Transform parent, string label)
    {
        GameObject labelGO = new(label, typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 220;
    }

    private static TextMeshProUGUI CreateDropdownText(Transform parent, string name, TextAlignmentOptions alignment)
    {
        GameObject textGO = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(parent, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 24f;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
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
