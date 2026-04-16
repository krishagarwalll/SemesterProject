using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    private INPCInteractable interactableInRange = null;
    public GameObject interactableIcon;

    void Start()
    {
        interactableIcon.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        INPCInteractable interactable = other.GetComponentInParent<INPCInteractable>();

        if (interactable != null && interactable.CanInteract())
        {
            interactableInRange = interactable;
            interactableIcon.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        INPCInteractable interactable = other.GetComponentInParent<INPCInteractable>();

        if (interactable != null && interactable == interactableInRange)
        {
            interactableInRange = null;
            interactableIcon.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && interactableInRange != null)
        {
            interactableInRange.Interact();
        }
    }
}