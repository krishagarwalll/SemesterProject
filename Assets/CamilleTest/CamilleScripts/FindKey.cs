using UnityEngine;

public class FindKey : MonoBehaviour
{
    public static FindKey Instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keySound;

    [SerializeField] private GameObject root;

    //object to enable when key is found
    [SerializeField] private GameObject objectToEnable;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (audioSource == null)
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

        root.SetActive(false); //better than disabling whole object
    }

    public void OnKeyFound()
    {
        Debug.Log("KEY FOUND");

        //TURN ON THE OBJECT
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