using UnityEngine;

public class FindKey : MonoBehaviour
{
    public static FindKey Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keySound;

    [SerializeField] private AudioClip minigameMusic;

    [Header("UI")]
    [SerializeField] private GameObject root;

    [Header("Reward")]
    [SerializeField] private GameObject objectToEnable;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.ignoreListenerPause = true;
        audioSource.loop = true; 
    }

    public void Open()
    {
        Debug.Log("MINIGAME OPEN");

        if (InteractionLock.IsLocked) return;
        root.SetActive(true);
        InteractionLock.IsLocked = true;

        //Pause all other audio
        AudioListener.pause = true;

        //Play minigame music
        if (minigameMusic != null)
        {
            audioSource.clip = minigameMusic;
            audioSource.Play();
        }
    }

    public void Close()
    {
        Debug.Log("Minigame Close");

        // Resume all other audio
        AudioListener.pause = false;
        audioSource.Stop();

        // Play key sound (still works because ignoreListenerPause = true)
        if (audioSource && keySound)
            audioSource.PlayOneShot(keySound);

        if (!InteractionLock.IsLocked) return;

        InteractionLock.IsLocked = false;
        root.SetActive(false);
    }

    public void OnKeyFound()
    {
        Debug.Log("KEY FOUND");

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
            Debug.Log("Activated: " + objectToEnable.name);
        }
        else
        {
            Debug.LogWarning("No object assigned to enable!");
        }

        Close();
    }
}