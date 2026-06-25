using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CutsceneController : MonoBehaviour
{
    [Header("Save")]
    [SerializeField] private string saveId;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool saveCompletionImmediately = true;

    [Header("Playback")]
    [FormerlySerializedAs("VideoPlayer")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private PlayableDirector playableDirector;
    [FormerlySerializedAs("cutsceneCanvas")]
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField] private bool pausePlayerInput = true;
    [SerializeField, Min(1f)] private float fastForwardSpeed = 4f;

    [Header("Input")]
    [SerializeField] private CutsceneInputBindings input = new();
    [SerializeField, HideInInspector] private InputActionReference skipAction;
    [SerializeField, HideInInspector] private InputActionReference fastForwardAction;
    [SerializeField, HideInInspector] private bool anyKeyboardKeySkips;
    [SerializeField, HideInInspector] private Key fallbackSkipKey = Key.None;
    [SerializeField, HideInInspector] private Key fallbackFastForwardKey = Key.None;

    [Header("Skip Button")]
    [SerializeField] private Button skipButton;
    [SerializeField, Min(0f)] private float revealSkipButtonAfterFastForwardSeconds = 1.25f;
    [SerializeField, Min(0.01f)] private float skipButtonFadeDuration = 0.25f;

    private CutscenePlaybackDriver playback;
    private CutsceneSkipButtonPresenter skipButtonPresenter;
    private GameObject firstFrameCurtain;
    private bool shuttingDown;
    private bool hasEnded;
    private bool inputPausedByThisCutscene;

    public string SaveId => ResolveSaveId();
    public bool HasEnded => hasEnded;

    protected virtual void Awake()
    {
        ResolveReferences();
        NormalizeVideoSourceForBuild();
        CreateFirstFrameCurtain();
        input.ApplyLegacyFields(
            skipAction,
            fastForwardAction,
            anyKeyboardKeySkips,
            fallbackSkipKey,
            fallbackFastForwardKey);
        playback = new CutscenePlaybackDriver(videoPlayer, playableDirector, this, name);
        skipButtonPresenter = new CutsceneSkipButtonPresenter(
            cutsceneRoot,
            skipButton,
            revealSkipButtonAfterFastForwardSeconds,
            skipButtonFadeDuration);
        skipButtonPresenter.Prepare();
        skipButton = skipButtonPresenter.Button;

        if (videoPlayer)
        {
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += HandleFirstVideoFrame;
        }
    }

    protected virtual void Start()
    {
        if (playOnce && SaveManager.Instance != null && SaveManager.Instance.IsCutsceneCompleted(SaveId))
        {
            RestoreCompleted();
            return;
        }

        playback.Bind(Complete, FailOpen);
        if (skipButton) skipButton.onClick.AddListener(Skip);
        input.Enable();

        if (pausePlayerInput)
        {
            PauseService.Pause(PauseType.Input);
            inputPausedByThisCutscene = true;
        }

        if (cutsceneRoot) cutsceneRoot.SetActive(true);
        StartCoroutine(EnsurePlayback());
    }

    protected virtual void OnDestroy()
    {
        ShutdownVideoForTeardown();
        playback?.Unbind();
        if (skipButton) skipButton.onClick.RemoveListener(Skip);

        if (inputPausedByThisCutscene)
        {
            PauseService.Resume(PauseType.Input);
            inputPausedByThisCutscene = false;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        ShutdownVideoForTeardown();
    }

#if UNITY_EDITOR
    protected virtual void OnDisable()
    {
        // Cutscene objects can be toggled during normal startup. Only tear down the
        // native video resources when the Editor is actually leaving Play Mode.
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            ShutdownVideoForTeardown();
    }
#endif

    protected virtual void Update()
    {
        if (hasEnded) return;

        if (input.WasSkipPressed())
        {
            Skip();
            return;
        }

        bool fastForwardHeld = input.IsFastForwardHeld();
        playback?.SetFastForward(fastForwardHeld, fastForwardSpeed);
        skipButtonPresenter?.Tick(fastForwardHeld);
    }

    public void Skip() => End(markComplete: true);
    public void End() => End(markComplete: true);
    public void RestoreCompleted() => End(markComplete: false);

    protected void End(bool markComplete)
    {
        if (hasEnded) return;
        hasEnded = true;

        playback?.Stop();
        playback?.Unbind();
        RemoveFirstFrameCurtain();

        if (markComplete && playOnce && SaveManager.Instance != null)
            SaveManager.Instance.MarkCutsceneCompleted(SaveId, saveCompletionImmediately);

        if (inputPausedByThisCutscene)
        {
            // Delay one frame so the UI click that triggered skip does not leak into game input.
            // PointerContext checks pause state in Update; if we resume synchronously, the same
            // frame the skip button fires, PointerContext can see the mouse release while unpaused
            // and move the character.
            StartCoroutine(DeactivateAndResumeInput());
        }
        else if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    private IEnumerator DeactivateAndResumeInput()
    {
        yield return null; // one frame — keeps input paused past the skip button's click frame
        if (inputPausedByThisCutscene)
        {
            PauseService.Resume(PauseType.Input);
            inputPausedByThisCutscene = false;
        }

        if (cutsceneRoot) cutsceneRoot.SetActive(false);
    }

    private IEnumerator EnsurePlayback()
    {
        if (hasEnded) yield break;

        if (videoPlayer)
        {
            if (!videoPlayer.isPrepared)
            {
                videoPlayer.Prepare();
                float timeout = 15f;
                while (!videoPlayer.isPrepared && !hasEnded && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (hasEnded) yield break;

                if (!videoPlayer.isPrepared)
                {
                    FailOpen();
                    yield break;
                }
            }

            videoPlayer.Play();
            yield break;
        }

        if (playableDirector && playableDirector.state != PlayState.Playing)
            playableDirector.Play();
    }

    private void Complete() => End(markComplete: true);
    private void FailOpen() => End(markComplete: false);

    private void ResolveReferences()
    {
        if (!cutsceneRoot) cutsceneRoot = gameObject;
        if (!videoPlayer) videoPlayer = cutsceneRoot.GetComponentInChildren<VideoPlayer>(true);
        if (!playableDirector) playableDirector = cutsceneRoot.GetComponentInChildren<PlayableDirector>(true);
        if (!skipButton) skipButton = cutsceneRoot.GetComponentInChildren<Button>(true);
    }

    private void NormalizeVideoSourceForBuild()
    {
        if (!videoPlayer)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = $"{Application.streamingAssetsPath}/Cutscenes/beginning-cutscene.mp4";
#else
        if (!videoPlayer.clip || videoPlayer.source != VideoSource.Url)
        {
            return;
        }

        string url = videoPlayer.url;
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.url = string.Empty;
#endif
    }

    private void CreateFirstFrameCurtain()
    {
        if (!cutsceneRoot || firstFrameCurtain) return;

        firstFrameCurtain = new GameObject(
            "FirstFrameCurtain",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        firstFrameCurtain.transform.SetParent(cutsceneRoot.transform, false);
        firstFrameCurtain.transform.SetAsFirstSibling();

        RectTransform rect = firstFrameCurtain.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = firstFrameCurtain.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }

    private void HandleFirstVideoFrame(VideoPlayer player, long frameIndex)
    {
        if (shuttingDown) return;
        RemoveFirstFrameCurtain();
        player.frameReady -= HandleFirstVideoFrame;
        player.sendFrameReadyEvents = false;
    }

    private void RemoveFirstFrameCurtain()
    {
        if (!firstFrameCurtain) return;
        Destroy(firstFrameCurtain);
        firstFrameCurtain = null;
    }

    private void ShutdownVideoForTeardown()
    {
        if (shuttingDown || !videoPlayer) return;
        shuttingDown = true;

        StopAllCoroutines();
        videoPlayer.frameReady -= HandleFirstVideoFrame;
        videoPlayer.sendFrameReadyEvents = false;
        videoPlayer.targetTexture = null;
        videoPlayer.Stop();
        videoPlayer.enabled = false;
    }

    private void Reset()
    {
        videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        playableDirector = GetComponentInChildren<PlayableDirector>(true);
        skipButton = GetComponentInChildren<Button>(true);
        cutsceneRoot = gameObject;
        EnsureSerializedSaveId();
    }

    private void OnValidate()
    {
        if (!cutsceneRoot) cutsceneRoot = gameObject;
        EnsureSerializedSaveId();
    }

    private string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId)) return saveId;
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
        return $"{sceneName}:cutscene:{GetHierarchyPath(transform)}";
    }

    private void EnsureSerializedSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId) || Application.isPlaying) return;
        saveId = Guid.NewGuid().ToString("N");
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (!current) return string.Empty;
        string path = current.name;
        while (current.parent)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }
}

public class IntroCutscene : CutsceneController { }
