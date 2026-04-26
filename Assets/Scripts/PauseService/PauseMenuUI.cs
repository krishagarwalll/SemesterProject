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
        WireMainMenuButton();
        WireQuitButton();
    }

    private void OnDisable()
    {
        PauseService.PauseChanged -= HandlePauseChanged;
    }

    // ── Navigation ───────────────────────────────────────────────

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

    // ── Wiring ───────────────────────────────────────────────────

    private void WireSettingsPanel()
    {
        if (!settingsPanelRoot) return;
        settingsPanel = settingsPanelRoot.GetComponentInChildren<SettingsPanel>(true);
        if (settingsPanel)
        {
            settingsPanel.BackRequested -= ShowMain;
            settingsPanel.BackRequested += ShowMain;
        }
    }

    private void WireSettingsButton()
    {
        if (!pauseMenu) return;
        // Find a Button in the pause menu hierarchy whose GameObject is named "Settings"
        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (IsNamed(btn, "Settings", "SettingsButton", "Options", "OptionsButton"))
            {
                btn.onClick.RemoveListener(ShowSettings);
                btn.onClick.AddListener(ShowSettings);
                break;
            }
        }
    }

    private void WireResumeButton()
    {
        if (resumeButton)
        {
            resumeButton.onClick.RemoveListener(OnResumeClicked);
            resumeButton.onClick.AddListener(OnResumeClicked);
            return;
        }

        if (!pauseMenu) return;
        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (IsNamed(btn, "Resume", "ResumeButton", "Continue", "ContinueButton"))
            {
                btn.onClick.RemoveListener(OnResumeClicked);
                btn.onClick.AddListener(OnResumeClicked);
                break;
            }
        }
    }

    private void OnResumeClicked() => PauseService.Toggle();

    private void WireMainMenuButton()
    {
        if (!pauseMenu) return;
        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (IsNamed(btn, "MainMenu", "MainMenuButton", "Menu", "MenuButton"))
            {
                btn.onClick.RemoveListener(OnMainMenuClicked);
                btn.onClick.AddListener(OnMainMenuClicked);
                break;
            }
        }
    }

    private void WireQuitButton()
    {
        if (!pauseMenu) return;
        Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (IsNamed(btn, "Quit", "QuitButton", "Exit", "ExitButton"))
            {
                btn.onClick.RemoveListener(OnQuitClicked);
                btn.onClick.AddListener(OnQuitClicked);
                break;
            }
        }
    }

    private void OnMainMenuClicked()
    {
        PauseService.ClearAll();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }

    private void AllowPausedUiInput()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem)
        {
            PauseService.SetPauseBypass(eventSystem, PauseType.UI, true);
        }
    }

    private static bool IsNamed(Component component, params string[] names)
    {
        if (!component)
        {
            return false;
        }

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
        if (!paused)
        {
            // Hide all panels when unpausing
            if (pauseMenu) pauseMenu.SetActive(false);
            if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        }
        else
        {
            // Show main pause panel (settings may already be open — leave it)
            if (settingsPanelRoot && settingsPanelRoot.activeSelf) return;
            if (pauseMenu) pauseMenu.SetActive(true);
        }
    }
}
