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
    private bool shuttingDown;
    private RenderTexture playbackTexture;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(MemoryFragmentCutscenePlayer)} disabled on '{name}'.", this);
            enabled = false;
            return;
        }

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
            videoPlayer.waitForFirstFrame = true;
            playbackTexture = videoPlayer.targetTexture;
        }

        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        Shutdown();

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
        playback?.SetFastForward(fastForwardHeld, fastForwardSpeed);
        skipButtonPresenter?.Tick(fastForwardHeld);
    }

    public bool Play(string fileName, string saveId, bool playOnce)
    {
        if (isPlaying)
        {
            Debug.LogWarning($"[{nameof(MemoryFragmentCutscenePlayer)}] Ignored '{fileName}' because another cutscene is playing.", this);
            return false;
        }

        if (!videoPlayer)
        {
            Debug.LogError($"[{nameof(MemoryFragmentCutscenePlayer)}] No VideoPlayer is assigned.", this);
            return false;
        }

        if (!StreamingAssetsVideoUrl.TryBuild(fileName, out string url))
        {
            Debug.LogError($"[{nameof(MemoryFragmentCutscenePlayer)}] Invalid cutscene file name '{fileName}'.", this);
            return false;
        }

        if (playOnce && SaveManager.Instance != null && SaveManager.Instance.IsCutsceneCompleted(saveId))
        {
            Debug.Log($"[{nameof(MemoryFragmentCutscenePlayer)}] '{fileName}' was already completed.", this);
            return false;
        }

        activeSaveId = saveId;
        activePlayOnce = playOnce;
        BeginPlayback(fileName, url);
        return true;
    }

    public void Skip()
    {
        if (isPlaying)
        {
            FinishPlayback(markComplete: true);
        }
    }

    private void BeginPlayback(string fileName, string url)
    {
        isPlaying = true;

        videoPlayer.enabled = true;
        if (!videoPlayer.targetTexture && playbackTexture)
        {
            videoPlayer.targetTexture = playbackTexture;
        }

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.clip = null;
        videoPlayer.url = url;
        videoPlayer.isLooping = false;
        videoPlayer.playbackSpeed = 1f;

        playback = new CutscenePlaybackDriver(videoPlayer, null, this, $"{name}:{fileName}");
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

        // Keep both calls in the pickup's pointer gesture. WebGL browsers can
        // reject audible media when playback is first requested on a later frame.
        videoPlayer.Prepare();
        videoPlayer.Play();
        Debug.Log($"[{nameof(MemoryFragmentCutscenePlayer)}] Starting '{fileName}' from '{url}'.", this);
        StartCoroutine(WatchPlaybackStart(fileName));
    }

    private IEnumerator WatchPlaybackStart(string fileName)
    {
        float timeout = 30f;
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
            Debug.LogError(
                $"[{nameof(MemoryFragmentCutscenePlayer)}] Timed out preparing '{fileName}' from '{videoPlayer.url}'.",
                this);
            FinishPlayback(markComplete: false);
        }
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
        StopAllCoroutines();

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
        if (inputPaused)
        {
            PauseService.Resume(CutscenePause);
            inputPaused = false;
        }

        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    private void Shutdown()
    {
        if (shuttingDown)
        {
            return;
        }

        shuttingDown = true;
        StopAllCoroutines();
        playback?.Unbind();
        playback = null;

        if (skipButton)
        {
            skipButton.onClick.RemoveListener(Skip);
        }

        if (inputPaused)
        {
            PauseService.Resume(CutscenePause);
            inputPaused = false;
        }

        if (videoPlayer)
        {
            // The Windows Media Foundation decoder can still submit a decoded
            // frame while Play Mode is being torn down. Detach the render target
            // first so the D3D12 worker cannot register a texture being destroyed.
            videoPlayer.targetTexture = null;
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }

        isPlaying = false;
    }

    private void OnApplicationQuit() => Shutdown();

#if UNITY_EDITOR
    private void OnDisable()
    {
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Shutdown();
        }
    }
#endif
}
