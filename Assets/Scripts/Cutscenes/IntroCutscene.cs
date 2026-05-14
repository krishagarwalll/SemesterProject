using UnityEngine;
using UnityEngine.Video;

public class IntroCutscene : MonoBehaviour
{
    public VideoPlayer VideoPlayer;

    public GameObject cutsceneCanvas;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        VideoPlayer.loopPointReached += EndVideo;
    }
    

    void EndVideo(VideoPlayer vp)
    {
        cutsceneCanvas.SetActive(false);
    }
}
