using UnityEngine;
using TMPro;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject containerObj;
    [SerializeField] private PlayerInteract playerInteract;
    void Update()
    {
        if (playerInteract.GetInteractableObject() != null)
        {
            Show(playerInteract.GetInteractableObject());
        }
        else
        {
            Hide();
        }
    }

    private void Show(NPCInteractable npcInteractable)
    {
        containerObj.SetActive(true);
    }

    private void Hide()
    {
        containerObj.SetActive(false);
    }
}
