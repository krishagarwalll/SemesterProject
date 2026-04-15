using System;
using TMPro;
using UnityEngine;

[Obsolete("Legacy Level1 interaction prompt. Sprint2 uses InteractionPromptPresenter.", false)]
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

    private void Show(IInteractable interactable) {
        containerGameObject.SetActive(true);
        interactTextMeshProUGUI.text = interactable.GetInteractiveText();
    }

    private void hide() {
        containerGameObject.SetActive(false);
    }
}
