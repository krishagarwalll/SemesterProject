using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuUI : MonoBehaviour
{
    private const string GameSceneName = "Sprint3";

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanelRoot;

    private SettingsPanel settingsPanel;

    private void Awake()
    {
        RuntimeUiUtility.ResetRuntimeState();
        RuntimeUiUtility.EnsureCoreSystems();
        RuntimeUiUtility.ApplyDisplaySettings();
        RuntimeUiUtility.EnsureEventSystem();
        RuntimeUiUtility.EnsureCanvasRaycasters();

        AutoDiscoverPanels();
        WebGLPauseQuickActions.RemoveFrom(menuPanel);

        if (menuPanel) menuPanel.SetActive(true);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
    }

    private void AutoDiscoverPanels()
    {
        if (!settingsPanelRoot)
        {
            SettingsPanel sp = FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (sp && sp.gameObject != gameObject)
                settingsPanelRoot = sp.gameObject;
        }

        if (!menuPanel)
        {
            Canvas canvas = GetComponentInParent<Canvas>(true);
            Transform root = canvas ? canvas.transform : transform;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.gameObject == gameObject) continue;
                if (settingsPanelRoot && child.gameObject == settingsPanelRoot) continue;
                if (child.GetComponentInChildren<Button>(true))
                {
                    menuPanel = child.gameObject;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        WireSettingsPanel();
        WireButtons();
    }

    private void OnDisable()
    {
        if (settingsPanel)
            settingsPanel.BackRequested -= ShowMain;
    }

    public void ShowMain()
    {
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        if (menuPanel) menuPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        if (menuPanel) menuPanel.SetActive(false);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(true);
    }

    private void WireSettingsPanel()
    {
        if (!settingsPanelRoot) return;
        settingsPanel = settingsPanelRoot.GetComponentInChildren<SettingsPanel>(true);
        if (!settingsPanel) return;
        settingsPanel.BackRequested -= ShowMain;
        settingsPanel.BackRequested += ShowMain;
    }

    private void WireButtons()
    {
        bool hasSave = SaveManager.Instance && SaveManager.Instance.HasSave();

        // Continue — only when save data exists
        Button continueBtn = FindButton("Continue", "ContinueButton");
        if (continueBtn)
        {
            continueBtn.gameObject.SetActive(hasSave);
            continueBtn.onClick.RemoveListener(OnContinueClicked);
            continueBtn.onClick.AddListener(OnContinueClicked);
        }

        // Start always begins a fresh game.
        Button startBtn = FindButton("Start", "StartButton");
        if (startBtn)
        {
            startBtn.gameObject.SetActive(true);
            startBtn.onClick.RemoveListener(OnStartClicked);
            startBtn.onClick.AddListener(OnStartClicked);
        }

        // Settings
        Button settingsBtn = FindButton("Settings", "SettingsButton", "Options", "OptionsButton");
        if (settingsBtn)
        {
            settingsBtn.onClick.RemoveListener(ShowSettings);
            settingsBtn.onClick.AddListener(ShowSettings);
        }

        // Exit
        Button exitBtn = FindButton("Exit", "ExitButton", "Quit", "QuitButton");
        if (exitBtn)
        {
            exitBtn.gameObject.SetActive(RuntimeUiUtility.CanQuitApplication);
            if (!RuntimeUiUtility.CanQuitApplication)
            {
                return;
            }

            exitBtn.onClick.RemoveListener(RuntimeUiUtility.QuitApplication);
            exitBtn.onClick.AddListener(RuntimeUiUtility.QuitApplication);
        }
    }

    private void OnContinueClicked()
    {
        PauseService.ClearAll();
        SaveManager.Instance?.LoadAndApply();
    }

    private void OnStartClicked()
    {
        PauseService.ClearAll();
        SaveManager.Instance?.DeleteSave();
        SceneManager.LoadScene(GameSceneName);
    }

    private Button FindButton(params string[] names)
    {
        if (!menuPanel) return null;
        Button[] buttons = menuPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            for (int j = 0; j < names.Length; j++)
            {
                if (buttons[i].name == names[j]) return buttons[i];
            }
        }
        return null;
    }
}
