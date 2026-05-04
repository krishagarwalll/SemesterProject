using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHoverSystem : MonoBehaviour
{
    private InteractionTarget currentHover;

    void Update()
    {
        // BLOCK EVERYTHING when minigame/UI is open
        if (InteractionLock.IsLocked)
        {
            // Clear current hover if something was hovered before
            if (currentHover != null)
            {
                currentHover.SetHovered(false);
                currentHover = null;
            }
            return;
        }

        if (Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);

        InteractionTarget newHover = FindTarget(mouseWorld);

        if (newHover != currentHover)
        {
            if (currentHover != null)
                currentHover.SetHovered(false);

            if (newHover != null)
                newHover.SetHovered(true);

            currentHover = newHover;
        }
    }

    private InteractionTarget FindTarget(Vector2 position)
    {
        InteractionTarget[] targets = FindObjectsOfType<InteractionTarget>();

        InteractionTarget best = null;
        int bestPriority = int.MinValue;

        foreach (var target in targets)
        {
            if (!target.ContainsPoint(position))
                continue;

            if (target.SelectionPriority > bestPriority)
            {
                best = target;
                bestPriority = target.SelectionPriority;
            }
        }

        return best;
    }
}