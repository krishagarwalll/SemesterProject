using UnityEngine;
using UnityEngine.UI;

public class InteractionPanelUI : MonoBehaviour
{
    public static InteractionPanelUI Instance;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image itemImage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple InteractionPanelUI instances!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();
    }

    // public void Show(Sprite sprite)
    // {
    //     Debug.Log("SHOW FUNCTION RUNNING");

    //     panelRoot.SetActive(true);
    //     itemImage.sprite = sprite;

    //     //itemImage.color = Color.red; // debug
    // }

    // public void Show(Sprite sprite)
    // {
    //     Debug.Log("SHOW FUNCTION RUNNING");

    //     gameObject.SetActive(true);
    //     panelRoot.SetActive(true);

    //     itemImage.sprite = sprite;
    // }

    public void Show(Sprite sprite)
    {
        Debug.Log("SHOW RUNNING");

        gameObject.SetActive(true);
        panelRoot.SetActive(true);

        panelRoot.transform.SetAsLastSibling();

        itemImage.sprite = sprite;
        //itemImage.color = Color.white;
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    public void Toggle(Sprite sprite)
    {
        if (panelRoot.activeSelf)
            Hide();
        else
            Show(sprite);
    }
}