using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RuntimeUiPrefabBuilder
{
    private const string ResourcesUiDirectory = "Assets/Resources/UI";
    private const string InventoryPrefabPath = ResourcesUiDirectory + "/GameplayInventoryCanvas.prefab";
    private const string PausePrefabPath = ResourcesUiDirectory + "/GameplayPauseCanvas.prefab";
    private const string ExistingSettingsPrefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";

    static RuntimeUiPrefabBuilder()
    {
        EditorApplication.delayCall -= EnsureRuntimeUiPrefabs;
        EditorApplication.delayCall += EnsureRuntimeUiPrefabs;
    }

    [MenuItem("Tools/UI/Create Missing Runtime UI Prefabs")]
    public static void EnsureRuntimeUiPrefabs()
    {
        Directory.CreateDirectory(ResourcesUiDirectory);

        bool createdAny = false;
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefabPath))
        {
            CreateInventoryPrefab();
            createdAny = true;
        }

        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath))
        {
            CreatePausePrefab();
            createdAny = true;
        }

        if (createdAny)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void CreateInventoryPrefab()
    {
        GameObject root = CreateCanvasRoot("GameplayInventoryCanvas", sortingOrder: 20);
        RectTransform canvasRect = root.transform as RectTransform;

        GameObject hotbarObject = CreateUiObject("HotbarRoot", canvasRect, typeof(InventoryHotbar));
        RectTransform hotbarRect = hotbarObject.GetComponent<RectTransform>();
        hotbarRect.anchorMin = new Vector2(0.5f, 1f);
        hotbarRect.anchorMax = new Vector2(0.5f, 1f);
        hotbarRect.pivot = new Vector2(0.5f, 1f);
        hotbarRect.anchoredPosition = new Vector2(0f, -24f);
        hotbarRect.sizeDelta = new Vector2(892f, 136f);

        GameObject slots = CreateUiObject("Slots", hotbarRect, typeof(HorizontalLayoutGroup));
        RectTransform slotsRect = slots.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 1f);
        slotsRect.anchorMax = new Vector2(0f, 1f);
        slotsRect.pivot = new Vector2(0f, 0.5f);
        slotsRect.anchoredPosition = new Vector2(12f, -68f);
        slotsRect.sizeDelta = new Vector2(732f, 112f);

        HorizontalLayoutGroup layout = slots.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < 6; i++)
        {
            InventoryHotbarSlot.Create(slotsRect, new Vector2(112f, 112f));
        }

        Button backpackButton = CreateButton(hotbarRect, "BackpackButton", "Bag", new Vector2(112f, 112f));
        RectTransform backpackRect = backpackButton.transform as RectTransform;
        backpackRect.anchorMin = new Vector2(1f, 0.5f);
        backpackRect.anchorMax = new Vector2(1f, 0.5f);
        backpackRect.pivot = new Vector2(1f, 0.5f);
        backpackRect.anchoredPosition = new Vector2(-12f, 0f);

        InventoryHotbarSlot collectibleSlot = InventoryHotbarSlot.Create(hotbarRect, new Vector2(112f, 112f));
        collectibleSlot.name = "MemoryFragmentSlot";
        RectTransform collectibleRect = collectibleSlot.transform as RectTransform;
        collectibleRect.anchorMin = new Vector2(0f, 0.5f);
        collectibleRect.anchorMax = new Vector2(0f, 0.5f);
        collectibleRect.pivot = new Vector2(0f, 0.5f);
        collectibleRect.anchoredPosition = new Vector2(12f, 0f);
        collectibleSlot.gameObject.SetActive(false);

        InventoryHotbar hotbar = hotbarObject.GetComponent<InventoryHotbar>();
        SerializedObject serializedHotbar = new(hotbar);
        serializedHotbar.FindProperty("panel").objectReferenceValue = hotbarRect;
        serializedHotbar.FindProperty("slotContainer").objectReferenceValue = slotsRect;
        serializedHotbar.FindProperty("backpackButton").objectReferenceValue = backpackButton;
        serializedHotbar.FindProperty("collectibleSlot").objectReferenceValue = collectibleSlot;
        serializedHotbar.FindProperty("slotSize").vector2Value = new Vector2(112f, 112f);
        serializedHotbar.ApplyModifiedPropertiesWithoutUndo();

        SavePrefabAndDestroy(root, InventoryPrefabPath);
    }

    private static void CreatePausePrefab()
    {
        GameObject root = CreateCanvasRoot("GameplayPauseCanvas", sortingOrder: 5000);
        RectTransform canvasRect = root.transform as RectTransform;

        GameObject controllerObject = CreateUiObject("PauseMenuRuntime", canvasRect, typeof(PauseMenuUI));
        RectTransform controllerRect = controllerObject.GetComponent<RectTransform>();
        Stretch(controllerRect);

        GameObject panel = CreateUiObject("PauseMenu", controllerRect, typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(420f, 360f);
        panel.GetComponent<Image>().color = new Color(0.055f, 0.047f, 0.07f, 0.96f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateLabel(panelRect, "Title", "Paused", 34f, 300f, 56f);
        Button resume = CreateButton(panelRect, "ResumeButton", "Resume", new Vector2(300f, 48f));
        CreateButton(panelRect, "SettingsButton", "Settings", new Vector2(300f, 48f));
        CreateButton(panelRect, "MainMenuButton", "Main Menu", new Vector2(300f, 48f));

        GameObject settingsPanel = InstantiateExistingSettingsPrefab(controllerRect);

        PauseMenuUI pauseMenu = controllerObject.GetComponent<PauseMenuUI>();
        SerializedObject serializedPause = new(pauseMenu);
        serializedPause.FindProperty("pauseMenu").objectReferenceValue = panel;
        serializedPause.FindProperty("settingsPanelRoot").objectReferenceValue = settingsPanel;
        serializedPause.FindProperty("resumeButton").objectReferenceValue = resume;
        serializedPause.ApplyModifiedPropertiesWithoutUndo();

        SavePrefabAndDestroy(root, PausePrefabPath);
    }

    private static GameObject InstantiateExistingSettingsPrefab(RectTransform parent)
    {
        GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExistingSettingsPrefabPath);
        GameObject instance;
        if (settingsPrefab)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab);
            instance.name = "SettingsPanelRoot";
            instance.transform.SetParent(parent, false);
        }
        else
        {
            instance = CreateUiObject("SettingsPanelRoot", parent, typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(SettingsPanel));
            CreateLabel(instance.transform as RectTransform, "Title", "Settings", 30f, 300f, 48f);
            CreateButton(instance.transform as RectTransform, "BackButton", "Back", new Vector2(300f, 48f));
        }

        RectTransform rect = instance.transform as RectTransform;
        if (rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        instance.SetActive(false);
        return instance;
    }

    private static GameObject CreateCanvasRoot(string name, int sortingOrder)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = UiLayer();
        RectTransform rect = root.GetComponent<RectTransform>();
        Stretch(rect);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return root;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new(name, PrependRectTransform(components));
        gameObject.layer = UiLayer();
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static System.Type[] PrependRectTransform(System.Type[] components)
    {
        System.Type[] types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        for (int i = 0; i < components.Length; i++)
        {
            types[i + 1] = components[i];
        }

        return types;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.11f, 0.15f, 1f);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        CreateLabel(rect, "Label", label, 20f, size.x, size.y);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize, float width, float height)
    {
        GameObject labelObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SavePrefabAndDestroy(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static int UiLayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 0;
    }
}
