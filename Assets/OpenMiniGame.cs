using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenMiniGame : MonoBehaviour
{
    [SerializeField] private string sceneName = "RippedUpLetterMiniGame";

    public void OpenScene()
    {
        Debug.Log("Opening mini-game scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
