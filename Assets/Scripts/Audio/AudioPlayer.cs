using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    private AudioSource source;
    public AudioSource Source
    {
        get
        {
            if (!source) source = GetComponent<AudioSource>();
            if (!source) { source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0f; source.loop = false; }
            return source;
        }
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        Source.pitch = Mathf.Max(0.01f, pitch);
        Source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlayClip(AudioClip clip, float volume = 1f, bool loop = false, float pitch = 1f)
    {
        if (!clip) return;
        Source.Stop();
        Source.clip = clip;
        Source.volume = Mathf.Clamp01(volume);
        Source.pitch = Mathf.Max(0.01f, pitch);
        Source.loop = loop;
        Source.Play();
    }

    public void Stop()
    {
        Source.Stop();
        Source.loop = false;
        Source.clip = null;
    }

    public static void PlayAtPoint(AudioClip clip, Vector3 worldPosition, float volume = 1f)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, worldPosition, Mathf.Clamp01(volume));
    }
}
