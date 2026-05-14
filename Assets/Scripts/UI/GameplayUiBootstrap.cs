using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayUiBootstrap
{
    private const string MainMenuSceneName = "MainMenu";
    private const string RuntimeRootName = "GameplayUIRuntime";

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
        if (!scene.IsValid() || scene.name == MainMenuSceneName)
        {
            return;
        }

        RuntimeUiUtility.ResetRuntimeState();
        RuntimeUiUtility.EnsureCoreSystems();
        RuntimeUiUtility.EnsureEventSystem();
        RuntimeUiUtility.EnsureCanvasRaycasters();

        GameObject root = GameObject.Find(RuntimeRootName);
        if (!root)
        {
            root = RuntimeUiUtility.CreateOverlayCanvas(RuntimeRootName, 850).gameObject;
        }

        if (!Object.FindFirstObjectByType<PauseInputHandler>(FindObjectsInactive.Include))
        {
            root.GetOrAddComponent<PauseInputHandler>();
        }

        if (!Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include))
        {
            root.GetOrAddComponent<PauseMenuUI>();
        }

        if (!Object.FindFirstObjectByType<AutoSaveIndicator>(FindObjectsInactive.Include))
        {
            root.GetOrAddComponent<AutoSaveIndicator>();
        }
    }

}
