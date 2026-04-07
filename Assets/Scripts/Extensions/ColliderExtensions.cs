using UnityEngine;

public static class ColliderExtensions
{
    public static bool IsUsable(this Collider collider)
    {
        return collider && collider.enabled && collider.gameObject.activeInHierarchy;
    }

    public static InteractionTarget ResolveInteractionTarget(this Collider collider)
    {
        if (!collider)
        {
            return null;
        }

        return collider.GetComponent<InteractionTarget>() ?? collider.GetComponentInParent<InteractionTarget>();
    }
}
