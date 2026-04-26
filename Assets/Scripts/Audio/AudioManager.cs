using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string PrefMusicVolume = "Vol_Music";
    private const string PrefSfxVolume = "Vol_Sfx";

    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;

    private AudioPlayer musicPlayer;
    private AudioPlayer sfxPlayer;
    private float musicVolume;
    private float sfxVolume;

    public static AudioManager Instance { get; private set; }

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private AudioPlayer MusicPlayer
    {
        get
        {
            if (musicPlayer) return musicPlayer;
            var go = new GameObject("MusicPlayer");
            go.transform.SetParent(transform);
            musicPlayer = go.AddComponent<AudioPlayer>();
            musicPlayer.Source.loop = true;
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

    // ── Public API ───────────────────────────────────────────────

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!clip) return;
        SfxPlayer.PlayOneShot(clip, sfxVolume * volume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!clip) return;
        MusicPlayer.PlayClip(clip, musicVolume, loop);
    }

    public void StopMusic() => MusicPlayer.Stop();

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefMusicVolume, musicVolume);
        if (MusicPlayer.Source.isPlaying)
            MusicPlayer.Source.volume = musicVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefSfxVolume, sfxVolume);
    }

    public void LoadVolumeSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, defaultSfxVolume);
    }
}
