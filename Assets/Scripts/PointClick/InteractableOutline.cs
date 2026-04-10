using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractableOutline : MonoBehaviour
{
#if UNITY_6000_0_OR_NEWER
    [SerializeField] private RenderingLayerMask outlineLayer = 1;
#else
    [SerializeField] private int outlineLayer = 1;
#endif
    [SerializeField] private bool includeInactiveChildren = true;

    private readonly List<Renderer> outlinedRenderers = new();
    private readonly List<uint> originalMasks = new();

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outlinedRenderers.Count == 0)
        {
            Rebuild();
        }

        uint layerMask = GetLayerMask();
        for (int i = 0; i < outlinedRenderers.Count; i++)
        {
            Renderer renderer = outlinedRenderers[i];
            if (!renderer)
            {
                continue;
            }

            renderer.renderingLayerMask = highlighted
                ? originalMasks[i] | layerMask
                : originalMasks[i];
        }
    }

    private void Rebuild()
    {
        outlinedRenderers.Clear();
        originalMasks.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].gameObject.name == "_SpriteOutlineProxy")
            {
                continue;
            }

            if (renderers[i] is SpriteRenderer spriteRenderer)
            {
                SpriteOutlineProxy proxy = spriteRenderer.GetOrAddComponent<SpriteOutlineProxy>();
                Renderer proxyRenderer = proxy.ProxyRenderer;
                if (proxyRenderer)
                {
                    outlinedRenderers.Add(proxyRenderer);
                    originalMasks.Add(proxyRenderer.renderingLayerMask);
                }

                continue;
            }

            outlinedRenderers.Add(renderers[i]);
            originalMasks.Add(renderers[i].renderingLayerMask);
        }
    }

    private uint GetLayerMask()
    {
#if UNITY_6000_0_OR_NEWER
        return outlineLayer;
#else
        return 1u << (int)Mathf.Log(outlineLayer, 2);
#endif
    }
}
