using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable {
    [SerializeField] private string interactiveText; 
    public void Interact() {
        Debug.Log(interactiveText);
    }
    
    public string GetInteractiveText() {
        return interactiveText;
    }
}
