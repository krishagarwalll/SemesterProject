using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanel : MonoBehaviour
{
    private const string ContentName = "SettingsContent";

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteSaveButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI saveStatusLabel;

    public event System.Action BackRequested;

    private void Awake()
    {
        EnsurePanelCanReceiveInput();
        RebuildUI();
    }

    private void OnEnable()
    {
        RefreshValues();
        WireButtons();
    }

    private void OnDisable()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        if (musicSlider) musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        if (windowModeDropdown) windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);
        if (saveButton) saveButton.onClick.RemoveListener(OnSaveClicked);
        if (loadButton) loadButton.onClick.RemoveListener(OnLoadClicked);
        if (deleteSaveButton) deleteSaveButton.onClick.RemoveListener(OnDeleteSaveClicked);
        if (backButton) backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void RefreshValues()
    {
        if (AudioManager.Instance)
        {
            masterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        windowModeDropdown.SetValueWithoutNotify(GetCurrentWindowModeIndex());
        windowModeDropdown.RefreshShownValue();
        RefreshSaveStatus();
    }

    private void WireButtons()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);
        windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);

        saveButton.onClick.RemoveListener(OnSaveClicked);
        saveButton.onClick.AddListener(OnSaveClicked);
        loadButton.onClick.RemoveListener(OnLoadClicked);
        loadButton.onClick.AddListener(OnLoadClicked);
        deleteSaveButton.onClick.RemoveListener(OnDeleteSaveClicked);
        deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
        backButton.onClick.RemoveListener(OnBackClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnMasterSliderChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
    private void OnMusicSliderChanged(float value) => AudioManager.Instance?.SetMusicVolume(value);
    private void OnSfxSliderChanged(float value) => AudioManager.Instance?.SetSfxVolume(value);
    private void OnBackClicked() => BackRequested?.Invoke();

    private void OnSaveClicked()
    {
        SaveManager.Instance?.Save();
        RefreshSaveStatus();
    }

    private void OnLoadClicked()
    {
        SaveManager.Instance?.LoadAndApply();
        RefreshSaveStatus();
    }

    private void OnDeleteSaveClicked()
    {
        SaveManager.Instance?.DeleteSave();
        RefreshSaveStatus();
    }

    private void RefreshSaveStatus()
    {
        bool hasSave = SaveManager.Instance && SaveManager.Instance.HasSave();
        loadButton.interactable = hasSave;
        deleteSaveButton.interactable = hasSave;
        saveStatusLabel.text = hasSave ? "Save data found" : "No save data";
    }

    private static int GetCurrentWindowModeIndex()
    {
        return Screen.fullScreenMode switch
        {
            FullScreenMode.Windowed => 0,
            FullScreenMode.ExclusiveFullScreen => 1,
            FullScreenMode.FullScreenWindow => 2,
            _ => Screen.fullScreen ? 2 : 0
        };
    }

    private static void OnWindowModeChanged(int index)
    {
        FullScreenMode mode = index switch
        {
            1 => FullScreenMode.ExclusiveFullScreen,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };

        Screen.fullScreenMode = mode;
        Screen.fullScreen = mode != FullScreenMode.Windowed;
        PlayerPrefs.SetInt("Settings_WindowMode", index);
        PlayerPrefs.Save();
    }

    private void EnsurePanelCanReceiveInput()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
        }

        CanvasGroup group = GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void RebuildUI()
    {
        RectTransform root = transform as RectTransform;
        if (!root) return;

        Transform existing = transform.Find(ContentName);
        if (existing)
        {
            Destroy(existing.gameObject);
        }

        Image background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.09f, 0.1f, 0.96f);

        GameObject contentObject = new(ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(root, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(72f, 56f);
        content.offsetMax = new Vector2(-72f, -56f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateTitle(content, "Options");
        masterSlider = CreateLabeledSlider(content, "Master Volume", AudioManager.Instance ? AudioManager.Instance.MasterVolume : 1f);
        musicSlider = CreateLabeledSlider(content, "Music Volume", AudioManager.Instance ? AudioManager.Instance.MusicVolume : 0.6f);
        sfxSlider = CreateLabeledSlider(content, "SFX Volume", AudioManager.Instance ? AudioManager.Instance.SfxVolume : 1f);
        windowModeDropdown = CreateWindowModeDropdown(content);
        saveStatusLabel = CreateStatusLabel(content);

        RectTransform saveRow = CreateRow(content, "SaveActions", 58f);
        saveButton = CreateButton(saveRow, "Save", new Vector2(150f, 52f));
        loadButton = CreateButton(saveRow, "Load", new Vector2(150f, 52f));
        deleteSaveButton = CreateButton(saveRow, "Delete Save", new Vector2(210f, 52f));
        backButton = CreateButton(content, "Back", new Vector2(220f, 58f));
    }

    private static void CreateTitle(RectTransform parent, string text)
    {
        GameObject go = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        go.GetComponent<LayoutElement>().preferredHeight = 62f;
    }

    private static Slider CreateLabeledSlider(RectTransform parent, string label, float value)
    {
        RectTransform row = CreateRow(parent, label + "Row", 54f);
        CreateRowLabel(row, label);

        Slider slider = CreateSlider(row);
        slider.SetValueWithoutNotify(value);
        return slider;
    }

    private static TMP_Dropdown CreateWindowModeDropdown(RectTransform parent)
    {
        RectTransform row = CreateRow(parent, "WindowModeRow", 54f);
        CreateRowLabel(row, "Window Mode");

        GameObject go = new("WindowModeDropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
        go.transform.SetParent(row, false);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.25f, 1f);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 44f;

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = image;
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Window", "Fullscreen", "Fullscreen Borderless" });
        dropdown.captionText = CreateDropdownText(go.transform, "Label");
        RectTransform template = CreateDropdownTemplate(go.transform, out TextMeshProUGUI itemText);
        dropdown.template = template;
        dropdown.itemText = itemText;
        dropdown.SetValueWithoutNotify(GetCurrentWindowModeIndex());
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static RectTransform CreateRow(RectTransform parent, string name, float height)
    {
        GameObject rowObject = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.SetParent(parent, false);
        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        rowObject.GetComponent<LayoutElement>().preferredHeight = height;
        return row;
    }

    private static void CreateRowLabel(RectTransform parent, string text)
    {
        GameObject go = new(text + "Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        go.GetComponent<LayoutElement>().preferredWidth = 220f;
    }

    private static Slider CreateSlider(RectTransform parent)
    {
        GameObject go = new("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 36f;

        RectTransform background = CreateImage(rect, "Background", new Color(0.18f, 0.2f, 0.22f, 1f));
        background.anchorMin = new Vector2(0f, 0.35f);
        background.anchorMax = new Vector2(1f, 0.65f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(rect, false);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;
        RectTransform fill = CreateImage(fillArea, "Fill", new Color(0.35f, 0.75f, 1f, 1f));

        RectTransform handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(rect, false);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);
        RectTransform handle = CreateImage(handleArea, "Handle", Color.white);
        handle.sizeDelta = new Vector2(22f, 0f);

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static RectTransform CreateImage(RectTransform parent, string name, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private static TextMeshProUGUI CreateStatusLabel(RectTransform parent)
    {
        GameObject go = new("SaveStatus", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.82f, 0.86f, 0.9f, 1f);
        go.GetComponent<LayoutElement>().preferredHeight = 28f;
        return label;
    }

    private static Button CreateButton(RectTransform parent, string label, Vector2 size)
    {
        GameObject go = new(label.Replace(" ", "") + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.25f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.34f, 0.38f, 1f);
        colors.pressedColor = new Color(0.12f, 0.15f, 0.18f, 1f);
        button.colors = colors;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        TextMeshProUGUI text = CreateDropdownText(go.transform, "Label");
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static TextMeshProUGUI CreateDropdownText(Transform parent, string name)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 0f);
        rect.offsetMax = new Vector2(-12f, 0f);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateDropdownTemplate(Transform parent, out TextMeshProUGUI itemText)
    {
        GameObject templateObject = new("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform template = templateObject.GetComponent<RectTransform>();
        template.SetParent(parent, false);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, 2f);
        template.sizeDelta = new Vector2(0f, 150f);
        templateObject.SetActive(false);
        templateObject.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.14f, 1f);

        RectTransform viewport = CreateImage(template, "Viewport", new Color(0.1f, 0.12f, 0.14f, 1f));
        viewport.gameObject.AddComponent<RectMask2D>();

        GameObject contentObject = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        contentObject.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject itemObject = new("Item", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        RectTransform item = itemObject.GetComponent<RectTransform>();
        item.SetParent(content, false);
        itemObject.GetComponent<LayoutElement>().preferredHeight = 38f;
        RectTransform itemBackground = CreateImage(item, "Item Background", new Color(0.18f, 0.22f, 0.25f, 1f));
        RectTransform checkmark = CreateImage(item, "Item Checkmark", new Color(0.35f, 0.75f, 1f, 1f));
        checkmark.anchorMin = new Vector2(0f, 0.5f);
        checkmark.anchorMax = new Vector2(0f, 0.5f);
        checkmark.anchoredPosition = new Vector2(18f, 0f);
        checkmark.sizeDelta = new Vector2(12f, 12f);

        itemText = CreateDropdownText(item, "Item Label");
        itemText.margin = new Vector4(28f, 0f, 0f, 0f);
        Toggle toggle = itemObject.GetComponent<Toggle>();
        toggle.targetGraphic = itemBackground.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();

        ScrollRect scroll = templateObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        return template;
    }
}
