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
