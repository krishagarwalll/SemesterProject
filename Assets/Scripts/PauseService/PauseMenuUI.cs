using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private Button resumeButton;

    private SettingsPanel settingsPanel;

    private void Awake()
    {
        EnsureMenuExists();
        if (pauseMenu) pauseMenu.SetActive(false);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        PauseService.PauseChanged += HandlePauseChanged;
        AllowPausedUiInput();
        WireSettingsPanel();
        WireSettingsButton();
        WireResumeButton();
        WireSaveButtons();
        WireMainMenuButton();
        WireQuitButton();
    }

    private void OnDisable()
    {
        PauseService.PauseChanged -= HandlePauseChanged;
    }

    public void ShowSettings()
    {
        if (pauseMenu) pauseMenu.SetActive(false);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(true);
    }

    public void ShowMain()
    {
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        if (pauseMenu) pauseMenu.SetActive(true);
    }

    private void WireSettingsPanel()
    {
        if (!settingsPanelRoot) return;
        settingsPanel = settingsPanelRoot.GetComponentInChildren<SettingsPanel>(true);
        if (!settingsPanel) return;
        settingsPanel.BackRequested -= ShowMain;
        settingsPanel.BackRequested += ShowMain;
    }

    private void WireSettingsButton()
    {
        Button button = FindButton("Options", "OptionsButton", "Settings", "SettingsButton");
        if (!button) return;
        button.onClick.RemoveListener(ShowSettings);
        button.onClick.AddListener(ShowSettings);
    }

    private void WireResumeButton()
    {
        Button button = resumeButton ? resumeButton : FindButton("Resume", "ResumeButton", "Continue", "ContinueButton");
        if (!button) return;
        button.onClick.RemoveListener(OnResumeClicked);
        button.onClick.AddListener(OnResumeClicked);
    }

    private void WireSaveButtons()
    {
        WireSaveButton("Save", OnSaveClicked);
        WireSaveButton("SaveButton", OnSaveClicked);
        WireSaveButton("Load", OnLoadClicked, requiresSave: true);
        WireSaveButton("LoadButton", OnLoadClicked, requiresSave: true);
        WireSaveButton("DeleteSave", OnDeleteSaveClicked, requiresSave: true);
        WireSaveButton("DeleteSaveButton", OnDeleteSaveClicked, requiresSave: true);
    }

    private void WireSaveButton(string name, UnityEngine.Events.UnityAction action, bool requiresSave = false)
    {
        Button button = FindButton(name);
        if (!button) return;
        button.interactable = !requiresSave || SaveManager.Instance && SaveManager.Instance.HasSave();
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void WireMainMenuButton()
    {
        Button button = FindButton("Main Menu", "Main MenuButton", "MainMenu", "MainMenuButton", "Menu", "MenuButton");
        if (!button) return;
        button.onClick.RemoveListener(OnMainMenuClicked);
        button.onClick.AddListener(OnMainMenuClicked);
    }

    private void WireQuitButton()
    {
        Button button = FindButton("Quit", "QuitButton", "Exit", "ExitButton");
        if (!button) return;
        button.onClick.RemoveListener(OnQuitClicked);
        button.onClick.AddListener(OnQuitClicked);
    }

    private Button FindButton(params string[] names)
    {
        if (!pauseMenu) return null;
        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsNamed(buttons[i], names))
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void OnResumeClicked() => PauseService.Resume();

    private void OnSaveClicked()
    {
        SaveManager.Instance?.Save();
        WireSaveButtons();
    }

    private void OnLoadClicked() => SaveManager.Instance?.LoadAndApply();

    private void OnDeleteSaveClicked()
    {
        SaveManager.Instance?.DeleteSave();
        WireSaveButtons();
    }

    private void OnMainMenuClicked()
    {
        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnQuitClicked() => RuntimeUiUtility.QuitApplication();

    private void AllowPausedUiInput()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (!eventSystem) continue;
            PauseService.SetPauseBypass(eventSystem, PauseType.Input | PauseType.UI, true);
            PauseService.SetPauseBypass(eventSystem.gameObject, PauseType.Input | PauseType.UI, true);
        }
    }

    private static bool IsNamed(Component component, params string[] names)
    {
        if (!component) return false;
        for (int i = 0; i < names.Length; i++)
        {
            if (component.name == names[i])
            {
                return true;
            }
        }

        return false;
    }

    private void HandlePauseChanged(PauseType pauseTypes)
    {
        bool paused = (pauseTypes & (PauseType.Animation | PauseType.Particles | PauseType.Audio)) != 0;
        if (!paused)
        {
            if (pauseMenu) pauseMenu.SetActive(false);
            if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
            return;
        }

        WireSaveButtons();
        AllowPausedUiInput();
        if (settingsPanelRoot && settingsPanelRoot.activeSelf) return;
        if (pauseMenu) pauseMenu.SetActive(true);
    }

    private void EnsureMenuExists()
    {
        RectTransform root = GetOrCreateCanvasRoot();
        if (!root)
        {
            return;
        }

        if (!pauseMenu)
        {
            pauseMenu = BuildPauseMenu(root);
        }

        if (!settingsPanelRoot)
        {
            settingsPanelRoot = BuildSettingsPanel(root);
        }
    }

    private RectTransform GetOrCreateCanvasRoot()
    {
        Canvas existingCanvas = GetComponentInParent<Canvas>(true);
        if (existingCanvas)
        {
            GraphicRaycaster raycaster = existingCanvas.GetComponent<GraphicRaycaster>() ?? existingCanvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
            return existingCanvas.transform as RectTransform;
        }

        GameObject canvasObject = new("PauseMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private GameObject BuildPauseMenu(RectTransform parent)
    {
        GameObject overlay = new("PauseMenu", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.55f);

        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;

        GameObject panel = new("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(overlayRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 500f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.96f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 36, 36);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateLabel(panelRect, "Paused", 42f, 72f);
        resumeButton = CreateButton(panelRect, "Resume", new Vector2(260f, 58f));
        CreateButton(panelRect, "Options", new Vector2(260f, 58f));
        CreateButton(panelRect, "Main Menu", new Vector2(260f, 58f));
        CreateButton(panelRect, "Quit", new Vector2(260f, 58f));
        return overlay;
    }

    private GameObject BuildSettingsPanel(RectTransform parent)
    {
        GameObject panel = new("SettingsPanel", typeof(RectTransform), typeof(SettingsPanel));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 720f);
        return panel;
    }

    private static void CreateLabel(RectTransform parent, string text, float fontSize, float height)
    {
        GameObject labelObject = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 300f;
        layout.preferredHeight = height;
    }

    private static Button CreateButton(RectTransform parent, string label, Vector2 size)
    {
        GameObject buttonObject = new(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.34f, 0.38f, 1f);
        colors.pressedColor = new Color(0.12f, 0.15f, 0.18f, 1f);
        button.colors = colors;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        GameObject textObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
        textMesh.text = label;
        textMesh.fontSize = 26f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.raycastTarget = false;
        return button;
    }
}
