using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline2D : MonoBehaviour
{
    [FieldHeader("Mode")]
    [SerializeField] private OutlineMode outlineMode = OutlineMode.SpriteProxyWithTint;
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private bool autoCreateSpriteOutlines = true;
    [SerializeField] private bool restrictToPrimarySortingLayer = true;

    [FieldHeader("Outline")]
    [SerializeField] private Color outlineColor = new(1f, 0.82f, 0.28f, 0.9f);
    [SerializeField, Range(0f, 1f)] private float outlineThickness = 0.025f;

    [FieldHeader("Tint")]
    [SerializeField] private Color highlightColor = new(1f, 0.94f, 0.68f, 1f);
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.22f;

    private readonly List<SpriteRenderer> spriteRenderers = new();
    private readonly List<Color> spriteColors = new();
    private readonly List<SpriteOutlineGroup> spriteOutlineGroups = new();
    private readonly List<Renderer> tintRenderers = new();
    private readonly List<Color> tintColors = new();
    private readonly List<MaterialPropertyBlock> tintBlocks = new();
    private InteractionTarget ownerTarget;
    private bool hasPrimarySortingLayer;
    private int primarySortingLayerId;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        outlineThickness = Mathf.Clamp01(outlineThickness);
        highlightStrength = Mathf.Clamp01(highlightStrength);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= DelayedEditorRebuild;
            UnityEditor.EditorApplication.delayCall += DelayedEditorRebuild;
            return;
        }
#endif

        Rebuild();
    }

#if UNITY_EDITOR
    private void DelayedEditorRebuild()
    {
        UnityEditor.EditorApplication.delayCall -= DelayedEditorRebuild;
        if (!this || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        Rebuild();
    }
#endif

    public void SetHighlighted(bool highlighted)
    {
        if (spriteRenderers.Count == 0)
        {
            Rebuild();
        }

        bool useSpriteOutline = outlineMode is OutlineMode.SpriteProxyOnly or OutlineMode.SpriteProxyWithTint;
        bool useTint = outlineMode is OutlineMode.TintOnly or OutlineMode.SpriteProxyWithTint;
        for (int i = 0; i < spriteOutlineGroups.Count; i++)
        {
            spriteOutlineGroups[i].SetOutline(highlighted && useSpriteOutline, outlineColor, outlineThickness);
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
                ? Color.Lerp(baseColor, highlightColor, highlightStrength)
                : baseColor;
        }

        for (int i = 0; i < tintRenderers.Count; i++)
        {
            Renderer renderer = tintRenderers[i];
            if (!renderer)
            {
                continue;
            }

            MaterialPropertyBlock block = tintBlocks[i];
            renderer.GetPropertyBlock(block);
            Color color = highlighted && useTint
                ? Color.Lerp(tintColors[i], highlightColor, highlightStrength)
                : tintColors[i];
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }
    }

    private void Rebuild()
    {
        spriteRenderers.Clear();
        spriteColors.Clear();
        spriteOutlineGroups.Clear();
        tintRenderers.Clear();
        tintColors.Clear();
        tintBlocks.Clear();
        ownerTarget = GetComponentInParent<InteractionTarget>(true);
        hasPrimarySortingLayer = false;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];
            if (!ShouldUseRenderer(spriteRenderer))
            {
                continue;
            }

            spriteRenderers.Add(spriteRenderer);
            spriteColors.Add(spriteRenderer.color);

            if (autoCreateSpriteOutlines)
            {
                SpriteOutlineGroup outlineGroup = new(spriteRenderer);
                spriteOutlineGroups.Add(outlineGroup);
                outlineGroup.SetOutline(false, outlineColor, outlineThickness);
            }
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer is SpriteRenderer || !ShouldUseRenderer(renderer))
            {
                continue;
            }

            tintRenderers.Add(renderer);
            tintColors.Add(ResolveRendererColor(renderer));
            tintBlocks.Add(new MaterialPropertyBlock());
        }
    }

    private static Color ResolveRendererColor(Renderer renderer)
    {
        Material material = renderer ? renderer.sharedMaterial : null;
        if (!material)
        {
            return Color.white;
        }

        if (material.HasProperty(BaseColorId))
        {
            return material.GetColor(BaseColorId);
        }

        return material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
    }

    private bool ShouldUseRenderer(Renderer renderer)
    {
        if (!renderer || renderer.gameObject.name.StartsWith("_SpriteOutlineProxy"))
        {
            return false;
        }

        InteractionTarget rendererTarget = renderer.GetComponentInParent<InteractionTarget>(true);
        if (ownerTarget && rendererTarget && rendererTarget != ownerTarget)
        {
            return false;
        }

        if (!restrictToPrimarySortingLayer)
        {
            return true;
        }

        if (!hasPrimarySortingLayer)
        {
            primarySortingLayerId = renderer.sortingLayerID;
            hasPrimarySortingLayer = true;
            return true;
        }

        return renderer.sortingLayerID == primarySortingLayerId;
    }

    private sealed class SpriteOutlineGroup
    {
        private const string ProxyChildPrefix = "_SpriteOutlineProxy";
        private const int OutlineCopies = 16;

        private static readonly Vector2[] Directions = BuildDirections();
        private static Material outlineMaterial;

        private readonly SpriteRenderer source;
        private readonly SpriteRenderer[] proxies = new SpriteRenderer[OutlineCopies];

        public SpriteOutlineGroup(SpriteRenderer source)
        {
            this.source = source;
            EnsureProxyRenderers();
        }

        public void SetOutline(bool enabled, Color color, float thickness)
        {
            if (!source || !source.sprite)
            {
                SetVisibility(false);
                return;
            }

            EnsureProxyRenderers();
            for (int i = 0; i < proxies.Length; i++)
            {
                SyncProxy(proxies[i], Directions[i], enabled, color, thickness);
            }
        }

        private void EnsureProxyRenderers()
        {
            if (!source) return;

#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(source.gameObject))
                return;
#endif

            DisableLegacyProxyObject();

            for (int i = 0; i < proxies.Length; i++)
            {
                if (proxies[i])
                {
                    continue;
                }

                string childName = $"{ProxyChildPrefix}_{i}";
                Transform child = source.transform.Find(childName);
                if (!child)
                {
                    GameObject proxyObject = new(childName)
                    {
                        hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                    };
                    child = proxyObject.transform;
                    child.SetParent(source.transform, false);
                }

                proxies[i] = child.GetOrAddComponent<SpriteRenderer>();
            }
        }

        private void SyncProxy(SpriteRenderer proxy, Vector2 direction, bool enabled, Color color, float thickness)
        {
            if (!proxy || !source)
            {
                return;
            }

            proxy.gameObject.layer = source.gameObject.layer;
            proxy.transform.localRotation = Quaternion.identity;
            proxy.transform.localScale = Vector3.one;
            proxy.transform.localPosition = new Vector3(direction.x * thickness, direction.y * thickness, 0.01f);
            proxy.enabled = enabled && source.enabled && color.a > 0.001f && thickness > 0f;
            proxy.sprite = source.sprite;
            proxy.drawMode = source.drawMode;
            proxy.size = source.size;
            proxy.tileMode = source.tileMode;
            proxy.adaptiveModeThreshold = source.adaptiveModeThreshold;
            proxy.flipX = source.flipX;
            proxy.flipY = source.flipY;
            proxy.maskInteraction = source.maskInteraction;
            proxy.sortingLayerID = source.sortingLayerID;
            proxy.sortingOrder = source.sortingOrder - 1;
            proxy.color = color;
            proxy.sharedMaterial = ResolveOutlineMaterial();
        }

        private void SetVisibility(bool visible)
        {
            for (int i = 0; i < proxies.Length; i++)
            {
                if (proxies[i])
                {
                    proxies[i].enabled = visible;
                }
            }
        }

        private void DisableLegacyProxyObject()
        {
            Transform legacy = source ? source.transform.Find(ProxyChildPrefix) : null;
            if (!legacy)
            {
                return;
            }

            Renderer[] renderers = legacy.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }

        private static Vector2[] BuildDirections()
        {
            Vector2[] directions = new Vector2[OutlineCopies];
            for (int i = 0; i < directions.Length; i++)
            {
                float radians = i * Mathf.PI * 2f / directions.Length;
                directions[i] = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }

            return directions;
        }

        private static Material ResolveOutlineMaterial()
        {
            if (outlineMaterial)
            {
                return outlineMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (!shader)
            {
                return null;
            }

            outlineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return outlineMaterial;
        }
    }
}

public enum OutlineMode
{
    TintOnly = 0,
    SpriteProxyOnly = 1,
    SpriteProxyWithTint = 2
}
