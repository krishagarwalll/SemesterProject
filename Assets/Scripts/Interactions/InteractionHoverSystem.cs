using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHoverSystem : MonoBehaviour
{
    private InteractionTarget currentHover;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (!cam) cam = Camera.main;
        if (!cam) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

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
        var targets = InteractionTarget.ActiveTargets;

        InteractionTarget best = null;
        int bestPriority = int.MinValue;

        for (int i = 0; i < targets.Count; i++)
        {
            InteractionTarget target = targets[i];
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