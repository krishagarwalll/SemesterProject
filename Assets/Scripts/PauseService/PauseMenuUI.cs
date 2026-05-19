using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        HandlePauseChanged(PauseService.ActivePauseTypes);
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
        SetInventoryVisible(!paused);

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
