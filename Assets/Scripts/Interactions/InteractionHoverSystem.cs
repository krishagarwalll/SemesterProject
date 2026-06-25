using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHoverSystem : MonoBehaviour
{
    private InteractionTarget currentHover;
    private Camera cam;

    void Awake()
    {
        // PointerContext owns hover selection and feedback in current gameplay scenes.
        // Keeping both active causes duplicate hover changes and audio on disabled targets.
        if (FindFirstObjectByType<PointerContext>(FindObjectsInactive.Include))
        {
            enabled = false;
            return;
        }

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
                newHover.SetHovered(true, HasEnabledAction(newHover));

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

    private static bool HasEnabledAction(InteractionTarget target)
    {
        if (!target) return false;
        PoptropicaController actor = FindFirstObjectByType<PoptropicaController>(FindObjectsInactive.Include);
        Inventory inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        InteractionContext context = new(actor, null, target, inventory);
        return target.HasAnyEnabledAction(context);
    }
}
