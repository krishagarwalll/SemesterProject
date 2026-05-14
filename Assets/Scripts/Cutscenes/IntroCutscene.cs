using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

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
    private bool hasEnded;
    private bool inputPausedByThisCutscene;

    public string SaveId => ResolveSaveId();
    public bool HasEnded => hasEnded;

    protected virtual void Awake()
    {
        ResolveReferences();
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
    }

    protected virtual void OnDestroy()
    {
        playback?.Unbind();
        if (skipButton) skipButton.onClick.RemoveListener(Skip);

        if (!hasEnded && inputPausedByThisCutscene)
        {
            PauseService.Resume(PauseType.Input);
            inputPausedByThisCutscene = false;
        }
    }

    protected virtual void Update()
    {
        if (hasEnded)
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

    public void Skip() => End(markComplete: true);

    public void End() => End(markComplete: true);

    public void RestoreCompleted() => End(markComplete: false);

    protected void End(bool markComplete)
    {
        if (hasEnded) return;
        hasEnded = true;

        playback?.Stop();
        playback?.Unbind();

        if (markComplete && playOnce && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkCutsceneCompleted(SaveId, saveCompletionImmediately);
        }

        if (inputPausedByThisCutscene)
        {
            PauseService.Resume(PauseType.Input);
            inputPausedByThisCutscene = false;
        }

        if (cutsceneRoot)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (!cutsceneRoot)
        {
            cutsceneRoot = gameObject;
        }

        if (!videoPlayer)
        {
            videoPlayer = cutsceneRoot.GetComponentInChildren<VideoPlayer>(true);
        }

        if (!playableDirector)
        {
            playableDirector = cutsceneRoot.GetComponentInChildren<PlayableDirector>(true);
        }

        if (!skipButton)
        {
            skipButton = cutsceneRoot.GetComponentInChildren<Button>(true);
        }
    }

    private void Complete()
    {
        End(markComplete: true);
    }

    private void FailOpen()
    {
        End(markComplete: false);
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
        if (!cutsceneRoot)
        {
            cutsceneRoot = gameObject;
        }

        EnsureSerializedSaveId();
    }

    private string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
        {
            return saveId;
        }

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
        return $"{sceneName}:cutscene:{GetHierarchyPath(transform)}";
    }

    private void EnsureSerializedSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId) || Application.isPlaying)
        {
            return;
        }

        saveId = Guid.NewGuid().ToString("N");
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (!current)
        {
            return string.Empty;
        }

        string path = current.name;
        while (current.parent)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}

public class IntroCutscene : CutsceneController
{
}
