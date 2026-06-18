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

        EnsureScenePauseInput();

        EnsureAutoSaveIndicator();
    }

    private static void EnsureScenePauseInput()
    {
        PauseInputHandler[] pauseInputs = Object.FindObjectsByType<PauseInputHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (pauseInputs.Length == 0)
        {
            Debug.LogWarning("No PauseInputHandler found in the loaded gameplay scene. Add one to the scene Services object.");
            return;
        }

        PauseInputHandler enabledInput = null;
        for (int i = 0; i < pauseInputs.Length; i++)
        {
            if (!pauseInputs[i]) continue;

            if (!enabledInput)
            {
                enabledInput = pauseInputs[i];
                enabledInput.enabled = true;
            }
            else
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
