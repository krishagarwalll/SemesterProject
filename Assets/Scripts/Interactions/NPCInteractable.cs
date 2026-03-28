using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private string interactiveText; 
    public void Interact() {
        Debug.Log("Interacting with NPC");
    }
    
    public string GetInteractiveText() {
        return interactiveText;
    }
}
