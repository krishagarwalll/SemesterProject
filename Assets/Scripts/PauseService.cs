using System;
using UnityEngine;

public static class PauseService
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> PauseChanged;

    public static void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        PauseChanged?.Invoke(true);
    }

    public static void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        PauseChanged?.Invoke(false);
    }

    public static void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
