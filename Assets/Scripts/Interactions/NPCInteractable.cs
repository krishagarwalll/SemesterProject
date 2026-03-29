
using UnityEngine;
using TMPro;
public class NPCInteractable : MonoBehaviour
{

    [SerializeField] private GameObject speechBubble;
    [SerializeField] private float interactThingoOffset;
    [SerializeField] private string dialogue;
    public void Interact() {
        Debug.Log("Interacting with NPC");
        
        GameObject bubble = Instantiate(speechBubble, new Vector3(transform.position.x, transform.position.y - interactThingoOffset, transform.position.z), transform.rotation);
        
        TextMeshPro textComponent = bubble.GetComponentInChildren<TextMeshPro>();
        
        textComponent.text = dialogue;
        
        Debug.Log(textComponent.text);
    }
}
