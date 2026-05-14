using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
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
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private GameObject cutsceneRoot;
    [SerializeField] private Button skipButton;
    [SerializeField] private bool pausePlayerInput = true;

    [Header("Input")]
    [SerializeField] private InputActionReference skipAction;
    [SerializeField] private InputActionReference fastForwardAction;
    [SerializeField] private bool anyKeyboardKeySkips;
    [SerializeField] private Key fallbackSkipKey = Key.Escape;
    [SerializeField] private Key fallbackFastForwardKey = Key.Space;
    [SerializeField, Min(1f)] private float fastForwardSpeed = 4f;

    private bool hasEnded;
    private double normalDirectorSpeed = 1d;
    private float normalVideoSpeed = 1f;

    public string SaveId => ResolveSaveId();
    public bool HasEnded => hasEnded;

    protected virtual void Start()
    {
        if (playOnce && SaveManager.Instance != null && SaveManager.Instance.IsCutsceneCompleted(SaveId))
        {
            RestoreCompleted();
            return;
        }

        normalVideoSpeed = videoPlayer ? videoPlayer.playbackSpeed : 1f;
        CacheDirectorSpeed();

        if (playableDirector) playableDirector.stopped += HandleDirectorStopped;
        if (videoPlayer) videoPlayer.loopPointReached += HandleVideoFinished;
        if (skipButton) skipButton.onClick.AddListener(Skip);

        EnableAction(skipAction);
        EnableAction(fastForwardAction);

        if (pausePlayerInput)
        {
            PauseService.Pause(PauseType.Input);
        }
    }

    protected virtual void OnDestroy()
    {
        if (videoPlayer) videoPlayer.loopPointReached -= HandleVideoFinished;
        if (playableDirector) playableDirector.stopped -= HandleDirectorStopped;
        if (skipButton) skipButton.onClick.RemoveListener(Skip);

        if (!hasEnded && pausePlayerInput)
        {
            PauseService.Resume(PauseType.Input);
        }
    }

    protected virtual void Update()
    {
        if (hasEnded)
        {
            return;
        }

        if (WasSkipPressed())
        {
            Skip();
            return;
        }

        SetFastForward(IsFastForwardHeld());
    }

    public void Skip()
    {
        End(markComplete: true);
    }

    public void End()
    {
        End(markComplete: true);
    }

    public void RestoreCompleted()
    {
        End(markComplete: false);
    }

    protected void End(bool markComplete)
    {
        if (hasEnded) return;
        hasEnded = true;
        SetFastForward(false);
        if (videoPlayer) videoPlayer.Stop();
        if (playableDirector) playableDirector.Stop();
        if (cutsceneRoot) cutsceneRoot.SetActive(false);

        if (markComplete && playOnce && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkCutsceneCompleted(SaveId, saveCompletionImmediately);
        }

        if (pausePlayerInput)
        {
            PauseService.Resume(PauseType.Input);
        }
    }

    private void CacheDirectorSpeed()
    {
        if (!playableDirector)
        {
            normalDirectorSpeed = 1d;
            return;
        }

        normalDirectorSpeed = playableDirector.playableGraph.IsValid()
            && playableDirector.playableGraph.GetRootPlayableCount() > 0
            ? playableDirector.playableGraph.GetRootPlayable(0).GetSpeed()
            : 1d;
    }

    private void SetFastForward(bool enabled)
    {
        float speed = enabled ? fastForwardSpeed : 1f;
        if (videoPlayer)
        {
            videoPlayer.playbackSpeed = normalVideoSpeed * speed;
        }

        if (playableDirector
            && playableDirector.playableGraph.IsValid()
            && playableDirector.playableGraph.GetRootPlayableCount() > 0)
        {
            playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(normalDirectorSpeed * speed);
        }
    }

    private bool WasSkipPressed()
    {
        if (skipAction && skipAction.action != null && skipAction.action.WasPressedThisFrame())
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (anyKeyboardKeySkips && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        return fallbackSkipKey != Key.None && keyboard[fallbackSkipKey].wasPressedThisFrame;
    }

    private bool IsFastForwardHeld()
    {
        if (fastForwardAction && fastForwardAction.action != null)
        {
            return fastForwardAction.action.IsPressed();
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null
            && fallbackFastForwardKey != Key.None
            && keyboard[fallbackFastForwardKey].isPressed;
    }

    private void HandleVideoFinished(VideoPlayer player)
    {
        End(markComplete: true);
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (!hasEnded)
        {
            End(markComplete: true);
        }
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

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference && actionReference.action != null && !actionReference.action.enabled)
        {
            actionReference.action.Enable();
        }
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
