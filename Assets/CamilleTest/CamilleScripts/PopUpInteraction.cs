using UnityEngine;

public class PopUpInteraction : MonoBehaviour
{
    [Header("UI Data")]
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private string itemName;
    [TextArea] public string description;

    public void HandleClick()
    {
        Debug.Log("PopUp Click: " + name);
        Debug.Log("CALLING POPUP");

        // Bowl = minigame
        if (CompareTag("Bowl"))
        {
            if (FindKey.Instance != null)
                FindKey.Instance.Open();
            return;
        }

        // Block if minigame open
        if (InteractionLock.IsLocked)
            return;

        // Show UI
        if (InteractionPanelUI.Instance != null)
        {
            InteractionPanelUI.Instance.Show(itemSprite, itemName, description);
        }
        else
        {
            Debug.LogError("InteractionPanelUI Instance missing!");
        }
    }
}