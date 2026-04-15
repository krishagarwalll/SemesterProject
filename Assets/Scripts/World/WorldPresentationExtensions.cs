using UnityEngine;

public static class WorldPresentationExtensions
{
    public static void ApplyWorldPresentation(this Component component, string layerName, string sortingLayerName)
    {
        if (!component)
        {
            return;
        }

        component.transform.SetLayerRecursively(layerName);
        component.transform.SetSortingLayerRecursively(sortingLayerName);
    }

    public static void SetLayerRecursively(this Transform root, string layerName)
    {
        if (!root)
        {
            return;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            return;
        }

        SetLayerRecursively(root, layer);
    }

    public static void SetSortingLayerRecursively(this Transform root, string sortingLayerName)
    {
        if (!root || string.IsNullOrWhiteSpace(sortingLayerName))
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = sortingLayerName;
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
