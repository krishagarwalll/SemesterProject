using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayUiBootstrap
{
    private const string MainMenuSceneName = "MainMenu";

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
        if (IsMinigameScene(scene))
        {
            RuntimeUiUtility.HideGameplayInventoryUi();
        }
        else
        {
            RuntimeUiUtility.EnsureGameplayInventoryUi();
        }

        RuntimeUiUtility.EnsureGameplayPauseUi();

        EnsureScenePauseInput();

        EnsureAutoSaveIndicator();
    }

    private static bool IsMinigameScene(Scene scene)
    {
        if (scene.path.Contains("/Minigames/"))
        {
            return true;
        }

        return scene.name.Contains("Minigame", System.StringComparison.OrdinalIgnoreCase)
            || scene.name.Contains("MiniGame", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureScenePauseInput()
    {
        const string runtimeInputName = "PauseInputRuntime";
        PauseInputHandler[] pauseInputs = Object.FindObjectsByType<PauseInputHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        PauseInputHandler runtimeInput = null;
        for (int i = 0; i < pauseInputs.Length; i++)
        {
            if (pauseInputs[i] && pauseInputs[i].gameObject.name == runtimeInputName)
            {
                runtimeInput = pauseInputs[i];
                break;
            }
        }

        if (!runtimeInput)
        {
            GameObject pauseInputRoot = new(runtimeInputName);
            runtimeInput = pauseInputRoot.AddComponent<PauseInputHandler>();
        }

        runtimeInput.gameObject.SetActive(true);
        runtimeInput.enabled = true;

        for (int i = 0; i < pauseInputs.Length; i++)
        {
            if (!pauseInputs[i]) continue;
            if (pauseInputs[i] != runtimeInput)
            {
                pauseInputs[i].enabled = false;
            }
        }
    }

    private static void EnsureAutoSaveIndicator()
    {
        if (Object.FindFirstObjectByType<AutoSaveIndicator>(FindObjectsInactive.Include))
        {
            return;
        }

        Debug.LogWarning("No AutoSaveIndicator found in the scene. Add the AutoSaveIndicator prefab to your UI canvas.");
    }

    private static Canvas FindGameplayCanvas()
    {
        PauseMenuUI pauseMenu = Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu)
        {
            Canvas pauseCanvas = pauseMenu.GetComponentInParent<Canvas>(true);
            if (pauseCanvas)
            {
                return pauseCanvas;
            }
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] && canvases[i].isRootCanvas)
            {
                return canvases[i];
            }
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }
}
