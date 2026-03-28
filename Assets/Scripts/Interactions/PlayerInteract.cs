using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour{
    private void Update(){
        if (Keyboard.current.eKey.wasPressedThisFrame) {
            float interactRange = 2f;
            Collider2D[] colliderArray = Physics2D.OverlapCircleAll(transform.position, interactRange);
            foreach (Collider2D collider in colliderArray) {
                if (collider.TryGetComponent(out NPCInteractable npcInteractable)) {
                    npcInteractable.Interact();
                }
            }
        }
    }
    
    public NPCInteractable GetInteractableObject() {
        float interactRange = 3f;
        Collider2D[] colliderArray = Physics2D.OverlapCircleAll(transform.position, interactRange);
        foreach (Collider2D collider in colliderArray) {
            if (collider.TryGetComponent(out NPCInteractable npcInteractable)) {
                return npcInteractable;
            }
        }
        return null;
    }
}
