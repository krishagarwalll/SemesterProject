using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InteractionPanelUI : MonoBehaviour
{
    public static InteractionPanelUI Instance;

    [Header("Objects")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;

        closeButton.onClick.AddListener(Hide);
        backgroundButton.onClick.AddListener(Hide);

        HideInstant();
    }

    public void Show(Sprite sprite, string itemName, string desc)
    {
        panelRoot.SetActive(true);

        itemImage.sprite = sprite;
        nameText.text = itemName;
        descriptionText.text = desc;

        if (audioSource && openSound)
            audioSource.PlayOneShot(openSound);

        StartFade(0, 1);
    }

    public void Hide()
    {
        if (audioSource && closeSound)
            audioSource.PlayOneShot(closeSound);

        StartFade(1, 0, true);
    }

    void HideInstant()
    {
        panelRoot.SetActive(false);
        canvasGroup.alpha = 0;
    }

    void StartFade(float from, float to, bool disableAfter = false)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(from, to, disableAfter));
    }

    IEnumerator FadeRoutine(float from, float to, bool disableAfter)
    {
        float time = 0f;
        float duration = 0.2f;

        canvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        canvasGroup.alpha = to;

        if (disableAfter)
            panelRoot.SetActive(false);
    }
}