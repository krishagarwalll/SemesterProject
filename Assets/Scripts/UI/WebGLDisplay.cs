using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLDisplay
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void HTGH_SetFullscreen(int enabled);

    [DllImport("__Internal")]
    private static extern int HTGH_IsFullscreen();
#endif

    public static bool IsFullscreen
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return HTGH_IsFullscreen() != 0;
#else
            return Screen.fullScreen;
#endif
        }
    }

    public static void SetFullscreen(bool enabled)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // This call must remain directly inside the UI click callback so browsers
        // recognize it as a user gesture.
        HTGH_SetFullscreen(enabled ? 1 : 0);
#else
        Screen.fullScreenMode = enabled
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        Screen.fullScreen = enabled;
#endif
    }

    public static void ToggleFullscreen() => SetFullscreen(!IsFullscreen);
}
