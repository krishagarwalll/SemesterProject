using UnityEngine;

public static class ComponentExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (!gameObject)
        {
            return null;
        }

        return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
    }

    public static T GetOrAddComponent<T>(this Component owner) where T : Component
    {
        return owner ? owner.gameObject.GetOrAddComponent<T>() : null;
    }

    public static Transform EnsureChild(this Transform parent, ref Transform cache, string childName)
    {
        if (!parent)
        {
            return null;
        }

        if (cache && cache.parent == parent)
        {
            if (cache.name != childName)
            {
                cache.name = childName;
            }

            return cache;
        }

        cache = null;
        cache = parent.Find(childName);
        if (cache)
        {
            return cache;
        }

        GameObject child = new(childName);
        cache = child.transform;
        cache.SetParent(parent, false);
        return cache;
    }

    public static T ResolveComponent<T>(this Component owner, ref T cache, bool includeChildren = false) where T : Component
    {
        if (cache)
        {
            return cache;
        }

        if (!owner)
        {
            return null;
        }

        cache = owner.GetComponent<T>();
        if (!cache && includeChildren)
        {
            cache = owner.GetComponentInChildren<T>(true);
        }

        return cache;
    }

    public static T ResolveSceneComponent<T>(this MonoBehaviour owner, ref T cache) where T : Component
    {
        if (cache)
        {
            return cache;
        }

        cache = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        return cache;
    }

    public static T ResolveBehaviour<T>(this Component owner, ref T cache, ref bool resolved, ref MonoBehaviour[] behaviours) where T : class
    {
        if (resolved)
        {
            return cache;
        }

        if (!owner)
        {
            resolved = true;
            return null;
        }

        resolved = true;
        behaviours ??= owner.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is T typedHandler)
            {
                cache = typedHandler;
                break;
            }
        }

        return cache;
    }
}
