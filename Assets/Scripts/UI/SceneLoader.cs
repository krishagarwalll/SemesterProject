using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int sceneIndex;

    public void LoadScene()
    {
        LoadScene(sceneIndex);
    }

    public void LoadScene(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[SceneLoader] Scene build index {index} is not in Build Settings.", this);
            return;
        }

        ResetRuntimeStateBeforeSceneChange();
        SceneManager.LoadScene(index);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Scene name is empty.", this);
            return;
        }

        ResetRuntimeStateBeforeSceneChange();
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private static void ResetRuntimeStateBeforeSceneChange()
    {
        PauseService.ClearAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
