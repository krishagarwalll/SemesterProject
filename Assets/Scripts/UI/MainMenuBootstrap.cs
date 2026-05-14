using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuBootstrap
{
    private const string MainMenuSceneName = "MainMenu";
    private const string DefaultGameSceneName = "Sprint3";
    private const string RuntimeRootName = "MainMenuRuntime";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryBuild(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBuild(scene);
    }

    private static void TryBuild(Scene scene)
    {
        if (scene.name != MainMenuSceneName)
        {
            return;
        }

        RuntimeUiUtility.ResetRuntimeState();
        RuntimeUiUtility.EnsureCoreSystems();
        RuntimeUiUtility.EnsureEventSystem();
        RuntimeUiUtility.EnsureCanvasRaycasters();

        if (GameObject.Find(RuntimeRootName))
        {
            return;
        }

        BuildMenu();
    }

    private static void BuildMenu()
    {
        RectTransform rootRect = RuntimeUiUtility.CreateOverlayCanvas(RuntimeRootName, 100);

        Image background = CreatePanel(rootRect, "Background", new Color(0f, 0f, 0f, 0.55f));
        background.raycastTarget = false;
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        RectTransform content = CreateContent(rootRect);
        CreateLabel(content, "How To Get To Heaven", 46f, 96f);
        bool hasSave = SaveManager.Instance && SaveManager.Instance.HasSave();
        Button startOrContinueButton = CreateButton(content, hasSave ? "Continue" : "Start", 320f, 64f);
        Button optionsButton = CreateButton(content, "Options", 320f, 64f);
        Button quitButton = CreateButton(content, "Quit", 320f, 64f);

        GameObject settingsRoot = CreateSettingsPanel(rootRect);
        SettingsPanel settingsPanel = settingsRoot.GetComponent<SettingsPanel>();
        settingsPanel.BackRequested += () =>
        {
            settingsRoot.SetActive(false);
            content.gameObject.SetActive(true);
        };
        settingsRoot.SetActive(false);

        startOrContinueButton.onClick.AddListener(() =>
        {
            PauseService.ClearAll();
            if (SaveManager.Instance && SaveManager.Instance.HasSave())
            {
                SaveManager.Instance.LoadAndApply();
                return;
            }

            SceneManager.LoadScene(DefaultGameSceneName);
        });

        optionsButton.onClick.AddListener(() =>
        {
            content.gameObject.SetActive(false);
            settingsRoot.SetActive(true);
        });

        quitButton.onClick.AddListener(RuntimeUiUtility.QuitApplication);
    }

    private static RectTransform CreateContent(RectTransform parent)
    {
        GameObject contentObject = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.SetParent(parent, false);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(440f, 420f);
        Image panelImage = contentObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.96f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 36, 36);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return content;
    }

    private static GameObject CreateSettingsPanel(RectTransform parent)
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

    private static Image CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject panelObject = new(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        return image;
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
        layout.preferredWidth = 720f;
        layout.preferredHeight = height;
    }

    private static Button CreateButton(RectTransform parent, string label, float width, float height)
    {
        GameObject buttonObject = new(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.25f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.34f, 0.38f, 1f);
        colors.pressedColor = new Color(0.12f, 0.15f, 0.18f, 1f);
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.45f);
        button.colors = colors;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        GameObject textObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
        textMesh.text = label;
        textMesh.fontSize = 28f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.raycastTarget = false;

        return button;
    }
}
