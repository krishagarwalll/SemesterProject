using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroCutscene : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject cutsceneCanvas;
    [SerializeField] private Button skipButton;

    private bool hasEnded;

    private void Start()
    {
        if (videoPlayer) videoPlayer.loopPointReached += _ => End();
        if (skipButton) skipButton.onClick.AddListener(End);

        PauseService.Pause(PauseType.Input);
    }

    private void OnDestroy()
    {
        if (!hasEnded) PauseService.Resume(PauseType.Input);
    }

    private void Update()
    {
        if (!hasEnded && Input.anyKeyDown)
            End();
    }

    private void End()
    {
        if (hasEnded) return;
        hasEnded = true;
        if (videoPlayer) videoPlayer.Stop();
        if (cutsceneCanvas) cutsceneCanvas.SetActive(false);
        PauseService.Resume(PauseType.Input);
    }
}
