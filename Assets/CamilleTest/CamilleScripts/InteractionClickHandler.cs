using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionClickHandler : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("CLICK");

            Vector3 mousePos3D = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mousePos = new Vector2(mousePos3D.x, mousePos3D.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("HIT: " + hit.collider.name);

                InteractionTarget target = hit.collider.GetComponentInParent<InteractionTarget>();

                if (target != null)
                {
                    Debug.Log("TARGET FOUND: " + target.name);
                    var bowl = target.GetComponent<BowlMinigameTrigger>();

                    if (bowl != null)
                    {
                        bowl.TriggerMinigame(); // minigame
                    }
                    else
                    {
                        target.OnClicked(); // normal popup
                    }
                }
            }
            else
            {
                Debug.Log("NO HIT");
            }
        }
    }
}