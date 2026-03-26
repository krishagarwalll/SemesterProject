using UnityEngine;

namespace StoryTool.Samples.VisualNovel
{
    public class BackgroundSoundService : MonoBehaviour
    {
        [SerializeField]
        private AudioSource audioSource;

        void Awake()
        {
            audioSource.loop = true;
        }

        public void Play(AudioClip clip)
        {
            if (audioSource.clip == clip)
            {
                return;
            }
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}