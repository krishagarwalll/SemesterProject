using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class MemoryFragmentCutscenePlayer : MonoBehaviour
{
    public static MemoryFragmentCutscenePlayer Instance { get; private set; }

    [Header("Playback")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField, Min(1f)] private float fastForwardSpeed = 4f;

    [Header("Input")]
    [SerializeField] private CutsceneInputBindings input = new();

    [Header("Skip Button")]
    [SerializeField] private Button skipButton;
    [SerializeField, Min(0f)] private float revealSkipButtonAfterFastForwardSeconds = 1.25f;
    [SerializeField, Min(0.01f)] private float skipButtonFadeDuration = 0.25f;

    private const PauseType CutscenePause = PauseType.Input | PauseType.Animation | PauseType.Particles;

    private CutscenePlaybackDriver playback;
    private CutsceneSkipButtonPresenter skipButtonPresenter;
    private string activeSaveId;
    private bool activePlayOnce;
    private bool isPlaying;
    private bool inputPaused;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        Instance = this;

        if (!cutsceneRoot)
        {
            cutsceneRoot = gameObject;
        }

        if (!videoPlayer)
        {
            videoPlayer = cutsceneRoot.GetComponentInChildren<VideoPlayer>(true);
        }

        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
        }

        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (isPlaying)
        {
            FinishPlayback(markComplete: false);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        if (input.WasSkipPressed())
        {
            Skip();
            return;
        }

        bool fastForwardHeld = input.IsFastForwardHeld();
        playback.SetFastForward(fastForwardHeld, fastForwardSpeed);
        skipButtonPresenter.Tick(fastForwardHeld);
    }

    public bool Play(VideoClip clip, string saveId, bool playOnce)
    {
        if (isPlaying || !clip || !videoPlayer)
        {
            return false;
        }

        if (playOnce && SaveManager.Instance != null && SaveManager.Instance.IsCutsceneCompleted(saveId))
        {
            return false;
        }

        activeSaveId = saveId;
        activePlayOnce = playOnce;
        StartCoroutine(PlayRoutine(clip));
        return true;
    }

    public void Skip()
    {
        if (isPlaying)
        {
            FinishPlayback(markComplete: true);
        }
    }

    private IEnumerator PlayRoutine(VideoClip clip)
    {
        isPlaying = true;

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.playbackSpeed = 1f;

        playback = new CutscenePlaybackDriver(videoPlayer, null, this, $"{name}:{clip.name}");
        playback.Bind(OnPlaybackCompleted, OnPlaybackFailed);

        skipButtonPresenter = new CutsceneSkipButtonPresenter(
            cutsceneRoot,
            skipButton,
            revealSkipButtonAfterFastForwardSeconds,
            skipButtonFadeDuration);
        skipButtonPresenter.Prepare();
        skipButton = skipButtonPresenter.Button;
        if (skipButton)
        {
            skipButton.onClick.AddListener(Skip);
        }

        input.Enable();

        PauseService.Pause(CutscenePause);
        inputPaused = true;

        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(true);
        }

        yield return null;
        if (!isPlaying)
        {
            yield break;
        }

        videoPlayer.Prepare();
        float timeout = 15f;
        while (!videoPlayer.isPrepared && isPlaying && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!isPlaying)
        {
            yield break;
        }

        if (!videoPlayer.isPrepared)
        {
            FinishPlayback(markComplete: false);
            yield break;
        }

        videoPlayer.Play();
    }

    private void OnPlaybackCompleted() => FinishPlayback(markComplete: true);

    private void OnPlaybackFailed() => FinishPlayback(markComplete: false);

    private void FinishPlayback(bool markComplete)
    {
        if (!isPlaying)
        {
            return;
        }

        isPlaying = false;

        playback?.Stop();
        playback?.Unbind();
        playback = null;

        if (skipButton)
        {
            skipButton.onClick.RemoveListener(Skip);
        }

        if (markComplete && activePlayOnce && !string.IsNullOrEmpty(activeSaveId) && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkCutsceneCompleted(activeSaveId, true);
        }

        if (inputPaused)
        {
            inputPaused = false;
            StartCoroutine(ResumeInputNextFrame());
        }
        else if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    private IEnumerator ResumeInputNextFrame()
    {
        yield return null;
        PauseService.Resume(CutscenePause);
        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }
}
