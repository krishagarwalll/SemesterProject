using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenMiniGame : MonoBehaviour
{
    [SerializeField] private string sceneName = "RippedUpLetterMiniGame";

    public void OpenScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
