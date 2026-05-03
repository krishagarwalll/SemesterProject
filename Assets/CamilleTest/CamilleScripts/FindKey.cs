using UnityEngine;

public class FindKey : MonoBehaviour
{
    public static FindKey Instance;
    private PointClickController player;
    private PointerContext pointer;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keySound;

    [SerializeField] private GameObject root;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);

        //if (audioSource == null)
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
    }

    public void Open()
    {
        root.SetActive(true);

        InteractionLock.IsLocked = true;
    }

    public void Close()
    {
        Debug.Log("Minigame Close");

        if (audioSource && keySound)
            audioSource.PlayOneShot(keySound);

        InteractionLock.IsLocked = false;

        gameObject.SetActive(false);
    }

    public void OnKeyFound()
    {
        Debug.Log("KEY FOUND ");

        Close();
    }
}