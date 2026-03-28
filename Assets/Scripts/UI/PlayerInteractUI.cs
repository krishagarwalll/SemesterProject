using System;
using TMPro;
using UnityEngine;

public class PlayerInteractUI : MonoBehaviour{
    
    [SerializeField] private GameObject containerGameObject;
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private TextMeshProUGUI interactTextMeshProUGUI;

    private void Update()
    {
        if (playerInteract.GetInteractableObject() != null) {
            Show(playerInteract.GetInteractableObject());
        } else {
            hide();
        }
    }

    private void Show(NPCInteractable npcInteractable) {
        containerGameObject.SetActive(true);
        interactTextMeshProUGUI.text = npcInteractable.GetInteractiveText();
    }

    private void hide() {
        containerGameObject.SetActive(false);
    }
}