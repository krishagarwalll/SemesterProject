using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionClickHandler : MonoBehaviour
{
    void Update()
    {
        if (InteractionLock.IsLocked)
            {
                return;
            }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos3D = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mousePos = new Vector2(mousePos3D.x, mousePos3D.y);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Walkable"))
                    continue;

                //check for minigame trigger
                var bowl = hit.collider.GetComponentInParent<BowlMinigameTrigger>();
                if (bowl != null)
                {
                    Debug.Log("BOWL TRIGGERED");
                    bowl.TriggerMinigame();
                    break;
                }

                //popup system
                var popup = hit.collider.GetComponentInParent<PopUpInteraction>();
                if (popup != null)
                {
                    popup.HandleClick();
                    break;
                }

                //fallback to team system
                var target = hit.collider.GetComponentInParent<InteractionTarget>();
                if (target != null)
                {
                    target.OnClicked();
                    break;
                }
            }
        }
    }
}