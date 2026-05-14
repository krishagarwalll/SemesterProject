using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class RuntimeUiUtility
{
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
        if (!AudioManager.Instance)
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        if (!SaveManager.Instance)
        {
            new GameObject("SaveManager").AddComponent<SaveManager>();
        }
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
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
