using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    private INPCInteractable interactableInRange = null;
    public GameObject interactableIcon;

    private void Awake()
    {
        Debug.Log("InteractionDetector is on player");
    }
    void Start()
    {
        interactableIcon.SetActive(false);
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        INPCInteractable interactable = other.GetComponentInParent<INPCInteractable>();

        if (interactable != null && interactable.CanInteract())
        {
            interactableInRange = interactable;
            interactableIcon.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        INPCInteractable interactable = other.GetComponentInParent<INPCInteractable>();

        if (interactable != null && interactable == interactableInRange)
        {
            if (interactable is NPC npc)
            {
                npc.StopDialogue();
            }
            interactableInRange = null;
            interactableIcon.SetActive(false);
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            interactableInRange?.Interact();
        }
    }
}