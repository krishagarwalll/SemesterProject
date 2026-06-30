using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string PrefMusicVolume = "Vol_Music";
    private const string PrefSfxVolume = "Vol_Sfx";
    private const string PrefMasterVolume = "Vol_Master";
    private const string PrefLastAudibleMasterVolume = "Vol_Master_LastAudible";

    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    private AudioPlayer musicPlayer;
    private AudioPlayer sfxPlayer;
    private Coroutine musicFadeCoroutine;
    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float lastAudibleMasterVolume = 1f;

    public static AudioManager Instance { get; private set; }

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool IsMuted => masterVolume <= 0.0001f;
    public AudioMixerGroup MusicGroup => musicGroup;
    public AudioMixerGroup SfxGroup => sfxGroup;

    private AudioPlayer MusicPlayer
    {
        get
        {
            if (musicPlayer) return musicPlayer;
            var go = new GameObject("MusicPlayer");
            go.transform.SetParent(transform);
            musicPlayer = go.AddComponent<AudioPlayer>();
            musicPlayer.Source.loop = true;
            musicPlayer.Source.outputAudioMixerGroup = musicGroup;
            return musicPlayer;
        }
    }

    private AudioPlayer SfxPlayer
    {
        get
        {
            if (sfxPlayer) return sfxPlayer;
            var go = new GameObject("SfxPlayer");
            go.transform.SetParent(transform);
            sfxPlayer = go.AddComponent<AudioPlayer>();
            sfxPlayer.Source.outputAudioMixerGroup = sfxGroup;
            return sfxPlayer;
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumeSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ── Public API ───────────────────────────────────────────────

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!clip) return;
        SfxPlayer.PlayOneShot(clip, sfxVolume * volume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!clip) return;
        if (musicPlayer && musicPlayer.Source.clip == clip && musicPlayer.Source.isPlaying) return;
        MusicPlayer.PlayClip(clip, musicVolume, loop);
    }

    public void StopMusic()
    {
        if (musicPlayer)
        {
            musicPlayer.Stop();
        }
    }

    public void PlayMusicFaded(AudioClip clip, float duration = 0.5f, bool loop = true)
    {
        if (!clip) { StopMusicFaded(duration); return; }
        if (musicPlayer && musicPlayer.Source.clip == clip && musicPlayer.Source.isPlaying) return;
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(CrossfadeRoutine(clip, duration, loop));
    }

    public void StopMusicFaded(float duration = 0.5f)
    {
        if (!musicPlayer || !musicPlayer.Source.isPlaying) return;
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeOutRoutine(duration));
    }

    public IEnumerator FadeOutMusic(float duration = 0.5f)
    {
        if (!musicPlayer || !musicPlayer.Source.isPlaying) yield break;
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = null;
        yield return FadeOutRoutine(duration);
    }

    private IEnumerator CrossfadeRoutine(AudioClip clip, float duration, bool loop)
    {
        float fadeDur = musicPlayer && musicPlayer.Source.isPlaying ? duration * 0.5f : duration;
        if (musicPlayer && musicPlayer.Source.isPlaying)
            yield return FadeOutRoutine(duration * 0.5f);
        MusicPlayer.PlayClip(clip, 0f, loop);
        yield return FadeInRoutine(fadeDur);
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        if (!musicPlayer) yield break;
        float start = musicPlayer.Source.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            if (!musicPlayer) yield break;
            musicPlayer.Source.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        StopMusic();
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            if (!musicPlayer) yield break;
            musicPlayer.Source.volume = Mathf.Lerp(0f, musicVolume, t / duration);
            yield return null;
        }
        if (musicPlayer) musicPlayer.Source.volume = musicVolume;
        musicFadeCoroutine = null;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (masterVolume > 0.0001f)
        {
            lastAudibleMasterVolume = masterVolume;
            PlayerPrefs.SetFloat(PrefLastAudibleMasterVolume, lastAudibleMasterVolume);
        }

        PlayerPrefs.SetFloat(PrefMasterVolume, masterVolume);
        PlayerPrefs.Save();
        ApplyMasterOutput();
        ApplyMusicVolume();
    }

    public void ToggleMute()
    {
        SetMasterVolume(IsMuted ? Mathf.Max(0.01f, lastAudibleMasterVolume) : 0f);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefMusicVolume, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefSfxVolume, sfxVolume);
        PlayerPrefs.Save();
    }

    public void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PrefMasterVolume, defaultMasterVolume);
        musicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, defaultSfxVolume);
        lastAudibleMasterVolume = PlayerPrefs.GetFloat(
            PrefLastAudibleMasterVolume,
            masterVolume > 0.0001f ? masterVolume : defaultMasterVolume);
        ApplyMasterOutput();
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicPlayer && musicPlayer.Source.isPlaying)
        {
            musicPlayer.Source.volume = musicVolume;
        }
    }

    private void ApplyMasterOutput()
    {
        // PauseService owns the temporary fade while paused. It reads MasterVolume
        // again when resuming, so mute/unmute changes made in the pause menu persist.
        if (IsMuted)
        {
            AudioListener.volume = 0f;
            return;
        }

        if (!PauseService.IsPaused(PauseType.Audio))
        {
            AudioListener.volume = masterVolume;
        }
    }
}
