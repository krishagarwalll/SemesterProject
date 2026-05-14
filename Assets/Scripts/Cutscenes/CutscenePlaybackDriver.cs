using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

public sealed class CutscenePlaybackDriver
{
    private readonly VideoPlayer videoPlayer;
    private readonly PlayableDirector playableDirector;
    private readonly UnityEngine.Object logContext;
    private readonly string logName;

    private double normalDirectorSpeed = 1d;
    private float normalVideoSpeed = 1f;
    private Action completed;
    private Action failed;

    public CutscenePlaybackDriver(
        VideoPlayer videoPlayer,
        PlayableDirector playableDirector,
        UnityEngine.Object logContext,
        string logName)
    {
        this.videoPlayer = videoPlayer;
        this.playableDirector = playableDirector;
        this.logContext = logContext;
        this.logName = logName;
    }

    public void Bind(Action onCompleted, Action onFailed)
    {
        completed = onCompleted;
        failed = onFailed;

        normalVideoSpeed = videoPlayer ? videoPlayer.playbackSpeed : 1f;
        CacheDirectorSpeed();

        if (playableDirector) playableDirector.stopped += HandleDirectorStopped;
        if (videoPlayer)
        {
            videoPlayer.loopPointReached += HandleVideoFinished;
            videoPlayer.errorReceived += HandleVideoError;
        }
    }

    public void Unbind()
    {
        if (playableDirector) playableDirector.stopped -= HandleDirectorStopped;
        if (videoPlayer)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        completed = null;
        failed = null;
    }

    public void SetFastForward(bool enabled, float multiplier)
    {
        float speed = enabled ? Mathf.Max(1f, multiplier) : 1f;
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

    public void Stop()
    {
        SetFastForward(false, 1f);
        if (videoPlayer) videoPlayer.Stop();
        if (playableDirector) playableDirector.Stop();
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

    private void HandleVideoFinished(VideoPlayer player)
    {
        completed?.Invoke();
    }

    private void HandleVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning($"[CutsceneController] Video playback failed on '{logName}': {message}", logContext);
        failed?.Invoke();
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        completed?.Invoke();
    }
}
