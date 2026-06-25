using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InteractionClickHandler : MonoBehaviour
{
    private Component pressedLegacyInteraction;

    void Update()
    {
        if (Mouse.current == null)
        {
            pressedLegacyInteraction = null;
            return;
        }

        if (InteractionLock.IsLocked)
        {
            pressedLegacyInteraction = null;
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressedLegacyInteraction = IsPointerOverUi()
                ? null
                : FindLegacyInteraction(Mouse.current.position.ReadValue());
        }

        if (!Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return;
        }

        Component releasedInteraction = IsPointerOverUi()
            ? null
            : FindLegacyInteraction(Mouse.current.position.ReadValue());
        if (!pressedLegacyInteraction || releasedInteraction != pressedLegacyInteraction)
        {
            pressedLegacyInteraction = null;
            return;
        }

        if (pressedLegacyInteraction is BowlMinigameTrigger bowl)
        {
            bowl.TriggerMinigame();
        }
        else if (pressedLegacyInteraction is PopUpInteraction popup)
        {
            popup.HandleClick();
        }

        pressedLegacyInteraction = null;
    }

    private static Component FindLegacyInteraction(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (!camera) return null;

        Vector3 mousePos3D = camera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos3D, Vector2.zero);
        foreach (RaycastHit2D hit in hits)
        {
            if (!hit.collider || hit.collider.gameObject.layer == LayerMask.NameToLayer("Walkable"))
            {
                continue;
            }

            BowlMinigameTrigger bowl = hit.collider.GetComponentInParent<BowlMinigameTrigger>();
            if (bowl)
            {
                return bowl;
            }

            PopUpInteraction popup = hit.collider.GetComponentInParent<PopUpInteraction>();
            if (popup) return popup;
        }

        return null;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
    }
}
