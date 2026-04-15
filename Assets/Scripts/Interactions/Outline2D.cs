using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline2D : MonoBehaviour
{
    [FieldHeader("Mode")]
    [SerializeField] private OutlineMode outlineMode = OutlineMode.SpriteProxyWithTint;
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private bool autoCreateSpriteOutlines = true;

    [FieldHeader("Outline")]
    [SerializeField] private Color outlineColor = new(0.05f, 0.03f, 0.01f, 0.95f);
    [SerializeField, Range(0f, 1f)] private float outlineThickness = 0.06f;

    [FieldHeader("Tint")]
    [SerializeField] private Color spriteHighlightColor = new(1f, 0.95f, 0.65f, 1f);
    [SerializeField, Range(0f, 1f)] private float spriteHighlightStrength = 0.2f;

    private readonly List<SpriteRenderer> spriteRenderers = new();
    private readonly List<Color> spriteColors = new();
    private readonly List<SpriteOutlineProxy> spriteOutlineProxies = new();

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        outlineThickness = Mathf.Clamp01(outlineThickness);
        spriteHighlightStrength = Mathf.Clamp01(spriteHighlightStrength);
        Rebuild();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (spriteRenderers.Count == 0)
        {
            Rebuild();
        }

        bool useProxyOutline = outlineMode is OutlineMode.SpriteProxyOnly or OutlineMode.SpriteProxyWithTint;
        bool useTint = outlineMode is OutlineMode.TintOnly or OutlineMode.SpriteProxyWithTint;
        for (int i = 0; i < spriteOutlineProxies.Count; i++)
        {
            SpriteOutlineProxy proxy = spriteOutlineProxies[i];
            if (proxy)
            {
                proxy.SetOutline(highlighted && useProxyOutline, outlineColor, outlineThickness);
            }
        }

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (!spriteRenderer)
            {
                continue;
            }

            Color baseColor = spriteColors[i];
            spriteRenderer.color = highlighted && useTint
                ? Color.Lerp(baseColor, spriteHighlightColor, spriteHighlightStrength)
                : baseColor;
        }
    }

    private void Rebuild()
    {
        spriteRenderers.Clear();
        spriteColors.Clear();
        spriteOutlineProxies.Clear();

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];
            if (!spriteRenderer || spriteRenderer.gameObject.name == "_SpriteOutlineProxy")
            {
                continue;
            }

            spriteRenderers.Add(spriteRenderer);
            spriteColors.Add(spriteRenderer.color);

            SpriteOutlineProxy proxy = spriteRenderer.GetComponent<SpriteOutlineProxy>();
            if (!proxy && autoCreateSpriteOutlines)
            {
                proxy = spriteRenderer.gameObject.GetOrAddComponent<SpriteOutlineProxy>();
            }

            if (proxy)
            {
                spriteOutlineProxies.Add(proxy);
                proxy.SetOutline(false, outlineColor, outlineThickness);
            }
        }
    }
}

public enum OutlineMode
{
    TintOnly = 0,
    SpriteProxyOnly = 1,
    SpriteProxyWithTint = 2
}
