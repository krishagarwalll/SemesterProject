using UnityEngine;
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
        WireSettingsPanel();
        WireSettingsButton();
        WireResumeButton();
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
            if (btn.name == "Settings" || btn.name == "SettingsButton")
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
            if (btn.name == "Resume" || btn.name == "ResumeButton")
            {
                btn.onClick.RemoveListener(OnResumeClicked);
                btn.onClick.AddListener(OnResumeClicked);
                break;
            }
        }
    }

    private void OnResumeClicked() => PauseService.Toggle();

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
