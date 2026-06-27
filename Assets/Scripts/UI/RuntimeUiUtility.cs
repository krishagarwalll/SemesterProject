using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public static class RuntimeUiUtility
{
    private const string PrefWindowMode = "Settings_WindowMode";
    private const string PrefVSync = "Settings_VSync";
    private const string InventoryPrefabResourcePath = "UI/GameplayInventoryCanvas";
    private const string PausePrefabResourcePath = "UI/GameplayPauseCanvas";

    public static bool CanQuitApplication
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }

    public static void ResetRuntimeState()
    {
        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public static void EnsureCoreSystems()
    {
        ApplyDisplaySettings();

        if (!AudioManager.Instance)
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        if (!SaveManager.Instance)
        {
            new GameObject("SaveManager").AddComponent<SaveManager>();
        }
    }

    public static void ApplyDisplaySettings()
    {
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(PrefVSync, 1) != 0 ? 1 : 0;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        bool hasWindowModePreference = PlayerPrefs.HasKey(PrefWindowMode);
        int windowMode = PlayerPrefs.GetInt(PrefWindowMode, 0);
        if (windowMode == 2)
        {
            windowMode = 0;
            PlayerPrefs.SetInt(PrefWindowMode, windowMode);
            PlayerPrefs.Save();
        }

        FullScreenMode mode = windowMode == 1
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.Windowed;
        Screen.fullScreenMode = mode;
        Screen.fullScreen = mode != FullScreenMode.Windowed;

        if (!hasWindowModePreference)
        {
            PlayerPrefs.SetInt(PrefWindowMode, 0);
            PlayerPrefs.Save();
        }
#elif !UNITY_WEBGL
        if (PlayerPrefs.HasKey(PrefWindowMode))
        {
            int windowMode = PlayerPrefs.GetInt(PrefWindowMode, 0);
            FullScreenMode mode = windowMode switch
            {
                1 => FullScreenMode.ExclusiveFullScreen,
                2 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed
            };
            Screen.fullScreenMode = mode;
            Screen.fullScreen = mode != FullScreenMode.Windowed;
        }
#endif
    }

    public static EventSystem EnsureEventSystem()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem eventSystem = eventSystems.Length > 0 ? eventSystems[0] : null;
        GameObject eventSystemObject = eventSystem ? eventSystem.gameObject : new GameObject("EventSystem");
        eventSystemObject.SetActive(true);

        eventSystem = eventSystemObject.GetOrAddComponent<EventSystem>();
        eventSystem.enabled = true;
        eventSystem.sendNavigationEvents = true;

        InputSystemUIInputModule inputModule = eventSystemObject.GetOrAddComponent<InputSystemUIInputModule>();
        inputModule.enabled = true;
        if (!inputModule.actionsAsset)
        {
            inputModule.AssignDefaultActions();
        }

        BaseInputModule[] inputModules = eventSystemObject.GetComponents<BaseInputModule>();
        for (int i = 0; i < inputModules.Length; i++)
        {
            if (inputModules[i] && inputModules[i] != inputModule)
            {
                inputModules[i].enabled = false;
            }
        }

        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] && eventSystems[i] != eventSystem)
            {
                eventSystems[i].enabled = false;
            }
        }

        return eventSystem;
    }

    public static void EnsureCanvasRaycasters()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i]) continue;
            GraphicRaycaster raycaster = canvases[i].GetComponent<GraphicRaycaster>() ?? canvases[i].gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
        }
    }

    public static InventoryHotbar EnsureGameplayInventoryUi()
    {
        Inventory inventory = Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (!inventory)
        {
            inventory = new GameObject("Inventory").AddComponent<Inventory>();
        }
        inventory.gameObject.SetActive(true);
        inventory.enabled = true;

        InventoryTransferController transferController =
            Object.FindFirstObjectByType<InventoryTransferController>(FindObjectsInactive.Include);
        if (!transferController)
        {
            transferController = inventory.gameObject.AddComponent<InventoryTransferController>();
        }
        transferController.gameObject.SetActive(true);
        transferController.enabled = true;

        InventoryHotbar hotbar = Object.FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
        if (hotbar)
        {
            hotbar.gameObject.SetActive(true);
            hotbar.enabled = true;

            Canvas existingCanvas = hotbar.GetComponentInParent<Canvas>(true);
            if (existingCanvas)
            {
                existingCanvas.gameObject.SetActive(true);
                existingCanvas.enabled = true;
                existingCanvas.GetOrAddComponent<GraphicRaycaster>().enabled = true;
            }

            return hotbar;
        }

        InventoryHotbar prefabHotbar = InstantiateInventoryPrefab();
        if (prefabHotbar)
        {
            return prefabHotbar;
        }

        RectTransform canvasRect = CreateOverlayCanvas("GameplayInventoryCanvas", 20);
        GameObject hotbarObject = new("HotbarRoot", typeof(RectTransform), typeof(InventoryHotbar));
        RectTransform hotbarRect = hotbarObject.GetComponent<RectTransform>();
        hotbarRect.SetParent(canvasRect, false);

        return hotbarObject.GetComponent<InventoryHotbar>();
    }

    public static void HideGameplayInventoryUi()
    {
        InventoryHotbar hotbar = Object.FindFirstObjectByType<InventoryHotbar>(FindObjectsInactive.Include);
        if (hotbar)
        {
            Canvas canvas = hotbar.GetComponentInParent<Canvas>(true);
            if (canvas)
            {
                canvas.gameObject.SetActive(false);
            }
            else
            {
                hotbar.gameObject.SetActive(false);
            }
        }
    }

    public static PauseMenuUI EnsureGameplayPauseUi()
    {
        PauseMenuUI pauseMenu = Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu)
        {
            pauseMenu.gameObject.SetActive(true);
            pauseMenu.enabled = true;

            RectTransform pauseRect = pauseMenu.transform as RectTransform;
            if (pauseRect)
            {
                StretchToParent(pauseRect);
                pauseRect.localScale = Vector3.one;
            }

            Canvas existingCanvas = pauseMenu.GetComponentInParent<Canvas>(true);
            if (existingCanvas)
            {
                existingCanvas.gameObject.SetActive(true);
                existingCanvas.enabled = true;
                existingCanvas.overrideSorting = true;
                existingCanvas.sortingOrder = Mathf.Max(existingCanvas.sortingOrder, 5000);
                existingCanvas.GetOrAddComponent<GraphicRaycaster>().enabled = true;

                RectTransform existingCanvasRect = existingCanvas.transform as RectTransform;
                if (existingCanvasRect)
                {
                    StretchToParent(existingCanvasRect);
                    existingCanvasRect.localScale = Vector3.one;
                }
            }

            if (!Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include))
            {
                RectTransform settingsParent = existingCanvas
                    ? existingCanvas.transform as RectTransform
                    : pauseRect;
                if (settingsParent)
                {
                    CreateSettingsPanel(settingsParent);
                }
            }

            pauseMenu.RefreshRuntimeBindings();
            return pauseMenu;
        }

        PauseMenuUI prefabPauseMenu = InstantiatePausePrefab();
        if (prefabPauseMenu)
        {
            if (!Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include))
            {
                Canvas prefabCanvas = prefabPauseMenu.GetComponentInParent<Canvas>(true);
                RectTransform settingsParent = prefabCanvas
                    ? prefabCanvas.transform as RectTransform
                    : prefabPauseMenu.transform as RectTransform;
                if (settingsParent)
                {
                    CreateSettingsPanel(settingsParent);
                }
            }

            prefabPauseMenu.RefreshRuntimeBindings();
            return prefabPauseMenu;
        }

        RectTransform canvasRect = CreateOverlayCanvas("GameplayPauseCanvas", 5000);
        Canvas canvas = canvasRect.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        GameObject root = new("PauseMenuRuntime", typeof(RectTransform), typeof(PauseMenuUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvasRect, false);
        StretchToParent(rootRect);

        CreatePauseMenuPanel(rootRect);
        CreateSettingsPanel(rootRect);
        PauseMenuUI runtimePauseMenu = root.GetComponent<PauseMenuUI>();
        runtimePauseMenu.RefreshRuntimeBindings();
        return runtimePauseMenu;
    }

    private static InventoryHotbar InstantiateInventoryPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(InventoryPrefabResourcePath);
        if (!prefab) return null;

        GameObject instance = Object.Instantiate(prefab);
        instance.name = prefab.name;
        instance.SetActive(true);

        Canvas canvas = instance.GetComponentInChildren<Canvas>(true);
        if (canvas)
        {
            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 20);
            canvas.GetOrAddComponent<GraphicRaycaster>().enabled = true;
        }

        InventoryHotbar hotbar = instance.GetComponentInChildren<InventoryHotbar>(true);
        if (hotbar)
        {
            hotbar.gameObject.SetActive(true);
            hotbar.enabled = true;
        }

        return hotbar;
    }

    private static PauseMenuUI InstantiatePausePrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(PausePrefabResourcePath);
        if (!prefab) return null;

        GameObject instance = Object.Instantiate(prefab);
        instance.name = prefab.name;
        instance.SetActive(true);

        Canvas canvas = instance.GetComponentInChildren<Canvas>(true);
        if (canvas)
        {
            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 5000);
            canvas.GetOrAddComponent<GraphicRaycaster>().enabled = true;
        }

        PauseMenuUI pauseMenu = instance.GetComponentInChildren<PauseMenuUI>(true);
        if (pauseMenu)
        {
            pauseMenu.gameObject.SetActive(true);
            pauseMenu.enabled = true;
        }

        return pauseMenu;
    }

    private static void CreatePauseMenuPanel(RectTransform parent)
    {
        GameObject panel = new("PauseMenu", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(420f, 360f);

        panel.GetComponent<Image>().color = new Color(0.055f, 0.047f, 0.07f, 0.96f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateLabel(rect, "Title", "Paused", 34f, 56f);
        CreateButton(rect, "ResumeButton", "Resume", 300f, 48f);
        CreateButton(rect, "SettingsButton", "Settings", 300f, 48f);
        CreateButton(rect, "MainMenuButton", "Main Menu", 300f, 48f);
    }

    private static void CreateSettingsPanel(RectTransform parent)
    {
        GameObject panel = new("SettingsPanelRoot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(SettingsPanel));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(480f, 640f);

        panel.GetComponent<Image>().color = new Color(0.055f, 0.047f, 0.07f, 0.96f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateLabel(rect, "Title", "Settings", 30f, 48f);
        CreateSliderRow(rect, "MasterVolumeRow", "Master Volume");
        CreateSliderRow(rect, "MusicVolumeRow", "Music Volume");
        CreateSliderRow(rect, "SfxVolumeRow", "SFX Volume");
        CreateDropdownRow(rect, "WindowModeRow", "Window Mode");
        CreateToggleRow(rect, "VSyncRow", "VSync");
        CreateLabel(rect, "SaveStatus", "No save data", 15f, 24f);
        CreateButton(rect, "SaveButton", "Save", 300f, 42f);
        CreateButton(rect, "LoadButton", "Load", 300f, 42f);
        CreateButton(rect, "DeleteSaveButton", "Delete Save", 300f, 42f);
        CreateButton(rect, "BackButton", "Back", 300f, 48f);
        panel.SetActive(false);
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize, float height)
    {
        GameObject labelObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 360f;
        layout.preferredHeight = height;
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
        return label;
    }

    private static Button CreateButton(RectTransform parent, string name, string text, float width, float height)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.15f, 1f);

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
        return buttonObject.GetComponent<Button>();
    }

    private static void CreateSliderRow(RectTransform parent, string name, string label)
    {
        GameObject row = CreateRow(parent, name);
        CreateRowLabel(row.transform, label);
        GameObject sliderObject = new("Slider", typeof(RectTransform), typeof(Slider));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.SetParent(row.transform, false);
        sliderRect.sizeDelta = new Vector2(210f, 20f);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    private static void CreateDropdownRow(RectTransform parent, string name, string label)
    {
        GameObject row = CreateRow(parent, name);
        CreateRowLabel(row.transform, label);
        GameObject dropdownObject = new("WindowModeDropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
        dropdownRect.SetParent(row.transform, false);
        dropdownRect.sizeDelta = new Vector2(210f, 36f);
        dropdownObject.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.15f, 1f);
    }

    private static void CreateToggleRow(RectTransform parent, string name, string label)
    {
        GameObject row = CreateRow(parent, name);
        CreateRowLabel(row.transform, label);
        GameObject toggleObject = new("VSyncToggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.SetParent(row.transform, false);
        toggleRect.sizeDelta = new Vector2(32f, 32f);
        Image background = toggleObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.11f, 0.15f, 1f);

        GameObject checkmarkObject = new("Checkmark", typeof(RectTransform), typeof(Image));
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.SetParent(toggleRect, false);
        checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;
        Image checkmark = checkmarkObject.GetComponent<Image>();
        checkmark.color = new Color(0.96f, 0.93f, 0.86f, 1f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
    }

    private static GameObject CreateRow(RectTransform parent, string name)
    {
        GameObject row = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredWidth = 400f;
        element.preferredHeight = 42f;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private static void CreateRowLabel(Transform parent, string text)
    {
        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(parent, false);
        labelRect.sizeDelta = new Vector2(160f, 32f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 17f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
    }

    public static RectTransform CreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = root.GetComponent<RectTransform>();
        StretchToParent(rect);
        return rect;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Debug.Log("Quit is not available in WebGL.");
#else
        Application.Quit();
#endif
    }
}
