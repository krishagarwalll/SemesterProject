using UnityEngine;

[DisallowMultipleComponent]
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;

    private void Awake()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    private void OnEnable()
    {
        PauseService.PauseChanged += HandlePauseChanged;
    }

    private void OnDisable()
    {
        PauseService.PauseChanged -= HandlePauseChanged;
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(isPaused);
    }
}
