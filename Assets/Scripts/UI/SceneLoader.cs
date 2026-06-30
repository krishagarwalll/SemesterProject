using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int sceneIndex;
    [SerializeField, Min(0f)] private float musicFadeDuration = 0.5f;

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
        StartCoroutine(FadeAudioAndLoad(() => SceneManager.LoadScene(index)));
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Scene name is empty.", this);
            return;
        }

        ResetRuntimeStateBeforeSceneChange();
        StartCoroutine(FadeAudioAndLoad(() => SceneManager.LoadScene(sceneName)));
    }

    public void QuitGame()
    {
        RuntimeUiUtility.QuitApplication();
    }

    private IEnumerator FadeAudioAndLoad(System.Action load)
    {
        if (AudioManager.Instance)
            yield return AudioManager.Instance.FadeOutMusic(musicFadeDuration);
        load();
    }

    private static void ResetRuntimeStateBeforeSceneChange()
    {
        RuntimeUiUtility.ResetRuntimeState();
    }
}
