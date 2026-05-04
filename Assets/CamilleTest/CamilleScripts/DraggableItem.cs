using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, 
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dragSound;

    private float soundCooldown = 0.1f;
    private float lastSoundTime;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.ignoreListenerPause = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();

        PlayDragSound();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float dragSensitivity = 0.5f; // try 0.3–0.7
        rect.anchoredPosition += (eventData.delta / canvas.scaleFactor) * dragSensitivity;
        
        // play sound repeatedly but controlled
        if (Time.time - lastSoundTime > soundCooldown)
        {
            PlayDragSound();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
    }

    private void PlayDragSound()
    {
        if (dragSound == null) return;

        audioSource.PlayOneShot(dragSound);
        lastSoundTime = Time.time;
    }
}