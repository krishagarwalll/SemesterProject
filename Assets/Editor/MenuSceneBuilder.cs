using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tools > UI > Build Main Menu       — run while the MainMenu scene is open.
/// Tools > UI > Build Settings Panel  — run while any scene containing a SettingsPanel is open.
/// Tools > UI > Create UI Prefabs     — saves MenuButton + SettingsPanel as .prefab assets.
/// After running, save the scene with Ctrl+S.
/// </summary>
public static class MenuSceneBuilder
{
    // ── Colour palette (identical to PauseMenuUI) ────────────────────────────
    static readonly Color PanelBg      = new(0.08f, 0.09f, 0.10f, 0.96f);
    static readonly Color BtnNormal    = new(0.18f, 0.22f, 0.25f, 1f);
    static readonly Color BtnHighlight = new(0.28f, 0.34f, 0.38f, 1f);
    static readonly Color BtnPressed   = new(0.12f, 0.15f, 0.18f, 1f);
    static readonly Color BtnDisabled  = new(0.12f, 0.15f, 0.18f, 0.5f);
    static readonly Color Overlay      = new(0f,    0f,    0f,    0.55f);
    static readonly Color MutedLabel   = new(0.60f, 0.65f, 0.72f, 1f);
    static readonly Color SubDivider   = new(1f,    1f,    1f,    0.06f);
    static readonly Color SliderTrack  = new(0.20f, 0.23f, 0.26f, 1f);
    static readonly Color SliderFill   = new(0.25f, 0.55f, 0.92f, 1f);

    // Anton SDF — the font already used by the pause menu canvas prefab
    const string FontGuid = "8a89fa14b10d46a99122fd4f73fca9a2";

    // ── Layout constants ─────────────────────────────────────────────────────
    const float BtnW      = 280f;   // standard menu button width
    const float BtnH      = 58f;    // standard menu button height
    const float BtnFontSz = 26f;    // button label size
    const float TitleSz   = 32f;    // panel title size
    const float SettingsPanelW = 620f;
    const float SettingsPanelH = 720f;
    const float SettingsInnerW = 540f;

    // ── Main Menu ────────────────────────────────────────────────────────────

    [MenuItem("Tools/UI/Build Main Menu")]
    static void BuildMainMenu()
    {
        TMP_FontAsset font = LoadFont();
        Canvas canvas     = FindOrCreateOverlayCanvas("Canvas", 0);
        var canvasRect    = (RectTransform)canvas.transform;

        DestroyChild(canvasRect, "MainMenuOverlay");
        DestroyChild(canvasRect, "SettingsPanelRoot");

        // Full-screen dimmer
        var overlayGo   = Child(canvasRect, "MainMenuOverlay", typeof(Image), typeof(CanvasGroup));
        var overlayRect = (RectTransform)overlayGo.transform;
        Stretch(overlayRect);
        overlayGo.GetComponent<Image>().color = Overlay;
        var cg = overlayGo.GetComponent<CanvasGroup>();
        cg.interactable = cg.blocksRaycasts = true;

        // Panel — same approach as pause menu: single VLG, centred
        var panelGo   = Child(overlayRect, "Panel", typeof(Image), typeof(VerticalLayoutGroup));
        var panelRect = (RectTransform)panelGo.transform;
        Centre(panelRect, new Vector2(420f, 400f));
        panelGo.GetComponent<Image>().color = PanelBg;

        var vl = panelGo.GetComponent<VerticalLayoutGroup>();
        vl.padding            = new RectOffset(40, 40, 36, 36);
        vl.spacing            = 18f;
        vl.childAlignment     = TextAnchor.MiddleCenter;
        vl.childControlWidth  = false;
        vl.childControlHeight = false;
        vl.childForceExpandWidth  = false;
        vl.childForceExpandHeight = false;

        // Title
        Label(panelRect, "Title", "How To Get To Heaven", TitleSz, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Center, 72f, font);

        // Buttons — Continue/Start are mutually exclusive; MainMenuUI shows the right one
        DarkButton(panelRect, "ContinueButton", "Continue", BtnW, BtnH, font);
        DarkButton(panelRect, "StartButton",    "Start",    BtnW, BtnH, font);
        DarkButton(panelRect, "SettingsButton", "Settings", BtnW, BtnH, font);
        DarkButton(panelRect, "ExitButton",     "Exit",     BtnW, BtnH, font);

        // Settings panel (hidden by default)
        var spRoot = BuildSettingsPanelHierarchy(canvasRect, font);
        spRoot.SetActive(false);

        // Wire MainMenuUI
        var mmui = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (!mmui) mmui = canvas.gameObject.AddComponent<MainMenuUI>();

        var so = new SerializedObject(mmui);
        so.FindProperty("menuPanel").objectReferenceValue         = overlayGo;
        so.FindProperty("settingsPanelRoot").objectReferenceValue = spRoot;
        so.ApplyModifiedProperties();

        MarkDirty(mmui);
        Debug.Log("[MenuSceneBuilder] Main menu built — save the scene (Ctrl+S).");
    }

    // ── Settings Panel (standalone, e.g. for Sprint3 pause menu) ─────────────

    [MenuItem("Tools/UI/Build Settings Panel")]
    static void BuildSettingsPanelMenu()
    {
        TMP_FontAsset font = LoadFont();
        Canvas canvas      = FindOrCreateOverlayCanvas("Canvas", 0);
        var canvasRect     = (RectTransform)canvas.transform;

        DestroyChild(canvasRect, "SettingsPanelRoot");

        var spRoot = BuildSettingsPanelHierarchy(canvasRect, font);

        // If a PauseMenuUI exists in the scene, wire the new panel to it
        var pmui = Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pmui)
        {
            var so = new SerializedObject(pmui);
            so.FindProperty("settingsPanelRoot").objectReferenceValue = spRoot;
            so.ApplyModifiedProperties();
            MarkDirty(pmui);
        }

        MarkDirty(spRoot.GetComponent<SettingsPanel>());
        Debug.Log("[MenuSceneBuilder] Settings panel built — save the scene (Ctrl+S).");
    }

    // ── Prefab export ─────────────────────────────────────────────────────────

    [MenuItem("Tools/UI/Create UI Prefabs")]
    static void CreateUIPrefabs()
    {
        TMP_FontAsset font = LoadFont();
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

        var btnGo  = BuildStandaloneButton("MenuButton", "Button", BtnW, BtnH, font);
        PrefabUtility.SaveAsPrefabAsset(btnGo, "Assets/Prefabs/UI/MenuButton.prefab");
        Object.DestroyImmediate(btnGo);

        var tempCanvas = new GameObject("_TempCanvas", typeof(Canvas));
        var spRoot     = BuildSettingsPanelHierarchy(
            (RectTransform)tempCanvas.transform, font);
        PrefabUtility.SaveAsPrefabAsset(spRoot, "Assets/Prefabs/UI/SettingsPanel.prefab");
        Object.DestroyImmediate(tempCanvas);

        AssetDatabase.Refresh();
        Debug.Log("[MenuSceneBuilder] Prefabs saved to Assets/Prefabs/UI/.");
    }

    // ── Settings panel hierarchy ──────────────────────────────────────────────
    // Uses ONE VerticalLayoutGroup — same structural approach as PauseMenuUI's
    // BuildPauseMenu(). No ScrollRect, no manually-anchored sub-zones.

    static GameObject BuildSettingsPanelHierarchy(RectTransform parent, TMP_FontAsset font)
    {
        // Root panel
        var spRoot  = Child(parent, "SettingsPanelRoot",
            typeof(SettingsPanel), typeof(CanvasGroup), typeof(Image), typeof(VerticalLayoutGroup));
        var spRect  = (RectTransform)spRoot.transform;
        Centre(spRect, new Vector2(SettingsPanelW, SettingsPanelH));

        spRoot.GetComponent<Image>().color = PanelBg;
        var grp = spRoot.GetComponent<CanvasGroup>();
        grp.alpha = 1f; grp.interactable = grp.blocksRaycasts = true;

        // Single VLG — matches the pause menu panel approach
        var vl = spRoot.GetComponent<VerticalLayoutGroup>();
        vl.padding            = new RectOffset(36, 36, 32, 32);
        vl.spacing            = 10f;
        vl.childAlignment     = TextAnchor.UpperCenter;
        vl.childControlWidth  = false;
        vl.childControlHeight = true;
        vl.childForceExpandWidth  = false;
        vl.childForceExpandHeight = false;

        var sp = spRoot.GetComponent<SettingsPanel>();

        // ── Title ──
        Label(spRect, "Title", "Settings", TitleSz, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Center, 50f, font);

        HairLine(spRect);

        // ── Audio ──
        SectionHeader(spRect, "Audio", font);
        var masterSlider = SliderRow(spRect, "MasterVolumeRow", "Master Volume", font);
        var musicSlider  = SliderRow(spRect, "MusicVolumeRow",  "Music Volume",  font);
        var sfxSlider    = SliderRow(spRect, "SfxVolumeRow",    "SFX Volume",    font);

        HairLine(spRect);

        // ── Display ──
        SectionHeader(spRect, "Display", font);
        var windowDropdown = DropdownRow(spRect, "WindowModeRow", "Window Mode", font);
        var vSyncToggle = ToggleRow(spRect, "VSyncRow", "VSync", font);

        HairLine(spRect);

        // ── Save data ──
        var saveStatus = StatusLabel(spRect, font);
        var saveRow    = RowContainer(spRect, "SaveActionsRow", 50f);
        var saveBtn    = DarkButton(saveRow, "SaveButton",       "Save",        128f, 44f, font);
        var loadBtn    = DarkButton(saveRow, "LoadButton",       "Load",        128f, 44f, font);
        var deleteBtn  = DarkButton(saveRow, "DeleteSaveButton", "Delete Save", 168f, 44f, font);

        FlexibleSpacer(spRect, "SettingsBottomSpacer");
        HairLine(spRect);

        // ── Back button — full-width to match the panel style ──
        var backBtn = DarkButton(spRect, "BackButton", "Back", SettingsInnerW, 52f, font);

        // Wire all [SerializeField] references on SettingsPanel
        var so = new SerializedObject(sp);
        so.FindProperty("masterSlider").objectReferenceValue       = masterSlider;
        so.FindProperty("musicSlider").objectReferenceValue        = musicSlider;
        so.FindProperty("sfxSlider").objectReferenceValue          = sfxSlider;
        so.FindProperty("windowModeDropdown").objectReferenceValue = windowDropdown;
        so.FindProperty("vSyncToggle").objectReferenceValue        = vSyncToggle;
        so.FindProperty("saveButton").objectReferenceValue         = saveBtn;
        so.FindProperty("loadButton").objectReferenceValue         = loadBtn;
        so.FindProperty("deleteSaveButton").objectReferenceValue   = deleteBtn;
        so.FindProperty("backButton").objectReferenceValue         = backBtn;
        so.FindProperty("saveStatusLabel").objectReferenceValue    = saveStatus;
        so.ApplyModifiedProperties();

        return spRoot;
    }

    // ── Atom helpers ──────────────────────────────────────────────────────────

    // Dark panel button — identical style to PauseMenuUI.CreateButton()
    static Button DarkButton(RectTransform parent, string goName, string label,
        float w, float h, TMP_FontAsset font)
    {
        var go  = Child(parent, goName, typeof(Image), typeof(Button), typeof(LayoutElement));
        var img = go.GetComponent<Image>();
        img.color  = BtnNormal;
        img.sprite = null;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = BtnHighlight;
        c.pressedColor     = BtnPressed;
        c.disabledColor    = BtnDisabled;
        btn.colors = c;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = w;
        le.preferredHeight = h;

        // Label inside the button
        var lGo  = Child((RectTransform)go.transform, "Label", typeof(TextMeshProUGUI));
        Stretch((RectTransform)lGo.transform);
        var tmp       = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = BtnFontSz;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.raycastTarget = false;
        if (font) tmp.font = font;
        return btn;
    }

    // Generic text label with LayoutElement height
    static void Label(RectTransform parent, string goName, string text,
        float size, Color color, FontStyles style, TextAlignmentOptions align,
        float height, TMP_FontAsset font)
    {
        var go  = Child(parent, goName, typeof(TextMeshProUGUI), typeof(LayoutElement));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        if (font) tmp.font = font;
        go.GetComponent<LayoutElement>().preferredHeight = height;
    }

    // Small bold section header (e.g. "Audio", "Display")
    static void SectionHeader(RectTransform parent, string text, TMP_FontAsset font)
    {
        var go  = Child(parent, text + "Header", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text.ToUpper();
        tmp.fontSize  = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = MutedLabel;
        tmp.alignment = TextAlignmentOptions.Left;
        if (font) tmp.font = font;
        // Give it some extra top spacing by using a taller LayoutElement
        go.GetComponent<LayoutElement>().preferredHeight = 22f;
    }

    // 1-px divider line
    static void HairLine(RectTransform parent)
    {
        var go = Child(parent, "Divider", typeof(Image), typeof(LayoutElement));
        go.GetComponent<Image>().color = SubDivider;
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = SettingsInnerW;
        le.preferredHeight = 1f;
    }

    // HLG row container for buttons/controls side-by-side
    static RectTransform RowContainer(RectTransform parent, string name, float height)
    {
        var go = Child(parent, name, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var hl = go.GetComponent<HorizontalLayoutGroup>();
        hl.spacing              = 8f;
        hl.childAlignment       = TextAnchor.MiddleCenter;
        hl.childControlWidth    = false;
        hl.childControlHeight   = false;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;
        go.GetComponent<LayoutElement>().preferredHeight = height;
        return (RectTransform)go.transform;
    }

    static void FlexibleSpacer(RectTransform parent, string name)
    {
        var go = Child(parent, name, typeof(LayoutElement));
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 0f;
        le.preferredHeight = 0f;
        le.flexibleHeight = 1f;
    }

    // Label on the left + slider on the right, both inside an HLG row
    static Slider SliderRow(RectTransform parent, string rowName, string labelText,
        TMP_FontAsset font)
    {
        var rowRect = RowContainer(parent, rowName, 40f);
        // Override HLG child sizing so the slider can flex
        var hl = rowRect.GetComponent<HorizontalLayoutGroup>();
        hl.childControlWidth  = true;
        hl.childForceExpandWidth = false;
        // Make the whole row 408px wide
        rowRect.GetComponent<LayoutElement>().preferredWidth = SettingsInnerW;

        // Left label — fixed width
        var lGo = Child(rowRect, "Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = labelText;
        tmp.fontSize  = 19f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color     = Color.white;
        if (font) tmp.font = font;
        var lLe = lGo.GetComponent<LayoutElement>();
        lLe.minWidth = 170f; lLe.preferredWidth = 170f; lLe.flexibleWidth = 0f;

        // Right slider — takes remaining space
        var slGo   = Child(rowRect, "Slider", typeof(Slider), typeof(LayoutElement));
        var slLe   = slGo.GetComponent<LayoutElement>();
        slLe.flexibleWidth   = 1f;
        slLe.preferredHeight = 40f;
        var slRect = (RectTransform)slGo.transform;

        // Track
        var bgGo   = Child(slRect, "Background", typeof(Image));
        var bgRect = (RectTransform)bgGo.transform;
        bgRect.anchorMin = new Vector2(0f, 0.35f);
        bgRect.anchorMax = new Vector2(1f, 0.65f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = SliderTrack;

        // Fill
        var fillArea     = MakeRT("Fill Area", slRect);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
        var fillGo = Child(fillArea, "Fill", typeof(Image));
        fillGo.GetComponent<Image>().color = SliderFill;

        // Handle
        var handleArea     = MakeRT("Handle Slide Area", slRect);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);
        var hGo   = Child(handleArea, "Handle", typeof(Image));
        var hRect = (RectTransform)hGo.transform;
        hRect.anchorMin = hRect.anchorMax = new Vector2(0f, 0.5f);
        hRect.pivot     = new Vector2(0.5f, 0.5f);
        hRect.sizeDelta = new Vector2(20f, 20f);
        hGo.GetComponent<Image>().color = Color.white;

        var slider = slGo.GetComponent<Slider>();
        slider.fillRect      = (RectTransform)fillGo.transform;
        slider.handleRect    = hRect;
        slider.targetGraphic = hGo.GetComponent<Image>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        return slider;
    }

    // Label + TMP_Dropdown row
    static TMP_Dropdown DropdownRow(RectTransform parent, string rowName,
        string labelText, TMP_FontAsset font)
    {
        var rowRect = RowContainer(parent, rowName, 40f);
        var hl = rowRect.GetComponent<HorizontalLayoutGroup>();
        hl.childControlWidth     = true;
        hl.childForceExpandWidth = false;
        rowRect.GetComponent<LayoutElement>().preferredWidth = SettingsInnerW;

        // Left label
        var lGo = Child(rowRect, "Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = labelText;
        tmp.fontSize  = 19f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color     = Color.white;
        if (font) tmp.font = font;
        var lLe = lGo.GetComponent<LayoutElement>();
        lLe.minWidth = 170f; lLe.preferredWidth = 170f; lLe.flexibleWidth = 0f;

        // Right dropdown
        var ddGo  = Child(rowRect, "Dropdown",
            typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
        var ddImg = ddGo.GetComponent<Image>();
        ddImg.color  = BtnNormal;
        ddImg.sprite = null;
        var dd  = ddGo.GetComponent<TMP_Dropdown>();
        dd.targetGraphic = ddImg;
        var dc = dd.colors;
        dc.highlightedColor = BtnHighlight;
        dc.pressedColor     = BtnPressed;
        dd.colors = dc;
        var ddLe = ddGo.GetComponent<LayoutElement>();
        ddLe.flexibleWidth = 1f; ddLe.preferredHeight = 38f;

        // Caption text
        var capGo  = Child((RectTransform)ddGo.transform, "Label", typeof(TextMeshProUGUI));
        Stretch((RectTransform)capGo.transform);
        var capTmp       = capGo.GetComponent<TextMeshProUGUI>();
        capTmp.fontSize  = 17f;
        capTmp.alignment = TextAlignmentOptions.MidlineLeft;
        capTmp.color     = Color.white;
        capTmp.margin    = new Vector4(8f, 0, 0, 0);
        capTmp.raycastTarget = false;
        if (font) capTmp.font = font;
        dd.captionText = capTmp;

        // Template (the dropdown list that opens on click)
        var tmplGo   = Child((RectTransform)ddGo.transform, "Template",
            typeof(Image), typeof(ScrollRect));
        var tmplRect = (RectTransform)tmplGo.transform;
        tmplRect.anchorMin        = new Vector2(0f, 0f);
        tmplRect.anchorMax        = new Vector2(1f, 0f);
        tmplRect.pivot            = new Vector2(0.5f, 1f);
        tmplRect.anchoredPosition = new Vector2(0f, -2f);
        tmplRect.sizeDelta        = new Vector2(0f, 120f);
        tmplGo.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.14f, 0.98f);
        tmplGo.SetActive(false);

        var vpGo = Child(tmplRect, "Viewport", typeof(RectMask2D));
        Stretch((RectTransform)vpGo.transform);

        var listGo = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listGo.transform.SetParent(vpGo.transform, false);
        var listRect = (RectTransform)listGo.transform;
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot     = new Vector2(0.5f, 1f);
        listRect.offsetMin = listRect.offsetMax = Vector2.zero;
        listGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listGo.GetComponent<VerticalLayoutGroup>().childControlWidth = true;

        var itemGo = new GameObject("Item",
            typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        itemGo.transform.SetParent(listRect, false);
        itemGo.GetComponent<LayoutElement>().preferredHeight = 34f;
        var itemRect = (RectTransform)itemGo.transform;
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 34f);

        var itemBgGo  = Child(itemRect, "Item Background", typeof(Image));
        Stretch((RectTransform)itemBgGo.transform);
        var itemBgImg = itemBgGo.GetComponent<Image>();
        itemBgImg.color = BtnNormal;

        var ckGo   = Child(itemRect, "Item Checkmark", typeof(Image));
        var ckRect = (RectTransform)ckGo.transform;
        ckRect.anchorMin        = ckRect.anchorMax = new Vector2(0f, 0.5f);
        ckRect.pivot            = new Vector2(0.5f, 0.5f);
        ckRect.anchoredPosition = new Vector2(14f, 0f);
        ckRect.sizeDelta        = new Vector2(10f, 10f);
        ckGo.GetComponent<Image>().color = SliderFill;

        var itemLGo = Child(itemRect, "Item Label", typeof(TextMeshProUGUI));
        Stretch((RectTransform)itemLGo.transform);
        var itemTmp         = itemLGo.GetComponent<TextMeshProUGUI>();
        itemTmp.fontSize    = 16f;
        itemTmp.alignment   = TextAlignmentOptions.MidlineLeft;
        itemTmp.color       = Color.white;
        itemTmp.margin      = new Vector4(26f, 0, 0, 0);
        itemTmp.raycastTarget = false;
        if (font) itemTmp.font = font;

        var tog = itemGo.GetComponent<Toggle>();
        tog.targetGraphic = itemBgImg;
        tog.graphic       = ckGo.GetComponent<Image>();

        var sc = tmplGo.GetComponent<ScrollRect>();
        sc.viewport  = (RectTransform)vpGo.transform;
        sc.content   = listRect;
        sc.horizontal = false; sc.vertical = true;

        dd.template = tmplRect;
        dd.itemText = itemTmp;
        dd.ClearOptions();
        dd.AddOptions(new List<string> { "Windowed", "Fullscreen", "Borderless" });
        return dd;
    }

    // Label + toggle row
    static Toggle ToggleRow(RectTransform parent, string rowName,
        string labelText, TMP_FontAsset font)
    {
        var rowRect = RowContainer(parent, rowName, 30f);
        var hl = rowRect.GetComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true;
        hl.childForceExpandWidth = false;
        rowRect.GetComponent<LayoutElement>().preferredWidth = SettingsInnerW;

        var lGo = Child(rowRect, "Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 19f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        if (font) tmp.font = font;
        var lLe = lGo.GetComponent<LayoutElement>();
        lLe.minWidth = 170f;
        lLe.preferredWidth = 170f;
        lLe.flexibleWidth = 0f;

        var toggleGo = Child(rowRect, "VSyncToggle", typeof(Image), typeof(Toggle), typeof(LayoutElement));
        var toggleLe = toggleGo.GetComponent<LayoutElement>();
        toggleLe.preferredWidth = 28f;
        toggleLe.preferredHeight = 28f;
        toggleLe.flexibleWidth = 0f;

        var background = toggleGo.GetComponent<Image>();
        background.color = BtnNormal;

        var checkmarkGo = Child((RectTransform)toggleGo.transform, "Checkmark", typeof(Image));
        var checkmarkRect = (RectTransform)checkmarkGo.transform;
        checkmarkRect.anchorMin = new Vector2(0.22f, 0.22f);
        checkmarkRect.anchorMax = new Vector2(0.78f, 0.78f);
        checkmarkRect.offsetMin = checkmarkRect.offsetMax = Vector2.zero;
        var checkmark = checkmarkGo.GetComponent<Image>();
        checkmark.color = SliderFill;

        var toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = true;
        return toggle;
    }

    // Small italic status text (e.g. "Save data found")
    static TextMeshProUGUI StatusLabel(RectTransform parent, TMP_FontAsset font)
    {
        var go  = Child(parent, "SaveStatus", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = MutedLabel;
        tmp.fontStyle = FontStyles.Italic;
        if (font) tmp.font = font;
        go.GetComponent<LayoutElement>().preferredHeight = 22f;
        return tmp;
    }

    // ── Low-level helpers ─────────────────────────────────────────────────────

    // Standalone button for prefab creation (not parented to a layout group)
    static GameObject BuildStandaloneButton(string goName, string label,
        float w, float h, TMP_FontAsset font)
    {
        var go  = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
        var img = go.GetComponent<Image>();
        img.color = BtnNormal; img.sprite = null;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = BtnHighlight; c.pressedColor = BtnPressed;
        c.disabledColor    = BtnDisabled;
        btn.colors = c;
        ((RectTransform)go.transform).sizeDelta = new Vector2(w, h);

        var lGo  = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        Stretch((RectTransform)lGo.transform);
        var tmp       = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = BtnFontSz;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        if (font) tmp.font = font;
        return go;
    }

    static Canvas FindOrCreateOverlayCanvas(string goName, int sortOrder)
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;

        var go = new GameObject(goName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        return canvas;
    }

    static GameObject Child(RectTransform parent, string name, params System.Type[] components)
    {
        var go = new GameObject(name, components);
        go.transform.SetParent(parent, false);
        if (!go.GetComponent<RectTransform>()) go.AddComponent<RectTransform>();
        return go;
    }

    // RectTransform-only child (no extra components)
    static RectTransform MakeRT(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void DestroyChild(Transform parent, string childName)
    {
        var t = parent.Find(childName);
        if (t) Object.DestroyImmediate(t.gameObject);
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax  = Vector2.zero;
    }

    static void Centre(RectTransform r, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = size;
    }

    static TMP_FontAsset LoadFont()
    {
        string path = AssetDatabase.GUIDToAssetPath(FontGuid);
        return string.IsNullOrEmpty(path)
            ? null
            : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }

    static void MarkDirty(Object obj)
    {
        if (!obj) return;
        EditorUtility.SetDirty(obj);
        if (obj is Component comp)
            EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
    }
}
