using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Obsolete("Legacy Level1 interaction probe. Sprint2 uses InteractionTarget plus PointerContext.", false)]
public class PlayerInteract : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame) 
        {
            IInteractable interactable = GetInteractableObject();
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }
    
    public IInteractable GetInteractableObject() 
    {
        float interactRange = 3f;
        Collider2D[] colliderArray = Physics2D.OverlapCircleAll(transform.position, interactRange);
        foreach (Collider2D collider in colliderArray) 
        {
            if (collider.TryGetComponent(out IInteractable interactable)) 
            {
                return interactable;
            }
        }
        return null;
    }
}
