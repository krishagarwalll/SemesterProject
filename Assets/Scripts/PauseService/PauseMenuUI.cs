using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseButton;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    private SettingsPanel settingsPanel;
    private CanvasGroup pauseMenuGroup;
    private bool waitingForPauseButtonRelease;
    private bool pauseMenuFadeTarget;

    private void Awake()
    {
        EnsureMenuExists();
        EnsurePauseButton();
        EnsurePauseMenuCanvasGroup();
        WebGLPauseQuickActions.EnsureCreated(pauseMenu);
        if (pauseMenu) pauseMenu.SetActive(false);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        SetPauseButtonVisible(true);
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
        HandlePauseChanged(PauseService.ActivePauseTypes);
    }

    private void OnDisable()
    {
        PauseService.PauseChanged -= HandlePauseChanged;
    }

    private void Update()
    {
        UpdatePauseMenuFade();

        bool paused = PauseService.IsPaused(PauseType.Physics);
        if (paused)
        {
            waitingForPauseButtonRelease = false;
            SetPauseButtonVisible(false);
            return;
        }

        if (waitingForPauseButtonRelease)
        {
            SetPauseButtonVisible(false);
            if (IsPrimaryPointerPressed()) return;

            waitingForPauseButtonRelease = false;
            if (EventSystem.current)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (pauseButton) pauseButton.interactable = true;
        }

        SetPauseButtonVisible(true);
    }

    private void OnPauseClicked()
    {
        if (PauseInputHandler.IsCutscenePlaying()) return;
        PauseService.Pause();
    }

    public void ShowSettings()
    {
        HidePauseMenuImmediately();
        if (settingsPanelRoot) settingsPanelRoot.SetActive(true);
    }

    public void ShowMain()
    {
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        ShowPauseMenuImmediately();
    }

    public void RefreshRuntimeBindings()
    {
        AutoDiscoverPanels();
        EnsurePauseMenuCanvasGroup();
        WireSettingsPanel();
        WireSettingsButton();
        WireResumeButton();
        WireSaveButtons();
        WireMainMenuButton();
        WireQuitButton();
        HandlePauseChanged(PauseService.ActivePauseTypes);
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
        button.gameObject.SetActive(RuntimeUiUtility.CanQuitApplication);
        if (!RuntimeUiUtility.CanQuitApplication) return;
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

    private void OnResumeClicked()
    {
        BeginPauseButtonReactivation();
        PauseService.Resume();
    }

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
        bool paused = (pauseTypes & PauseType.Physics) != 0;
        SetInventoryVisible(!paused);

        if (!paused)
        {
            SetPauseMenuVisible(false);
            if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
            BeginPauseButtonReactivation();
            return;
        }

        SetPauseButtonVisible(false);
        WireSaveButtons();
        AllowPausedUiInput();
        if (settingsPanelRoot && settingsPanelRoot.activeSelf) return;
        SetPauseMenuVisible(true);
    }

    private static void SetInventoryVisible(bool visible)
    {
        InventoryHotbar hotbar = FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
        if (hotbar) hotbar.gameObject.SetActive(visible);
    }

    private void EnsureMenuExists()
    {
        AutoDiscoverPanels();

        if (!pauseMenu)
        {
            Debug.LogWarning($"{nameof(PauseMenuUI)} on {name} is missing a pause menu reference.", this);
        }

        if (!settingsPanelRoot)
        {
            Debug.LogWarning($"{nameof(PauseMenuUI)} on {name} is missing a settings panel reference.", this);
        }
    }

    private void EnsurePauseButton()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (!canvas) return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 5000);

        if (!pauseButton)
        {
            Transform existing = canvas.transform.Find("PauseButton");
            if (existing) pauseButton = existing.GetComponent<Button>();
        }

        if (!pauseButton)
        {
            GameObject buttonObject = new(
                "PauseButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = LayerMask.NameToLayer("UI");
            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -28f);
            rect.sizeDelta = new Vector2(126f, 54f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.055f, 0.047f, 0.07f, 0.96f);

            pauseButton = buttonObject.GetComponent<Button>();
            ColorBlock colors = pauseButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            pauseButton.colors = colors;

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.layer = buttonObject.layer;
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "PAUSE";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 21f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            label.raycastTarget = false;
        }

        pauseButton.onClick.RemoveListener(OnPauseClicked);
        pauseButton.onClick.AddListener(OnPauseClicked);
    }

    private void EnsurePauseMenuCanvasGroup()
    {
        if (!pauseMenu) return;
        pauseMenuGroup = pauseMenu.GetComponent<CanvasGroup>();
        if (!pauseMenuGroup)
        {
            pauseMenuGroup = pauseMenu.AddComponent<CanvasGroup>();
        }

        pauseMenuGroup.alpha = 0f;
        pauseMenuGroup.interactable = false;
        pauseMenuGroup.blocksRaycasts = false;
    }

    private void SetPauseMenuVisible(bool visible)
    {
        if (!pauseMenu || !pauseMenuGroup) return;

        pauseMenuFadeTarget = visible;
        if (visible)
        {
            pauseMenu.SetActive(true);
            pauseMenuGroup.interactable = true;
            pauseMenuGroup.blocksRaycasts = true;
        }
        else
        {
            pauseMenuGroup.interactable = false;
            pauseMenuGroup.blocksRaycasts = false;
        }
    }

    private void UpdatePauseMenuFade()
    {
        if (!pauseMenu || !pauseMenuGroup || !pauseMenu.activeSelf) return;

        float target = pauseMenuFadeTarget ? 1f : 0f;
        pauseMenuGroup.alpha = Mathf.MoveTowards(
            pauseMenuGroup.alpha,
            target,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));

        if (!pauseMenuFadeTarget && pauseMenuGroup.alpha <= 0f)
        {
            pauseMenu.SetActive(false);
        }
    }

    private void HidePauseMenuImmediately()
    {
        if (!pauseMenu || !pauseMenuGroup) return;
        pauseMenuFadeTarget = false;
        pauseMenuGroup.alpha = 0f;
        pauseMenuGroup.interactable = false;
        pauseMenuGroup.blocksRaycasts = false;
        pauseMenu.SetActive(false);
    }

    private void ShowPauseMenuImmediately()
    {
        if (!pauseMenu || !pauseMenuGroup) return;
        pauseMenuFadeTarget = true;
        pauseMenu.SetActive(true);
        pauseMenuGroup.alpha = 1f;
        pauseMenuGroup.interactable = true;
        pauseMenuGroup.blocksRaycasts = true;
    }

    private void SetPauseButtonVisible(bool visible)
    {
        if (!pauseButton) return;
        pauseButton.gameObject.SetActive(visible && !PauseInputHandler.IsCutscenePlaying());
    }

    private void BeginPauseButtonReactivation()
    {
        waitingForPauseButtonRelease = true;
        SetPauseButtonVisible(false);
    }

    private static bool IsPrimaryPointerPressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
    }

    private void AutoDiscoverPanels()
    {
        if (!pauseMenu)
        {
            Transform pauseTransform = FindDescendant(transform, "PauseMenu");
            if (pauseTransform)
            {
                pauseMenu = pauseTransform.gameObject;
            }
        }

        if (!settingsPanelRoot)
        {
            SettingsPanel sp = FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (sp && sp.gameObject != gameObject)
            {
                settingsPanelRoot = sp.gameObject;
            }
        }
    }

    private static Transform FindDescendant(Transform root, string childName)
    {
        if (!root) return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] && children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }
}
