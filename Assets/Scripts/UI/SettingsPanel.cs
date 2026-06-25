using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanel : MonoBehaviour
{
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
        AutoDiscover();
        var group = GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = group.blocksRaycasts = true;
    }

    private void AutoDiscover()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        if (!masterSlider && sliders.Length > 0) masterSlider = sliders[0];
        if (!musicSlider  && sliders.Length > 1) musicSlider  = sliders[1];
        if (!sfxSlider    && sliders.Length > 2) sfxSlider    = sliders[2];

        if (!windowModeDropdown) windowModeDropdown = GetComponentInChildren<TMP_Dropdown>(true);

        foreach (Button btn in GetComponentsInChildren<Button>(true))
        {
            string n = btn.name.ToLowerInvariant();
            if (!saveButton       && n.Contains("save") && !n.Contains("delete") && !n.Contains("load")) saveButton = btn;
            if (!loadButton       && n.Contains("load"))   loadButton       = btn;
            if (!deleteSaveButton && n.Contains("delete")) deleteSaveButton = btn;
            if (!backButton       && (n.Contains("back") || n.Contains("return") || n.Contains("close"))) backButton = btn;
        }

        if (!saveStatusLabel)
        {
            Transform t = transform.Find("SaveStatus");
            if (t) saveStatusLabel = t.GetComponent<TextMeshProUGUI>();
        }

        if (!saveStatusLabel)
        {
            foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.GetComponentInParent<Button>()) continue;
                string n = tmp.name.ToLowerInvariant();
                if (n.Contains("status") || n.Contains("save"))
                {
                    saveStatusLabel = tmp;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        RefreshValues();
        WireAll();
    }

    private void OnDisable()
    {
        UnwireAll();
    }

    private void RefreshValues()
    {
        if (AudioManager.Instance)
        {
            if (masterSlider) masterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            if (musicSlider)  musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxSlider)    sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        if (windowModeDropdown)
        {
            windowModeDropdown.SetValueWithoutNotify(CurrentWindowModeIndex());
            windowModeDropdown.RefreshShownValue();
        }

        RefreshSaveStatus();
    }

    private void WireAll()
    {
        if (masterSlider)       { masterSlider.onValueChanged.RemoveListener(OnMaster);      masterSlider.onValueChanged.AddListener(OnMaster); }
        if (musicSlider)        { musicSlider.onValueChanged.RemoveListener(OnMusic);        musicSlider.onValueChanged.AddListener(OnMusic); }
        if (sfxSlider)          { sfxSlider.onValueChanged.RemoveListener(OnSfx);            sfxSlider.onValueChanged.AddListener(OnSfx); }
        if (windowModeDropdown) { windowModeDropdown.onValueChanged.RemoveListener(OnWindowMode); windowModeDropdown.onValueChanged.AddListener(OnWindowMode); }
        if (saveButton)         { saveButton.onClick.RemoveListener(OnSave);                 saveButton.onClick.AddListener(OnSave); }
        if (loadButton)         { loadButton.onClick.RemoveListener(OnLoad);                 loadButton.onClick.AddListener(OnLoad); }
        if (deleteSaveButton)   { deleteSaveButton.onClick.RemoveListener(OnDeleteSave);     deleteSaveButton.onClick.AddListener(OnDeleteSave); }
        if (backButton)         { backButton.onClick.RemoveListener(OnBack);                 backButton.onClick.AddListener(OnBack); }
    }

    private void UnwireAll()
    {
        if (masterSlider)       masterSlider.onValueChanged.RemoveListener(OnMaster);
        if (musicSlider)        musicSlider.onValueChanged.RemoveListener(OnMusic);
        if (sfxSlider)          sfxSlider.onValueChanged.RemoveListener(OnSfx);
        if (windowModeDropdown) windowModeDropdown.onValueChanged.RemoveListener(OnWindowMode);
        if (saveButton)         saveButton.onClick.RemoveListener(OnSave);
        if (loadButton)         loadButton.onClick.RemoveListener(OnLoad);
        if (deleteSaveButton)   deleteSaveButton.onClick.RemoveListener(OnDeleteSave);
        if (backButton)         backButton.onClick.RemoveListener(OnBack);
    }

    private void OnMaster(float v)  => AudioManager.Instance?.SetMasterVolume(v);
    private void OnMusic(float v)   => AudioManager.Instance?.SetMusicVolume(v);
    private void OnSfx(float v)     => AudioManager.Instance?.SetSfxVolume(v);
    private void OnBack()           => BackRequested?.Invoke();

    private void OnSave()       { SaveManager.Instance?.Save();         RefreshSaveStatus(); }
    private void OnLoad()       { SaveManager.Instance?.LoadAndApply(); }
    private void OnDeleteSave() { SaveManager.Instance?.DeleteSave();   RefreshSaveStatus(); }

    private void RefreshSaveStatus()
    {
        bool has = SaveManager.Instance && SaveManager.Instance.HasSave();
        if (loadButton)       loadButton.interactable       = has;
        if (deleteSaveButton) deleteSaveButton.interactable = has;
        if (saveStatusLabel)  saveStatusLabel.text          = has ? "Save data found" : "No save data";
    }

    private static void OnWindowMode(int index)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLDisplay.SetFullscreen(index != 0);
#else
        FullScreenMode mode = index switch
        {
            1 => FullScreenMode.ExclusiveFullScreen,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };
        Screen.fullScreenMode = mode;
        Screen.fullScreen = mode != FullScreenMode.Windowed;
#endif
        PlayerPrefs.SetInt("Settings_WindowMode", index);
        PlayerPrefs.Save();
    }

    private static int CurrentWindowModeIndex()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return WebGLDisplay.IsFullscreen ? 2 : 0;
#else
        return Screen.fullScreenMode switch
        {
            FullScreenMode.Windowed            => 0,
            FullScreenMode.ExclusiveFullScreen => 1,
            FullScreenMode.FullScreenWindow    => 2,
            _ => Screen.fullScreen ? 2 : 0
        };
#endif
    }
}
